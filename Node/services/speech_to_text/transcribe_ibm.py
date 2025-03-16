import os
import sys
import json
import argparse
import time
import threading
from ibm_watson import SpeechToTextV1
from ibm_watson.websocket import RecognizeCallback, AudioSource
from ibm_cloud_sdk_core.authenticators import IAMAuthenticator
from ibm_cloud_sdk_core.api_exception import ApiException

class MyRecognizeCallback(RecognizeCallback):
    def __init__(self):
        RecognizeCallback.__init__(self)
        self.done = False
        self.last_data_time = time.time()

    def on_transcription(self, transcript):
        # Print each transcription result
        print("Got transcription result!", file=sys.stderr)
        print(f"Transcript data: {json.dumps(transcript)}", file=sys.stderr)
        if len(transcript) > 0:
            try:
                print(">{}".format(transcript[0].get('transcript', '')))
                sys.stdout.flush()  # Make sure the output is flushed to Node.js
            except BrokenPipeError:
                print("BrokenPipeError when writing to stdout. Node process may have closed the pipe.", file=sys.stderr)
                self.done = True
            except Exception as e:
                print(f"Error writing transcription to stdout: {str(e)}", file=sys.stderr)

    def on_error(self, error):
        print('Error received: {}'.format(error), file=sys.stderr)
        self.done = True

    def on_inactivity_timeout(self, error):
        print('Inactivity timeout: {}'.format(error), file=sys.stderr)
        self.done = True

    def on_connected(self):
        print('Connection was successful', file=sys.stderr)

    def on_close(self):
        print('Connection closed', file=sys.stderr)
        self.done = True
        
    def on_data(self, data):
        # Called when data is received
        print(f"Received data from Watson API", file=sys.stderr)
        self.last_data_time = time.time()

class DebugAudioSource(AudioSource):
    def __init__(self, input_stream, is_recording=True):
        super().__init__(input_stream, is_recording)
        self.bytes_read = 0
        self.last_read_time = time.time()
        self.buffer = b''  # Add a buffer to store data
        
    def read(self, size):
        try:
            # Try to read data from stdin
            if len(self.buffer) < size:
                # Need more data from stdin
                chunk = self._input.read(1024)  # Read a larger chunk to fill buffer
                if chunk:
                    self.buffer += chunk
                    self.bytes_read += len(chunk)
                    self.last_read_time = time.time()
                    if self.bytes_read % 10240 == 0:  # Only log every 10KB to reduce spam
                        print(f"Read {len(chunk)} bytes from input stream, total: {self.bytes_read}", file=sys.stderr)
                else:
                    # No data available, wait a bit
                    time.sleep(0.01)
            
            # Return data from the buffer
            if self.buffer:
                # Give requested size or all we have if less
                out_data = self.buffer[:size]
                self.buffer = self.buffer[size:]
                return out_data
            else:
                # No data in buffer
                return b''
                
        except BrokenPipeError:
            print("BrokenPipeError when reading from stdin. Node process may have closed the pipe.", file=sys.stderr)
            self.is_recording = False
            return b''
        except Exception as e:
            print(f"Error reading from input stream: {str(e)}", file=sys.stderr)
            time.sleep(0.01)  # Add a small delay on error
            return b''

# Monitor stdin to see if we're getting data
def monitor_stdin(audio_source, callback):
    print("Started monitoring thread for input stream and Watson API", file=sys.stderr)
    last_bytes_read = 0
    
    while not callback.done:
        try:
            current_time = time.time()
            
            # Check if we're still receiving data
            if current_time - audio_source.last_read_time > 5:
                bytes_diff = audio_source.bytes_read - last_bytes_read
                print(f"No new audio data received for 5 seconds. Total bytes read: {audio_source.bytes_read}, New bytes: {bytes_diff}", file=sys.stderr)
                audio_source.last_read_time = current_time
                last_bytes_read = audio_source.bytes_read
            
            # Check if Watson API is responding
            if current_time - callback.last_data_time > 10 and audio_source.bytes_read > 0:
                print(f"No Watson API interaction for 10 seconds (Potential API issue)", file=sys.stderr)
                callback.last_data_time = current_time
                
            time.sleep(1)
        except Exception as e:
            print(f"Error in monitoring thread: {str(e)}", file=sys.stderr)
            time.sleep(1)

