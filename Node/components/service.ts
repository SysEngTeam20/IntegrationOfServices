import { EventEmitter } from 'node:events';
import { spawn, ChildProcess } from 'child_process';
import { NetworkScene } from 'ubiq';
import { Logger } from './logger';
import { RoomClient } from 'ubiq-server/components/roomclient';
import { createInterface } from 'readline';
import * as path from 'path';

class ServiceController extends EventEmitter {
    name: string;
    config: any;
    roomClient: RoomClient;
    childProcesses: { [identifier: string]: ChildProcess };
    closingProcesses: Set<string>; // Track processes that are being closed
    epipeErrorCounts: Map<string, number>;
    lastEpipeLogTime: Map<string, number>;
    EPIPE_LOG_THRESHOLD = 10; // Only log every X occurrences
    EPIPE_LOG_INTERVAL = 5000; // Minimum time between logs (ms)

    /**
     * Constructor for the Service class.
     *
     * @constructor
     * @param {NetworkScene} scene - The NetworkScene in which the service should be registered.
     * @param {string} name - The name of the service.
     * @param {object} config - An object containing configuration information for the service.
     */
    constructor(scene: NetworkScene, name: string) {
        super();
        this.name = name;
        this.roomClient = scene.getComponent('RoomClient') as RoomClient;
        this.childProcesses = {};
        this.closingProcesses = new Set(); // Initialize set to track closing processes
        this.epipeErrorCounts = new Map<string, number>();
        this.lastEpipeLogTime = new Map<string, number>();

        // Use a more graceful shutdown process
        const gracefulShutdown = (signal: string) => {
            this.log(`Received ${signal} signal, shutting down gracefully...`);
            this.killAllChildProcesses();
            // Give processes a moment to clean up before exiting
            setTimeout(() => {
                process.exit(0);
            }, 500);
        };

        // Listen for process exit events and ensure child processes are killed
        process.on('exit', () => this.killAllChildProcesses());
        process.on('SIGINT', () => gracefulShutdown('SIGINT'));
        process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));
        
        // Handle uncaught exceptions
        process.on('uncaughtException', (err: NodeJS.ErrnoException) => {
            this.log(`Uncaught exception: ${err.message}`, 'error');
            if (err.code === 'EPIPE') {
                this.log(`Gracefully handled EPIPE error: ${err.message}`, 'warning');
            } else {
                // For other errors, log and exit
                this.killAllChildProcesses();
                process.exit(1);
            }
        });
    }

    /**
     * Method to register a child process. This method registers the child process with the existing OnResponse and OnError callbacks.
     *
     * @memberof Service
     * @instance
     * @param {string} identifier - The identifier for the child process. This should be unique for each child process.
     * @param {string} command - The command to execute. E.g. "python".
     * @param {Array<string>} options - The options to pass to the command.
     * @throws {Error} If identifier is undefined or if the child process fails to spawn.
     * @returns {ChildProcess} The spawned child process.
     */
    registerChildProcess(identifier: string, command: string, options: Array<string>): ChildProcess {
        if (identifier === undefined) {
            throw new Error(`Identifier must be defined for child process of service: ${this.name}`);
        }
        if (this.childProcesses[identifier] !== undefined) {
            throw new Error(`Identifier: ${identifier} already in use for child process of service: ${this.name}`);
        }

        try {
            // Make a copy of the current environment variables
            const env = { ...process.env };
            
            // Ensure we have a unified API key that all services can use
            if (env.IBM_STT_API_KEY && !env.IBM_API_KEY) {
                env.IBM_API_KEY = env.IBM_STT_API_KEY;
                this.log(`Setting IBM_API_KEY from IBM_STT_API_KEY for all services`);
            } else if (env.IBM_TTS_API_KEY && !env.IBM_API_KEY) {
                env.IBM_API_KEY = env.IBM_TTS_API_KEY;
                this.log(`Setting IBM_API_KEY from IBM_TTS_API_KEY for all services`);
            }
            
            // Log all critical environment variables for debugging
            this.log(`Environment variables for ${this.name}:`);
            if (this.name === 'TextToSpeechService') {
                this.log(`IBM_TTS_API_KEY=${env.IBM_TTS_API_KEY ? env.IBM_TTS_API_KEY.substring(0, 5) + '...' : 'Not set'}`);
                this.log(`IBM_API_KEY=${env.IBM_API_KEY ? env.IBM_API_KEY.substring(0, 5) + '...' : 'Not set'}`);
                this.log(`IBM_TTS_SERVICE_URL=${env.IBM_TTS_SERVICE_URL || 'Not set'}`);
            } else if (this.name === 'SpeechToTextService') {
                this.log(`IBM_STT_API_KEY=${env.IBM_STT_API_KEY ? env.IBM_STT_API_KEY.substring(0, 5) + '...' : 'Not set'}`);
                this.log(`IBM_API_KEY=${env.IBM_API_KEY ? env.IBM_API_KEY.substring(0, 5) + '...' : 'Not set'}`);
                this.log(`IBM_STT_SERVICE_URL=${env.IBM_STT_SERVICE_URL || 'Not set'}`);
            }
            
            // Spawn the child process with the environment variables and improved options
            this.childProcesses[identifier] = spawn(command, options, { 
                env,
                stdio: ['pipe', 'pipe', 'pipe'], // Ensure all stdio channels are created
                windowsHide: true, // Don't create a console window on Windows
                detached: false // Keep process attached to parent
            });
            
            this.log(`Child process spawned for ${this.name} with ID: ${identifier}`);
        } catch (e) {
            throw new Error(`Failed to spawn child process for service: ${this.name}. Error: ${e}`);
        }

        // Register events for the child process.
        const childProcess = this.childProcesses[identifier];
        if (childProcess && childProcess.stdout && childProcess.stderr) {
            // Handle standard output data
            childProcess.stdout.on('data', (data) => {
                if (!this.closingProcesses.has(identifier)) {
                    this.emit('data', data, identifier);
                }
            });
            
            // Handle error output
            childProcess.stderr.on('data', (data) => {
                console.error(`\x1b[31mService ${this.name} error, from child process ${identifier}:${data}\x1b[0m`);
            });
            
            // Handle process close
            childProcess.on('close', (code, signal) => {
                this.log(`Child process ${identifier} closed with code ${code} and signal ${signal}`);
                this.closingProcesses.delete(identifier);
                delete this.childProcesses[identifier];
                this.emit('close', code, signal, identifier);
            });
            
            // Handle process exit (can occur before close)
            childProcess.on('exit', (code, signal) => {
                this.log(`Child process ${identifier} exited with code ${code} and signal ${signal}`);
                this.closingProcesses.add(identifier);
            });
            
            // Handle process errors
            childProcess.on('error', (err) => {
                this.log(`Child process error for ${identifier}: ${err.message}`, 'error');
            });
            
            // Handle stdin errors separately
            if (childProcess.stdin) {
                childProcess.stdin.on('error', (err: NodeJS.ErrnoException) => {
                    if (err.code === 'EPIPE') {
                        this.log(`EPIPE error when writing to process ${identifier}. Process may have closed its input.`, 'warning');
                        // Mark the process as closing to prevent further writes
                        this.closingProcesses.add(identifier);
                    } else {
                        this.log(`Error on stdin for process ${identifier}: ${err.message}`, 'error');
                    }
                });
            }
        }

        this.log(`Registered child process with identifier: ${identifier}`);

        // Check if the child process has already been closed.
        if (this.childProcesses[identifier].killed) {
            this.closingProcesses.add(identifier);
            delete this.childProcesses[identifier];
            this.emit('close', 0, 'SIGTERM', identifier);
        }

        // Return reference to the child process.
        return this.childProcesses[identifier];
    }

    /**
     * Logs a message to the console with the service name.
     *
     * @memberof ServiceController
     * @param {string} message - The message to log.
     */
    log(message: string, level: 'info' | 'warning' | 'error' = 'info', end: string = '\n'): void {
        Logger.log(this.name, message, level, end, '\x1b[35m');
    }

    /**
     * Sends data to a child process with the specified identifier.
     *
     * @memberof Service
     * @param {string} data - The data to send to the child process.
     * @param {string} identifier - The identifier of the child process to send the data to.
     * @instance
     * @throws {Error} Throws an error if the child process with the specified identifier is not found.
     */
    sendToChildProcess(identifier: string, data: string | Buffer) {
        // Don't attempt to write to a closing or nonexistent process
        if (this.closingProcesses.has(identifier) || this.childProcesses[identifier] === undefined) {
            this.log(`Child process with identifier ${identifier} not found or is closing for service: ${this.name}`, 'warning');
            return;
        }

        try {
            const process = this.childProcesses[identifier];
            if (process && process.stdin && !process.stdin.destroyed && process.stdin.writable) {
                process.stdin.write(data, (err) => {
                    if (err) {
                        const nodeErr = err as NodeJS.ErrnoException;
                        this.log(`Write callback error for ${identifier}: ${nodeErr.message}`, 'error');
                        if (nodeErr.code === 'EPIPE') {
                            // If we get EPIPE, mark the process as closing
                            this.closingProcesses.add(identifier);
                        }
                    }
                });
            } else {
                this.log(`Cannot write to child process ${identifier}: stdin not available or not writable`, 'warning');
            }
        } catch (error) {
            const err = error as NodeJS.ErrnoException;
            this.log(`Error writing to child process ${identifier}: ${err.message}`, 'error');
            if (err.code === 'EPIPE') {
                // If we get EPIPE, mark the process as closing
                this.closingProcesses.add(identifier);
            }
        }
    }

    /**
     * Method to kill a specific child process.
     *
     * @memberof Service
     * @param {string} identifier - The identifier for the child process to kill.
     * @instance
     */
    killChildProcess(identifier: string) {
        if (this.childProcesses[identifier] === undefined) {
            throw new Error(`Child process with identifier: ${identifier} not found for service: ${this.name}`);
        }

        // Mark as closing before we kill it
        this.closingProcesses.add(identifier);
        
        try {
            // Try to end stdin gracefully first
            if (this.childProcesses[identifier].stdin) {
                this.childProcesses[identifier].stdin.end();
            }
            
            // Give it a moment to clean up, then force kill if necessary
            setTimeout(() => {
                if (this.childProcesses[identifier] && !this.childProcesses[identifier].killed) {
                    this.childProcesses[identifier].kill('SIGTERM');
                    delete this.childProcesses[identifier];
                }
            }, 100);
        } catch (e) {
            this.log(`Error killing child process ${identifier}: ${e}`, 'error');
            // Force remove from our tracking
            delete this.childProcesses[identifier];
        }
    }

    /**
     * Method to kill all child processes.
     *
     * @memberof Service
     * @instance
     */
    killAllChildProcesses() {
        this.log('Killing all child processes');
        
        // Mark all as closing
        Object.keys(this.childProcesses).forEach(id => {
            this.closingProcesses.add(id);
        });
        
        // First try to end stdin for all processes gracefully
        for (const [id, childProcess] of Object.entries(this.childProcesses)) {
            try {
                if (childProcess.stdin) {
                    childProcess.stdin.end();
                }
            } catch (e) {
                this.log(`Error ending stdin for process ${id}: ${e}`, 'error');
            }
        }
        
        // Give them a moment to clean up, then kill any that remain
        setTimeout(() => {
            for (const [id, childProcess] of Object.entries(this.childProcesses)) {
                try {
                    if (!childProcess.killed) {
                        childProcess.kill('SIGTERM');
                    }
                } catch (e) {
                    this.log(`Error killing process ${id}: ${e}`, 'error');
                }
            }
            // Clear the childProcesses object
            this.childProcesses = {};
        }, 100);
    }

    writeToChildProcess(identifier: string, data: string | Buffer): boolean {
        if (this.childProcesses[identifier]) {
            // Don't write to processes that are being closed
            if (this.closingProcesses.has(identifier)) {
                return false;
            }

            try {
                const stdin = this.childProcesses[identifier].stdin;
                if (stdin === null) {
                    this.log(`Cannot write to child process ${identifier}: stdin is null`, 'error');
                    return false;
                }
                return stdin.write(data);
            } catch (error: any) { // Type the error as 'any' to access code property
                // Handle EPIPE errors with batching
                if (error.code === 'EPIPE') {
                    // Increment the error count
                    const count = (this.epipeErrorCounts.get(identifier) || 0) + 1;
                    this.epipeErrorCounts.set(identifier, count);
                    
                    const now = Date.now();
                    const lastLogTime = this.lastEpipeLogTime.get(identifier) || 0;
                    const timeSinceLastLog = now - lastLogTime;
                    
                    // Only log every 10 occurrences or if 5 seconds have passed since last log
                    if (count % 10 === 0 || timeSinceLastLog > 5000) {
                        this.log(`Write callback error for ${identifier}: write EPIPE (occurred ${count} times)`, 'warning');
                        this.lastEpipeLogTime.set(identifier, now);
                        
                        // If this happens many times, log a summary message
                        if (count >= 100) {
                            this.log(`EPIPE error when writing to process ${identifier}. Process may have closed its input.`, 'warning');
                            this.epipeErrorCounts.set(identifier, 0); // Reset the counter
                        }
                    }
                } else {
                    // For other errors, log each occurrence
                    this.log(`Error writing to child process ${identifier}: ${error.message || 'Unknown error'}`, 'error');
                }
                return false;
            }
        } else {
            this.log(`Cannot write to child process ${identifier}: Process not found`, 'error');
            return false;
        }
    }
}

export { ServiceController };
