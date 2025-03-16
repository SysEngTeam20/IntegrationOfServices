import os
import sys
import json
import argparse
import time
import wave
import io
import struct
import numpy as np
from datetime import datetime
from ibm_watson import TextToSpeechV1
from ibm_cloud_sdk_core.authenticators import IAMAuthenticator
from ibm_cloud_sdk_core.api_exception import ApiException

def debug_print(*args, **kwargs):
    """Print debug messages to stderr"""
    print(*args, file=sys.stderr, **kwargs)

def initialize_speech_synthesizer():
    debug_print("Initializing Text-to-Speech service...")
    
    # First check for TTS-specific API key, then fall back to generic API key
    api_key = os.environ.get('IBM_TTS_API_KEY') or os.environ.get('IBM_API_KEY')
    service_url = os.environ.get('IBM_TTS_SERVICE_URL')
    
    # Debug environment variables (truncate sensitive info)
    debug_print(f"Environment check: TTS_API_KEY={'✓' if os.environ.get('IBM_TTS_API_KEY') else '✗'}, " +
                f"API_KEY={'✓' if os.environ.get('IBM_API_KEY') else '✗'}, " +
                f"SERVICE_URL={'✓' if service_url else '✗'}")
    
    if not api_key:
        debug_print("ERROR: Neither IBM_TTS_API_KEY nor IBM_API_KEY environment variables are set!")
        debug_print(f"Available environment variables: {list(os.environ.keys())}")
        return None
    
    if not service_url:
        debug_print("ERROR: IBM_TTS_SERVICE_URL environment variable is not set!")
        debug_print(f"Available environment variables: {list(os.environ.keys())}")
        return None
    
    # Truncate key for logging
    if api_key:
        debug_print(f"Using API key: {api_key[:5]}... and service URL: {service_url}")
    
    try:
        authenticator = IAMAuthenticator(api_key)
        text_to_speech = TextToSpeechV1(
            authenticator=authenticator
        )
        text_to_speech.set_service_url(service_url)
        
        # Test connection by getting voices
        voices = text_to_speech.list_voices().get_result()
        voice_count = len(voices['voices']) if voices and 'voices' in voices else 0
        debug_print(f"TTS Service successfully initialized. Available voices: {voice_count}")
        
        return text_to_speech
    except ApiException as e:
        if e.code == 403:
            debug_print(f"ERROR: Authentication failed with 403 Forbidden. The API key doesn't have permission for Text-to-Speech service.")
            debug_print(f"Please verify in your IBM Cloud account that this API key has access to the Text-to-Speech service.")
            debug_print(f"You might need separate API keys for Speech-to-Text and Text-to-Speech services.")
            debug_print(f"Current API key (first 5 chars): {api_key[:5] if api_key else 'None'}")
        else:
            debug_print(f"ERROR initializing TTS service: API error {e.code}: {str(e)}")
        return None
    except Exception as e:
        debug_print(f"Error initializing TTS service: {type(e).__name__}: {str(e)}")
        return None

def save_debug_audio(audio_content, text):
    """Save audio to a file for debugging purposes"""
    try:
        timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        filename = f"debug-tts-{timestamp}.wav"
        
        with wave.open(filename, 'wb') as wf:
            wf.setnchannels(1)
            wf.setsampwidth(2)  # 16-bit audio
            wf.setframerate(22050)  # Default rate for IBM TTS
            wf.writeframes(audio_content)
        
        debug_print(f"Saved debug audio to {filename} for text: '{text[:30]}...'")
    except Exception as e:
        debug_print(f"Failed to save debug audio: {str(e)}")