def recognize_from_stdin(peer):
    print(f"Starting speech recognition for peer: {peer}", file=sys.stderr)
    
    # Initialize the IBM Watson Speech to Text client
    # Check for both specific and shared API keys
    api_key = os.environ.get('IBM_STT_API_KEY') or os.environ.get('IBM_API_KEY')
    service_url = os.environ.get('IBM_STT_SERVICE_URL')
    
    if not api_key:
        print("ERROR: Neither IBM_STT_API_KEY nor IBM_API_KEY environment variables are set", file=sys.stderr)
        print(f"Environment variables: {list(os.environ.keys())}", file=sys.stderr)
        sys.exit(1)
    
    if not service_url:
        print("ERROR: IBM_STT_SERVICE_URL environment variable is not set", file=sys.stderr)
        print(f"Environment variables: {list(os.environ.keys())}", file=sys.stderr)
        sys.exit(1)
    
    print(f"Using API key: {api_key[:5]}... and service URL: {service_url}", file=sys.stderr)
    
    try:
        authenticator = IAMAuthenticator(api_key)
        speech_to_text = SpeechToTextV1(
            authenticator=authenticator
        )
        speech_to_text.set_service_url(service_url)
        
        # Test connection
        models = speech_to_text.list_models().get_result()
        print(f"STT Service initialized successfully. Available models: {len(models['models'])}", file=sys.stderr)
    except ApiException as e:
        if e.code == 403:
            print(f"ERROR: Authentication failed with 403 Forbidden. The API key doesn't have permission for Speech-to-Text service.", file=sys.stderr)
            print(f"Please verify in your IBM Cloud account that this API key has access to the Speech-to-Text service.", file=sys.stderr)
        else:
            print(f"ERROR initializing STT service: API error {e.code}: {str(e)}", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"Error initializing STT service: {type(e).__name__}: {str(e)}", file=sys.stderr)
        sys.exit(1)

    # Create callback object
    my_callback = MyRecognizeCallback()

    # Create audio source from stdin with debugging
    print("Creating audio source from stdin", file=sys.stderr)
    try:
        audio_source = DebugAudioSource(
            sys.stdin.buffer,
            is_recording=True
        )
    except Exception as e:
        print(f"Failed to create audio source: {str(e)}", file=sys.stderr)
        sys.exit(1)

    # Start a monitoring thread
    monitor_thread = threading.Thread(target=monitor_stdin, args=(audio_source, my_callback))
    monitor_thread.daemon = True
    monitor_thread.start()

    # Start recognition
    print("Starting recognition using websocket", file=sys.stderr)
    try:
        speech_to_text.recognize_using_websocket(
            audio=audio_source,
            content_type='audio/l16; rate=48000',
            recognize_callback=my_callback,
            model='en-US_BroadbandModel',
            interim_results=True,  # Enable interim results for more feedback
            inactivity_timeout=-1,  # Disable inactivity timeout
            max_alternatives=3  # Get multiple alternatives
        )
    except ApiException as e:
        print(f"API Error during recognition: {e.code}: {str(e)}", file=sys.stderr)
        if e.code == 403:
            print("Error 403 Forbidden: API key doesn't have permission for Speech-to-Text service", file=sys.stderr)
        my_callback.done = True
        sys.exit(1)
    except Exception as e:
        print(f"Error starting recognition: {type(e).__name__}: {str(e)}", file=sys.stderr)
        my_callback.done = True
        sys.exit(1)

    print("Recognition started, waiting for input...", file=sys.stderr)

    # Keep reading until done
    try:
        while not my_callback.done:
            time.sleep(0.1)  # Reduced CPU usage compared to pass
    except KeyboardInterrupt:
        print("Keyboard interrupt received, stopping", file=sys.stderr)
    except BrokenPipeError:
        print("BrokenPipeError in main loop, stopping", file=sys.stderr)
    except Exception as e:
        print(f"Error during recognition: {type(e).__name__}: {str(e)}", file=sys.stderr)
    finally:
        print("Recognition loop ended, cleaning up", file=sys.stderr)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--peer", type=str, default="00000000-0000-0000-0000-000000000000")
    args = parser.parse_args()

    print(f"Python executable: {sys.executable}, version: {sys.version}", file=sys.stderr)
    
    try:
        recognize_from_stdin(args.peer)
        print("IBM Watson Speech client stopped receiving chunks.", file=sys.stderr)
    except BrokenPipeError:
        print("BrokenPipeError at main level. Node.js may have terminated the connection.", file=sys.stderr)
        # Exit quietly for broken pipe
        try:
            sys.stderr.close()
        except:
            pass
        sys.exit(0)
    except KeyboardInterrupt:
        print("Keyboard interrupt received, exiting", file=sys.stderr)
        sys.exit(0)
    except Exception as e:
        print(f"Error in speech recognition service: {type(e).__name__}: {str(e)}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main() 