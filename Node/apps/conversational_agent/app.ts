import { NetworkId } from 'ubiq';
import { ApplicationController } from '../../components/application';
import { TextToSpeechService } from '../../services/text_to_speech/service';
import { SpeechToTextService } from '../../services/speech_to_text/service';
import { TextGenerationService } from '../../services/text_generation/service';
import { MediaReceiver } from '../../components/media_receiver';
import path from 'path';
import { RTCAudioData } from '@roamhq/wrtc/types/nonstandard';
import { fileURLToPath } from 'url';
import dotenv from 'dotenv';

export class ConversationalAgent extends ApplicationController {
    components: {
        mediaReceiver?: MediaReceiver;
        speech2text?: SpeechToTextService;
        textGenerationService?: TextGenerationService;
        textToSpeechService?: TextToSpeechService;
    } = {};
    targetPeer: string = '';
    private activityId: string;

    constructor(configFile: string = 'config.json') {
        super(configFile);
        
        // Load .env.local from project root
        dotenv.config({ path: path.resolve(process.cwd(), '.env.local') });
        
        // Get activityId from environment variable with a default value
        this.activityId = process.env.ACTIVITY_ID || 'default-activity';
        this.log(`Initializing Conversational Agent for activity: ${this.activityId}`);
    }

    start(): void {
        // STEP 1: Register services (and any other components) used by the application
        this.registerComponents();
        this.log(`Services registered: ${Object.keys(this.components).join(', ')}`);

        // STEP 2: Define the application pipeline
        this.definePipeline();
        this.log('Pipeline defined');

        // STEP 3: Join a room based on the configuration (optionally creates a server)
        this.joinRoom();
    }

    registerComponents() {
        // An MediaReceiver to receive audio data from peers
        this.components.mediaReceiver = new MediaReceiver(this.scene);

        // A SpeechToTextService to transcribe audio coming from peers
        this.components.speech2text = new SpeechToTextService(this.scene);

        // A TextGenerationService to generate text based on text
        this.components.textGenerationService = new TextGenerationService(this.scene, this.activityId);

        // A TextToSpeechService to generate audio based on text
        this.components.textToSpeechService = new TextToSpeechService(this.scene);
    }

    definePipeline() {
        // Step 1: When we receive audio data from a peer we send it to the transcription service
        this.components.mediaReceiver?.on('audio', (uuid: string, data: RTCAudioData) => {
            const sampleBuffer = Buffer.from(data.samples.buffer);

            if (this.roomClient.peers.get(uuid) !== undefined) {
                this.log(`Received audio data from peer ${uuid}, sending to STT service. Samples length: ${data.samples.length}`);
                this.components.speech2text?.sendToChildProcess(uuid, sampleBuffer);
            }
        });

        // Step 2: When we receive a response from the transcription service, we send it to the text generation service
        this.components.speech2text?.on('data', (data: Buffer, identifier: string) => {
            const peer = this.roomClient.peers.get(identifier);
            const peerName = peer?.properties.get('ubiq.displayname');

            let response = data.toString().replace(/(\r\n|\n|\r)/gm, '');
            
            if (response.startsWith('>')) {
                response = response.slice(1);
                if (response.trim()) {
                    const message = (peerName + ' -> Agent:: ' + response).trim();
                    this.log(message);

                    // Pass activityId with each message
                    this.components.textGenerationService?.sendToChildProcess(
                        'default',
                        message + '\n',
                        this.activityId
                    );
                }
            }
        });

        // Step 3: When we receive a response from the text generation service, send it to text to speech
        this.components.textGenerationService?.on('data', (data: Buffer, identifier: string) => {
            const response = data.toString();
            this.log('Received text generation response from child process ' + identifier + ': ' + response, 'info');

            const [, name, message] = response.match(/-> (.*?):: (.*)/) || [];

            if (!name || !message) {
                this.log('Error parsing target peer and message', 'error');
                return;
            }

            this.targetPeer = name.trim();
            this.components.textToSpeechService?.sendToChildProcess('default', message.trim() + '\n');
        });

        this.components.textToSpeechService?.on('data', (data: Buffer, identifier: string) => {
            let response = data;
            const totalLength = data.length;
            
            this.log(`Received audio data from TTS service. Total size: ${totalLength} bytes.`);

            // First send the AudioInfo message with metadata about the audio
            this.scene.send(new NetworkId(95), {
                type: 'AudioInfo',
                targetPeer: this.targetPeer,
                audioLength: totalLength,
                sampleRate: 44100,  // Match the sample rate set in text_to_speech_ibm.py
                channels: 1,        // Mono audio
                bitsPerSample: 16,  // 16-bit PCM
            });
            
            // Use a smaller chunk size of 4096 bytes which is more standard for audio processing
            const chunkSize = 4096;
            let chunkIndex = 0;
            const totalChunks = Math.ceil(response.length / chunkSize);
            
            this.log(`Sending audio in ${totalChunks} chunks of ${chunkSize} bytes each.`);

            // Send chunks with index information to help Unity reconstruct them in order
            while (response.length > 0) {
                const chunk = response.slice(0, chunkSize);
                
                // Send the chunk with metadata to help Unity reconstruct the audio
                this.scene.send(new NetworkId(95), {
                    type: 'AudioData',
                    chunkIndex: chunkIndex,
                    totalChunks: totalChunks,
                    data: chunk
                });
                
                response = response.slice(chunkSize);
                chunkIndex++;
            }
            
            this.log(`Finished sending all ${chunkIndex} audio chunks.`);
        });
    }
}

if (fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
    const configPath = './config.json';
    const __dirname = path.dirname(fileURLToPath(import.meta.url));
    const absConfigPath = path.resolve(__dirname, configPath);
    const app = new ConversationalAgent(absConfigPath);
    app.start();
}