def extract_pcm_from_wav(wav_data):
    """Extract raw PCM data from WAV format"""
    try:
        with io.BytesIO(wav_data) as wav_io:
            with wave.open(wav_io, 'rb') as wav_file:
                # Get PCM format details for debugging
                channels = wav_file.getnchannels()
                sample_width = wav_file.getsampwidth()
                framerate = wav_file.getframerate()
                n_frames = wav_file.getnframes()
                
                # Get the PCM frames
                pcm_data = wav_file.readframes(n_frames)
                debug_print(f"Extracted {len(pcm_data)} bytes of PCM data from WAV format")
                debug_print(f"WAV format details: channels={channels}, sample_width={sample_width}, " +
                          f"framerate={framerate}, frames={n_frames}")
                
                # Verify it's 16-bit audio (2 bytes per sample) as expected by Unity
                if sample_width != 2:
                    debug_print(f"WARNING: WAV has {sample_width}-byte samples, but Unity expects 2-byte (16-bit) samples")
                
                # Check if we need to convert from stereo to mono (Unity's InjectPcm expects mono)
                if channels == 2:
                    debug_print("Converting stereo to mono for Unity...")
                    # Create a new buffer for mono data
                    mono_pcm = bytearray(n_frames * 2)  # 2 bytes per sample
                    for i in range(0, len(pcm_data), 4):  # 4 bytes per frame (2 channels * 2 bytes)
                        if i+3 < len(pcm_data):
                            # Extract left and right channel samples
                            left = pcm_data[i:i+2]
                            right = pcm_data[i+2:i+4]
                            # Average the channels (simple mix)
                            left_val = struct.unpack('<h', left)[0]
                            right_val = struct.unpack('<h', right)[0]
                            mixed = (left_val + right_val) // 2
                            # Pack the mixed value back
                            mixed_bytes = struct.pack('<h', mixed)
                            mono_pcm[i//2:i//2+2] = mixed_bytes
                    return bytes(mono_pcm)
                
                return pcm_data
    except Exception as e:
        debug_print(f"Error extracting PCM data from WAV: {type(e).__name__}: {str(e)}")
        return wav_data  # Return original data if extraction fails

def resample_audio(pcm_data, src_rate=22050, target_rate=48000):
    """
    Resample PCM audio data from source rate to target rate using linear interpolation
    
    Args:
        pcm_data (bytes): Raw PCM audio data (16-bit signed, little-endian)
        src_rate (int): Source sample rate in Hz
        target_rate (int): Target sample rate in Hz
    
    Returns:
        bytes: Resampled PCM audio data
    """
    debug_print(f"Resampling audio from {src_rate}Hz to {target_rate}Hz")
    
    # Check if resampling is needed
    if src_rate == target_rate:
        debug_print("No resampling needed, sample rates match")
        return pcm_data
    
    try:
        # Convert PCM bytes to numpy array of 16-bit integers
        samples = np.frombuffer(pcm_data, dtype=np.int16)
        
        # Calculate the resampling ratio
        ratio = float(target_rate) / float(src_rate)
        
        # Calculate the number of samples in the resampled audio
        target_length = int(len(samples) * ratio)
        debug_print(f"Original samples: {len(samples)}, Resampled length: {target_length}")
        
        # Create the output array for resampled data
        resampled = np.zeros(target_length, dtype=np.int16)
        
        # Perform linear interpolation
        for i in range(target_length):
            # Find the position in the original array
            src_idx_float = i / ratio
            src_idx_int = int(src_idx_float)
            fraction = src_idx_float - src_idx_int
            
            # Make sure we don't go out of bounds
            if src_idx_int >= len(samples) - 1:
                resampled[i] = samples[-1]  # Use the last sample
            else:
                # Linear interpolation between adjacent samples
                resampled[i] = int((1.0 - fraction) * samples[src_idx_int] + 
                                  fraction * samples[src_idx_int + 1])
        
        # Convert back to bytes
        resampled_bytes = resampled.tobytes()
        debug_print(f"Resampled PCM data size: {len(resampled_bytes)} bytes")
        
        return resampled_bytes
        
    except Exception as e:
        debug_print(f"Error resampling audio: {type(e).__name__}: {str(e)}")
        debug_print("Returning original audio data")
        return pcm_data

def verify_pcm_format(pcm_data, expected_sample_rate=22050):
    """Verify PCM data format and log details"""
    debug_print(f"PCM data size: {len(pcm_data)} bytes")
    
    # Calculate duration based on 16-bit mono PCM at expected sample rate
    samples = len(pcm_data) // 2  # 2 bytes per sample for 16-bit audio
    duration = samples / expected_sample_rate
    debug_print(f"Estimated audio duration: {duration:.2f} seconds ({samples} samples at {expected_sample_rate}Hz)")
    
    # Check for likely valid PCM data - simple heuristic
    if len(pcm_data) < 100:
        debug_print("WARNING: PCM data seems too short to be valid audio!")
    
    # Log the first few samples to help troubleshoot byte order issues
    if len(pcm_data) >= 20:
        debug_print("First 10 sample values:")
        for i in range(0, 20, 2):
            # Interpret as 16-bit signed integer (little-endian, as expected by Unity)
            sample_value = struct.unpack('<h', pcm_data[i:i+2])[0]
            debug_print(f"  Sample {i//2}: {sample_value} (bytes: {pcm_data[i]:02x} {pcm_data[i+1]:02x})")
    
    return pcm_data

def fix_audio_for_unity(pcm_data, source_rate=22050, unity_rate=48000):
    """
    Fix audio format for Unity by ensuring correct byte order and sample rate.
    
    This is a simplified approach focusing on what Unity exactly needs:
    - 16-bit signed PCM
    - Little-endian byte order
    - Resampled to Unity's expected sample rate
    """
    debug_print(f"Fixing audio format for Unity. Source rate: {source_rate}Hz, Unity rate: {unity_rate}Hz")

    # If no resampling needed, just verify format is correct
    if source_rate == unity_rate:
        debug_print("Sample rates match, no resampling needed")
        # Ensure data is in the correct format (16-bit signed PCM, little-endian)
        return pcm_data
    
    try:
        # Convert PCM bytes to a numpy array of 16-bit integers
        samples = np.frombuffer(pcm_data, dtype=np.int16)
        debug_print(f"Original PCM samples: {len(samples)}")
        
        # NEW APPROACH: Use a higher quality resampling technique with pre-filter to reduce aliasing
        # Normalize the original samples to float (-1.0 to 1.0 range) for better precision
        float_samples = samples.astype(np.float32) / 32768.0
        
        # Calculate the time axis for the original samples
        original_time = np.arange(len(float_samples)) / source_rate
        
        # Calculate the time axis for the resampled samples
        target_length = int(len(float_samples) * unity_rate / source_rate)
        target_time = np.arange(target_length) / unity_rate
        
        debug_print(f"Target length: {target_length} samples")
        
        # Use a more precise interpolation method
        # This uses a cubic spline interpolation which should reduce artifacts
        from scipy import interpolate
        try:
            # Try to use scipy's interpolate for better quality if available
            f = interpolate.interp1d(
                original_time, 
                float_samples, 
                kind='cubic', 
                bounds_error=False, 
                fill_value=(float_samples[0], float_samples[-1])
            )
            resampled_float = f(target_time)
            debug_print("Using cubic interpolation for higher quality resampling")
        except (ImportError, NameError):
            # Fallback to linear interpolation if scipy is not available
            debug_print("scipy not available, falling back to linear interpolation")
            resampled_float = np.zeros(target_length, dtype=np.float32)
            ratio = float(source_rate) / float(unity_rate)
            
            for i in range(target_length):
                src_idx = i * ratio
                src_idx_floor = int(src_idx)
                fraction = src_idx - src_idx_floor
                
                if src_idx_floor >= len(float_samples) - 1:
                    resampled_float[i] = float_samples[-1]
                else:
                    resampled_float[i] = (1.0 - fraction) * float_samples[src_idx_floor] + \
                                        fraction * float_samples[src_idx_floor + 1]
        
        # Apply a gentle low-pass filter to smooth out the signal (optional)
        # Simple boxcar filter, works for basic smoothing
        if len(resampled_float) > 10:
            kernel_size = 3  # Small kernel to avoid excessive smoothing
            kernel = np.ones(kernel_size) / kernel_size
            from numpy import convolve
            try:
                # Only apply filter if enough samples
                smoothed = convolve(resampled_float, kernel, mode='same')
                # Don't apply to the edges to avoid boundary effects
                resampled_float[kernel_size:-kernel_size] = smoothed[kernel_size:-kernel_size]
                debug_print("Applied gentle smoothing filter")
            except:
                debug_print("Error applying smoothing filter, skipping")
        
        # Convert back to int16 range with proper rounding
        resampled = np.round(resampled_float * 32767).astype(np.int16)
        
        # Normalize to avoid clipping (scale down if needed)
        max_val = np.max(np.abs(resampled))
        if max_val > 32767:
            debug_print(f"Normalizing audio to avoid clipping (max value: {max_val})")
            scale_factor = 32767.0 / max_val
            resampled = np.round(resampled * scale_factor).astype(np.int16)
        
        # Convert back to bytes
        resampled_bytes = resampled.tobytes()
        debug_print(f"Resampled audio: {len(resampled_bytes)} bytes")
        
        # Show some sample values for debugging
        debug_print("First 5 samples of resampled audio:")
        for i in range(min(5, len(resampled))):
            debug_print(f"  Sample {i}: {resampled[i]}")
            
        # Debug: Check for potentially problematic values
        zeros_count = np.sum(resampled == 0)
        max_sample = np.max(resampled)
        min_sample = np.min(resampled)
        debug_print(f"Audio statistics: min={min_sample}, max={max_sample}, zeros={zeros_count}/{len(resampled)}")
        
        return resampled_bytes
        
    except Exception as e:
        debug_print(f"Error fixing audio format: {type(e).__name__}: {str(e)}")
        debug_print("Falling back to simple resampling method")
        
        # Fallback to simple resampling in case of error
        try:
            # Convert PCM bytes to numpy array
            samples = np.frombuffer(pcm_data, dtype=np.int16)
            
            # Calculate resampling ratio
            ratio = float(source_rate) / float(unity_rate)
            target_length = int(len(samples) / ratio)
            resampled = np.zeros(target_length, dtype=np.int16)
            
            # Simple linear resampling
            for i in range(target_length):
                src_idx = i * ratio
                src_idx_floor = int(src_idx)
                if src_idx_floor < len(samples) - 1:
                    resampled[i] = samples[src_idx_floor]
                elif src_idx_floor < len(samples):
                    resampled[i] = samples[src_idx_floor]
            
            return resampled.tobytes()
        except:
            debug_print("All resampling methods failed, returning original data")
            return pcm_data

def create_basic_unity_compatible_audio(pcm_data, source_rate=22050, unity_rate=44100):
    """
    Create a guaranteed Unity-compatible PCM format by exactly matching
    what Unity's InjectPcm method expects.

    Looking at Unity's code:
    ```csharp
    public void InjectPcm(Span<byte> bytes)
    {
        for (int i = 0; i < bytes.Length / 2; i++)
        {
            var sample = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8)) / 32768f;
            samples.Enqueue(sample);
        }
    }
    ```
    
    This shows that Unity expects a little-endian byte order 
    (LSB first at index i*2, MSB second at index i*2+1).
    
    Parameters:
        pcm_data (bytes): Input PCM data (ignored for this test)
        source_rate (int): Original sample rate (ignored for this test)
        unity_rate (int): Target sample rate for Unity (44100Hz is what app.ts uses)
        
    Returns:
        bytes: Unity-compatible PCM data
    """
    try:
        debug_print("Creating ABSOLUTE GUARANTEED Unity-compatible audio - byte-by-byte approach")
        
        # Define a test tone
        duration = 1.0  # 1 second
        frequency = 440  # A4 note (standard tuning reference)
        
        # Generate as individual bytes to precisely control the format
        num_samples = int(unity_rate * duration)
        debug_print(f"Creating {num_samples} samples at {unity_rate}Hz")
        
        # Pre-allocate buffer for efficiency
        result_buffer = bytearray(num_samples * 2)  # 2 bytes per sample
        
        # Generate a very quiet sine wave
        for i in range(num_samples):
            # Generate sine wave value between -1 and 1
            t = i / unity_rate
            value = np.sin(2 * np.pi * frequency * t)
            
            # Scale to only 10% of maximum amplitude
            value = value * 0.1
            
            # Apply fade-in (first 0.05 seconds) and fade-out (last 0.05 seconds)
            fade_samples = int(unity_rate * 0.05)
            if i < fade_samples:
                value = value * (i / fade_samples)
            elif i > num_samples - fade_samples:
                value = value * ((num_samples - i) / fade_samples)
            
            # Convert to short (-32768 to 32767) but use only 10% of range
            short_value = int(value * 3276)  # 10% of 32768
            
            # Convert short to bytes in little-endian order
            # This exactly matches Unity's reading method: (bytes[i * 2] | (bytes[i * 2 + 1] << 8))
            result_buffer[i * 2] = short_value & 0xFF  # LSB
            result_buffer[i * 2 + 1] = (short_value >> 8) & 0xFF  # MSB
        
        # Extra check to ensure we're filling the buffer correctly
        if len(result_buffer) != num_samples * 2:
            debug_print(f"WARNING: Buffer length mismatch: expected {num_samples * 2}, got {len(result_buffer)}")
        
        # Log some sample values to verify format
        debug_print("First few samples (decimal, hex):")
        for i in range(min(5, num_samples)):
            sample_bytes = result_buffer[i*2:i*2+2]
            sample_value = (sample_bytes[0] | (sample_bytes[1] << 8))
            # Sign-extend if top bit is set
            if sample_value & 0x8000:
                sample_value = sample_value - 0x10000
            debug_print(f"  Sample {i}: {sample_value} (0x{sample_bytes[0]:02x}, 0x{sample_bytes[1]:02x})")
        
        return bytes(result_buffer)
    
    except Exception as e:
        debug_print(f"Error creating test tone: {str(e)}")
        # If everything fails, return a tiny amount of silence
        return np.zeros(100, dtype=np.int16).tobytes()

def synthesize_speech(text, synthesizer=None):
    if not synthesizer:
        debug_print("ERROR: TTS service not initialized properly.")
        api_key = os.environ.get('IBM_TTS_API_KEY')
        generic_key = os.environ.get('IBM_API_KEY')
        service_url = os.environ.get('IBM_TTS_SERVICE_URL')
        debug_print(f"Current environment: TTS_API_KEY={api_key[:5] + '...' if api_key else None}, " +
                   f"API_KEY={generic_key[:5] + '...' if generic_key else None}, " +
                   f"SERVICE_URL={service_url}")
        return None
    
    try:
        debug_print(f"Synthesizing speech for: '{text[:50]}...'")
        
        # NEW APPROACH: Return a properly formatted WAV file instead of raw PCM
        debug_print("USING WAV APPROACH - sending a properly formatted WAV file")
        
        try:
            # Request WAV data directly from Watson - this ensures a properly formatted WAV file
            # Use standard parameters that are well-supported everywhere
            debug_print("Requesting WAV data at 44100Hz (maximum compatibility)")
            result = synthesizer.synthesize(
                text=text,
                accept='audio/wav',
                voice='en-US_MichaelV3Voice'
            ).get_result()
            
            wav_data = result.content
            debug_print(f"Successfully synthesized {len(wav_data)} bytes of WAV audio")
            
            # DIAGNOSTIC: Save the original Watson WAV to a file
            try:
                timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                debug_filename = f"watson-original-{timestamp}.wav"
                with open(debug_filename, 'wb') as f:
                    f.write(wav_data)
                debug_print(f"DIAGNOSTIC: Saved original Watson WAV to {debug_filename}")
            except Exception as e:
                debug_print(f"Error saving diagnostic file: {str(e)}")
            
            # Check if it's a valid WAV file by examining the header
            if len(wav_data) > 44 and wav_data[:4] == b'RIFF' and wav_data[8:12] == b'WAVE':
                debug_print("Received valid WAV file with proper RIFF header")
            else:
                debug_print("WARNING: Received data doesn't appear to be a valid WAV file")
            
            try:
                # Extract WAV information for debugging
                with io.BytesIO(wav_data) as wav_io:
                    with wave.open(wav_io, 'rb') as wav_file:
                        channels = wav_file.getnchannels()
                        sample_width = wav_file.getsampwidth()
                        framerate = wav_file.getframerate()
                        n_frames = wav_file.getnframes()
                        
                        debug_print(f"WAV details: channels={channels}, bits={sample_width*8}, " +
                                 f"rate={framerate}Hz, frames={n_frames}, " +
                                 f"duration={n_frames/framerate:.2f}s")
                
                # If it's stereo, convert to mono for better Unity compatibility
                if channels == 2:
                    debug_print("Converting stereo WAV to mono for better Unity compatibility")
                    
                    # Create a new mono WAV file
                    with io.BytesIO(wav_data) as wav_in:
                        with wave.open(wav_in, 'rb') as stereo:
                            # Extract PCM data
                            stereo_pcm = stereo.readframes(stereo.getnframes())
                            
                            # Create a new mono WAV
                            mono_wav = io.BytesIO()
                            with wave.open(mono_wav, 'wb') as mono:
                                mono.setnchannels(1)
                                mono.setsampwidth(sample_width)
                                mono.setframerate(framerate)
                                
                                # Convert stereo to mono by averaging channels
                                mono_pcm = bytearray(len(stereo_pcm) // 2)
                                for i in range(0, len(stereo_pcm), 4):  # 4 bytes per frame (2 channels * 2 bytes)
                                    if i+3 < len(stereo_pcm):
                                        # Extract left and right channel samples
                                        left = stereo_pcm[i:i+2]
                                        right = stereo_pcm[i+2:i+4]
                                        # Average the channels (simple mix)
                                        left_val = struct.unpack('<h', left)[0]
                                        right_val = struct.unpack('<h', right)[0]
                                        mixed = (left_val + right_val) // 2
                                        # Pack the mixed value back
                                        mixed_bytes = struct.pack('<h', mixed)
                                        mono_pcm[i//2:i//2+2] = mixed_bytes
                                
                                mono.writeframes(mono_pcm)
                            
                            # Get the mono WAV data
                            wav_data = mono_wav.getvalue()
                            debug_print(f"Created mono WAV file: {len(wav_data)} bytes")
                            
                            # DIAGNOSTIC: Save the mono WAV to a file
                            try:
                                timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                                debug_filename = f"watson-mono-{timestamp}.wav"
                                with open(debug_filename, 'wb') as f:
                                    f.write(wav_data)
                                debug_print(f"DIAGNOSTIC: Saved mono converted WAV to {debug_filename}")
                            except Exception as e:
                                debug_print(f"Error saving diagnostic file: {str(e)}")
            
            except Exception as e:
                debug_print(f"Error analyzing WAV data: {str(e)}")
                # Continue with the original WAV data even if analysis fails
            
            # Generate a single cycle pilot tone WAV to help Unity with sample rate detection
            # This is a 440Hz sine wave that's only a few milliseconds long
            try:
                # Create a short pilot WAV file at precisely 44100Hz with a clear 440Hz tone
                debug_print("Adding pilot tone WAV at the start for better detection")
                pilot_io = io.BytesIO()
                with wave.open(pilot_io, 'wb') as pilot:
                    pilot.setnchannels(1)  # Mono
                    pilot.setsampwidth(2)  # 16-bit
                    pilot.setframerate(44100)  # Standard CD quality
                    
                    # Generate about 5ms of a 440Hz tone
                    samples = 220  # ~5ms at 44100Hz
                    tone_data = bytearray(samples * 2)  # 2 bytes per sample
                    for i in range(samples):
                        value = int(np.sin(2 * np.pi * 440 * i / 44100) * 3276)  # 10% volume
                        # Pack in little-endian format
                        tone_data[i*2] = value & 0xFF
                        tone_data[i*2+1] = (value >> 8) & 0xFF
                    
                    pilot.writeframes(tone_data)
                
                pilot_wav = pilot_io.getvalue()
                debug_print(f"Created pilot tone WAV: {len(pilot_wav)} bytes")
                
                # DIAGNOSTIC: Save the pilot tone WAV to a file
                try:
                    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                    debug_filename = f"pilot-tone-{timestamp}.wav"
                    with open(debug_filename, 'wb') as f:
                        f.write(pilot_wav)
                    debug_print(f"DIAGNOSTIC: Saved pilot tone WAV to {debug_filename}")
                except Exception as e:
                    debug_print(f"Error saving diagnostic file: {str(e)}")
                
                # Combine pilot with speech WAV using a simple approach
                # We need to extract the PCM data from both WAVs and create a new combined WAV
                with io.BytesIO(pilot_wav) as pilot_io, io.BytesIO(wav_data) as speech_io:
                    with wave.open(pilot_io, 'rb') as pilot, wave.open(speech_io, 'rb') as speech:
                        # Ensure formats match
                        if (pilot.getnchannels() != speech.getnchannels() or
                            pilot.getsampwidth() != speech.getsampwidth() or
                            pilot.getframerate() != speech.getframerate()):
                            debug_print("Pilot and speech WAV formats don't match, using speech WAV only")
                            
                            # Save the final speech-only WAV for diagnostic purposes
                            try:
                                timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                                debug_filename = f"final-speech-only-{timestamp}.wav"
                                with open(debug_filename, 'wb') as f:
                                    f.write(wav_data)
                                debug_print(f"DIAGNOSTIC: Saved final speech-only WAV to {debug_filename}")
                            except Exception as e:
                                debug_print(f"Error saving diagnostic file: {str(e)}")
                                
                            return wav_data
                        
                        # Extract PCM data
                        pilot_pcm = pilot.readframes(pilot.getnframes())
                        speech_pcm = speech.readframes(speech.getnframes())
                        
                        # Create a new combined WAV
                        combined_wav = io.BytesIO()
                        with wave.open(combined_wav, 'wb') as combined:
                            combined.setnchannels(speech.getnchannels())
                            combined.setsampwidth(speech.getsampwidth())
                            combined.setframerate(speech.getframerate())
                            # Write pilot then speech
                            combined.writeframes(pilot_pcm)
                            combined.writeframes(speech_pcm)
                        
                        final_wav = combined_wav.getvalue()
                        debug_print(f"Created combined WAV with pilot tone: {len(final_wav)} bytes")
                        
                        # DIAGNOSTIC: Save the final combined WAV to a file
                        try:
                            timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                            debug_filename = f"final-combined-{timestamp}.wav"
                            with open(debug_filename, 'wb') as f:
                                f.write(final_wav)
                            debug_print(f"DIAGNOSTIC: Saved final combined WAV to {debug_filename}")
                        except Exception as e:
                            debug_print(f"Error saving diagnostic file: {str(e)}")
                        
                        return final_wav
            
            except Exception as e:
                debug_print(f"Error creating pilot tone: {str(e)}")
                # Fall back to original WAV if pilot creation fails
                
                # DIAGNOSTIC: Save the fallback WAV to a file
                try:
                    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                    debug_filename = f"fallback-{timestamp}.wav"
                    with open(debug_filename, 'wb') as f:
                        f.write(wav_data)
                    debug_print(f"DIAGNOSTIC: Saved fallback WAV to {debug_filename}")
                except Exception as e:
                    debug_print(f"Error saving diagnostic file: {str(e)}")
            
            return wav_data
            
        except Exception as wav_error:
            debug_print(f"Error synthesizing WAV: {str(wav_error)}. Falling back to PCM.")
            
            # Fallback to PCM and create a WAV manually
            try:
                # Request PCM at 44100Hz (most compatible)
                result = synthesizer.synthesize(
                    text=text,
                    accept='audio/l16;rate=44100',
                    voice='en-US_MichaelV3Voice'
                ).get_result()
                
                pcm_data = result.content
                debug_print(f"Got {len(pcm_data)} bytes of PCM data, creating WAV")
                
                # DIAGNOSTIC: Save the raw PCM to a file
                try:
                    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                    debug_filename = f"raw-pcm-{timestamp}.pcm"
                    with open(debug_filename, 'wb') as f:
                        f.write(pcm_data)
                    debug_print(f"DIAGNOSTIC: Saved raw PCM to {debug_filename}")
                except Exception as e:
                    debug_print(f"Error saving diagnostic file: {str(e)}")
                
                # Create a WAV from the PCM data
                wav_buffer = io.BytesIO()
                with wave.open(wav_buffer, 'wb') as wav_file:
                    wav_file.setnchannels(1)  # Mono
                    wav_file.setsampwidth(2)  # 16-bit
                    wav_file.setframerate(44100)  # 44.1kHz
                    wav_file.writeframes(pcm_data)
                
                wav_data = wav_buffer.getvalue()
                debug_print(f"Created WAV from PCM: {len(wav_data)} bytes")
                
                # DIAGNOSTIC: Save the PCM-derived WAV to a file
                try:
                    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                    debug_filename = f"pcm-derived-wav-{timestamp}.wav"
                    with open(debug_filename, 'wb') as f:
                        f.write(wav_data)
                    debug_print(f"DIAGNOSTIC: Saved PCM-derived WAV to {debug_filename}")
                except Exception as e:
                    debug_print(f"Error saving diagnostic file: {str(e)}")
                
                return wav_data
                
            except Exception as pcm_error:
                debug_print(f"Error creating WAV from PCM: {str(pcm_error)}")
                
                # If all else fails, return a minimal beep WAV as indicator
                try:
                    # Create a simple "beep" WAV to indicate failure
                    beep_io = io.BytesIO()
                    with wave.open(beep_io, 'wb') as beep:
                        beep.setnchannels(1)  # Mono
                        beep.setsampwidth(2)  # 16-bit
                        beep.setframerate(44100)  # 44.1kHz
                        
                        # Generate a short beep
                        samples = 4410  # 0.1s at 44.1kHz
                        beep_data = bytearray(samples * 2)  # 2 bytes per sample
                        for i in range(samples):
                            value = int(np.sin(2 * np.pi * 880 * i / 44100) * 16383)  # 50% volume
                            # Pack in little-endian format
                            beep_data[i*2] = value & 0xFF
                            beep_data[i*2+1] = (value >> 8) & 0xFF
                        
                        beep.writeframes(beep_data)
                    
                    beep_wav = beep_io.getvalue()
                    
                    # DIAGNOSTIC: Save the beep WAV to a file
                    try:
                        timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
                        debug_filename = f"beep-{timestamp}.wav"
                        with open(debug_filename, 'wb') as f:
                            f.write(beep_wav)
                        debug_print(f"DIAGNOSTIC: Saved beep WAV to {debug_filename}")
                    except Exception as e:
                        debug_print(f"Error saving diagnostic file: {str(e)}")
                    
                    return beep_wav
                    
                except Exception as e:
                    debug_print(f"Even creating a simple beep failed: {str(e)}")
                    return None
            
    except ApiException as e:
        debug_print(f"Watson API Error during synthesis: {e.code}: {str(e)}")
        return None
    except Exception as e:
        debug_print(f"Error synthesizing speech: {type(e).__name__}: {str(e)}")
        return None

def main():
    parser = argparse.ArgumentParser()
    args = parser.parse_args()
    
    debug_print(f"Python executable: {sys.executable}, version: {sys.version}")
    
    # Initialize the speech synthesizer
    synthesizer = initialize_speech_synthesizer()
    if not synthesizer:
        debug_print("Cannot initialize IBM Watson Text-to-Speech service. Check your API key and service URL.")
        sys.exit(1)
    
    debug_print("TTS service ready. Waiting for text input from stdin...")
    
    # Main processing loop
    try:
        for line in sys.stdin:
            line = line.strip()
            if line:
                debug_print(f"Received text to synthesize: '{line[:50]}...'")
                audio_data = synthesize_speech(line, synthesizer)
                if audio_data:
                    # Write audio data to stdout
                    sys.stdout.buffer.write(audio_data)
                    sys.stdout.buffer.flush()
                    debug_print(f"Sent {len(audio_data)} bytes of audio data")
                else:
                    debug_print("Failed to synthesize speech")
    except KeyboardInterrupt:
        debug_print("Keyboard interrupt received, exiting")
    except EOFError:
        debug_print("End of input stream reached")
    except BrokenPipeError:
        debug_print("BrokenPipeError - Node.js may have terminated the connection")
        # Exit quietly for broken pipe
        try:
            sys.stderr.close()
        except:
            pass
        sys.exit(0)
    except Exception as e:
        debug_print(f"Error in TTS main loop: {type(e).__name__}: {str(e)}")
        sys.exit(1)
    
    debug_print("TTS service shutting down")

if __name__ == "__main__":
    main() 