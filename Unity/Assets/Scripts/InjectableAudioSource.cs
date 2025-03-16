using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Security.Cryptography;

/// <summary>
/// This Component manages an Audio Source that you can insert PCM data into.
/// This is useful for when sound data comes from the internet.
/// </summary>
public class InjectableAudioSource : MonoBehaviour
{
    private AudioClip clip;
    private AudioSource audioSource;
    
    // For custom audio playback
    private Queue<float[]> samples = new Queue<float[]>();
    private float[] currentSample;
    private int position;
    
    // Audio format properties
    private int channels = 1;
    private int bitsPerSample = 16;
    private int sampleRate = 48000;
    
    // Path to the debug folder
    private string debugFolderPath;
    
    // Queue of WAV data to play
    private class AudioDataInfo
    {
        public byte[] audioData;
        public bool isWav;
        public int originalSampleRate;
        public int dataSize;
        public DateTime creationTime;
        public string debugInfo;
        public string audioHash; // Hash for deduplication
    }
    private Queue<AudioDataInfo> audioDataQueue = new Queue<AudioDataInfo>();
    private bool isProcessingAudioQueue = false;
    private bool isPaused = false;
    
    // Currently playing audio
    private AudioClip currentClip;
    
    // For deduplication
    private HashSet<string> recentAudioHashes = new HashSet<string>();
    private float deduplicationTimeWindow = 3.0f; // Only deduplicate within this time window (seconds)
    private List<KeyValuePair<string, float>> hashTimestamps = new List<KeyValuePair<string, float>>();
    
    // Response tracking
    private bool isCurrentlyResponding = false;
    private float lastResponseEndTime = 0f;
    private float responseGapThreshold = 0.5f; // Time gap to consider responses separate
    
    public bool debug = true; // Set to true to enable debug logging by default

    private void Start()
    {
        // Initialize the audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set default audio source properties
        audioSource.loop = false;
        audioSource.spatialBlend = 0; // 2D sound
        
        // Create a debug folder for saving WAV files
        debugFolderPath = Path.Combine(Application.persistentDataPath, "AudioDebug");
        if (!Directory.Exists(debugFolderPath))
        {
            try
            {
                Directory.CreateDirectory(debugFolderPath);
                Debug.Log($"Created audio debug folder at: {debugFolderPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create audio debug folder: {e.Message}");
            }
        }
        
        // Start the audio processing coroutine
        StartCoroutine(ProcessAudioQueue());
        StartCoroutine(CleanupOldHashes());
    }

    private void OnDestroy()
    {
        // Clean up any playing audio
        if (currentClip != null)
        {
            audioSource.Stop();
            Destroy(currentClip);
            currentClip = null;
        }
        
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Called each frame to check the audio status
    /// </summary>
    private void Update()
    {
        // Check if current audio finished playing
        if (currentClip != null && !audioSource.isPlaying && !isPaused)
        {
            // Clean up the current clip
            Destroy(currentClip);
            currentClip = null;
        }
        
        // Update response status
        if (currentClip != null && audioSource.isPlaying)
        {
            isCurrentlyResponding = true;
        }
        else if (isCurrentlyResponding && currentClip == null)
        {
            isCurrentlyResponding = false;
            lastResponseEndTime = Time.time;
        }
    }
    
    // Coroutine to clean up old audio hashes periodically
    private IEnumerator CleanupOldHashes()
    {
        while (true)
        {
            // Clean up old hashes
            float currentTime = Time.time;
            hashTimestamps.RemoveAll(pair => currentTime - pair.Value > deduplicationTimeWindow);
            
            // Update the hash set
            recentAudioHashes.Clear();
            foreach (var pair in hashTimestamps)
            {
                recentAudioHashes.Add(pair.Key);
            }
            
            yield return new WaitForSeconds(1.0f);
        }
    }

    /// <summary>
    /// Coroutine to process the audio queue
    /// </summary>
    private IEnumerator ProcessAudioQueue()
    {
        isProcessingAudioQueue = true;
        
        while (true)
        {
            // Process the next audio in queue if not currently playing
            if (currentClip == null && audioDataQueue.Count > 0 && !isPaused)
            {
                // Check if we're starting a new response after a gap
                bool isNewResponse = (Time.time - lastResponseEndTime) > responseGapThreshold;
                
                // If this is a new response, add a tiny delay to ensure full separation
                if (isNewResponse && lastResponseEndTime > 0)
                {
                    yield return new WaitForSeconds(0.2f);
                }
                
                AudioDataInfo audioInfo = audioDataQueue.Dequeue();
                
                // Play this audio
                if (audioInfo.isWav)
                {
                    // Direct playback for WAV files
                    yield return StartCoroutine(PlayWavData(audioInfo));
                }
                else
                {
                    // Handle raw PCM data
                    PlayPcmData(audioInfo.audioData);
                }
                
                // Small yield to ensure proper audio queue processing
                yield return null;
            }
            
            // Use a shorter wait time to reduce stuttering
            yield return new WaitForSeconds(0.03f);
        }
    }
    
    /// <summary>
    /// Calculate a hash from audio data for deduplication
    /// </summary>
    private string CalculateAudioHash(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
            return string.Empty;
            
        try
        {
            // For large audio files, we'll hash only specific portions to improve performance
            // but still maintain uniqueness
            using (MD5 md5 = MD5.Create())
            {
                // Hash a combination of:
                // 1. First 1024 bytes (or less if file is smaller)
                // 2. Middle 1024 bytes (if file is large enough)
                // 3. Last 1024 bytes (if different from #1)
                // 4. File length as bytes
                
                byte[] firstPortion = new byte[Math.Min(1024, audioData.Length)];
                Buffer.BlockCopy(audioData, 0, firstPortion, 0, firstPortion.Length);
                
                byte[] lengthBytes = BitConverter.GetBytes(audioData.Length);
                
                // For very small files, just hash what we have
                if (audioData.Length <= 1024)
                {
                    md5.TransformBlock(firstPortion, 0, firstPortion.Length, null, 0);
                    md5.TransformFinalBlock(lengthBytes, 0, lengthBytes.Length);
                    return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
                }
                
                // For larger files, include middle and end portions
                int middleStart = (audioData.Length / 2) - 512;
                if (middleStart < 0) middleStart = 0;
                
                byte[] middlePortion = new byte[Math.Min(1024, audioData.Length - middleStart)];
                Buffer.BlockCopy(audioData, middleStart, middlePortion, 0, middlePortion.Length);
                
                int endStart = Math.Max(0, audioData.Length - 1024);
                if (endStart < 1024) endStart = Math.Min(1024, audioData.Length);
                
                byte[] endPortion = new byte[Math.Min(1024, audioData.Length - endStart)];
                Buffer.BlockCopy(audioData, endStart, endPortion, 0, endPortion.Length);
                
                md5.TransformBlock(firstPortion, 0, firstPortion.Length, null, 0);
                md5.TransformBlock(middlePortion, 0, middlePortion.Length, null, 0);
                md5.TransformBlock(endPortion, 0, endPortion.Length, null, 0);
                md5.TransformFinalBlock(lengthBytes, 0, lengthBytes.Length);
                
                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error calculating audio hash: {e.Message}");
            return audioData.Length.ToString(); // Fallback to just length
        }
    }
    
    /// <summary>
    /// Process incoming audio data (either WAV or PCM)
    /// </summary>
    public void InjectPcm(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogWarning("Received null or empty audio data");
            return;
        }
        
        // Skip very small audio chunks (likely just noise)
        if (audioData.Length < 200)
        {
            if (debug) Debug.Log($"Skipping very small audio chunk ({audioData.Length} bytes)");
            return;
        }
        
        // Calculate hash for deduplication
        string audioHash = CalculateAudioHash(audioData);
        
        // Check for duplicates (could be a duplicate message from server)
        if (!string.IsNullOrEmpty(audioHash) && recentAudioHashes.Contains(audioHash))
        {
            if (debug) Debug.Log($"Skipping duplicate audio data (hash: {audioHash})");
            return;
        }
        
        // Add hash to recent hashes with current timestamp
        recentAudioHashes.Add(audioHash);
        hashTimestamps.Add(new KeyValuePair<string, float>(audioHash, Time.time));
        
        // Debug: Output the first 20 bytes to check format
        if (debug)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("First 20 bytes of audio data (hex): ");
            for (int i = 0; i < Math.Min(20, audioData.Length); i++)
            {
                sb.Append(audioData[i].ToString("X2") + " ");
            }
            Debug.Log(sb.ToString());
        }
        
        // Check if this is a WAV file
        bool isWav = audioData.Length > 44 && 
                     audioData[0] == 'R' && audioData[1] == 'I' && audioData[2] == 'F' && audioData[3] == 'F' &&
                     audioData[8] == 'W' && audioData[9] == 'A' && audioData[10] == 'V' && audioData[11] == 'E';
        
        // Create info about this audio
        AudioDataInfo audioInfo = new AudioDataInfo
        {
            audioData = audioData,
            isWav = isWav,
            originalSampleRate = 22050, // Default IBM Watson TTS sample rate
            dataSize = audioData.Length,
            creationTime = DateTime.Now,
            debugInfo = "",
            audioHash = audioHash
        };
        
        if (isWav)
        {
            // Parse WAV header for more information
            try
            {
                // Get sample rate from WAV header (offset 24-27 in little-endian format)
                audioInfo.originalSampleRate = BitConverter.ToInt32(audioData, 24);
                
                // Find the data chunk to get the audio data size
                for (int i = 36; i < Math.Min(200, audioData.Length - 8); i++)
                {
                    if (audioData[i] == 'd' && audioData[i + 1] == 'a' && 
                        audioData[i + 2] == 't' && audioData[i + 3] == 'a')
                    {
                        audioInfo.dataSize = BitConverter.ToInt32(audioData, i + 4);
                        break;
                    }
                }
                
                audioInfo.debugInfo = $"WAV {audioInfo.originalSampleRate}Hz, data size {audioInfo.dataSize} bytes";
                
                if (debug) Debug.Log($"Detected WAV format: {audioInfo.debugInfo}");
                
                // Save WAV file for debugging
                SaveAudioDebugFile(audioData, $"wav_{audioInfo.originalSampleRate}Hz_{audioInfo.dataSize}bytes");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error parsing WAV header: {e.Message}");
            }
        }
        else
        {
            // Mark as PCM data and save for debugging
            audioInfo.debugInfo = $"PCM data, {audioData.Length} bytes";
            if (debug) Debug.Log($"Processing as raw PCM data: {audioInfo.debugInfo}");
            
            SaveAudioDebugFile(audioData, "raw_pcm_data");
        }
        
        // Add to the processing queue
        audioDataQueue.Enqueue(audioInfo);
        
        if (debug) Debug.Log($"Queued audio for playback: {audioInfo.debugInfo} (Queue size: {audioDataQueue.Count})");
    }
    
    /// <summary>
    /// Play WAV audio data directly
    /// </summary>
    private IEnumerator PlayWavData(AudioDataInfo audioInfo)
    {
        if (debug) Debug.Log($"Playing WAV data: {audioInfo.debugInfo}");
        
        // Create a temporary file for the WAV data
        string tempFilePath = Path.Combine(Application.temporaryCachePath, $"temp_audio_{DateTime.Now.Ticks}.wav");
        
        bool fileCreated = false;
        try
        {
            // Save the WAV data to a temporary file
            File.WriteAllBytes(tempFilePath, audioInfo.audioData);
            fileCreated = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving WAV data to temporary file: {e.Message}");
            yield break;
        }
        
        if (!fileCreated)
        {
            Debug.LogError("Failed to create temporary WAV file");
            yield break;
        }
        
        // Load the WAV file using UnityWebRequest
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.WAV);
        www.SendWebRequest();
        
        // Wait for request to complete (outside try/catch)
        while (!www.isDone)
        {
            yield return null;
        }
        
        AudioClip newClip = null;
        
        try
        {
            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to load audio: {www.error}");
            }
            else
            {
                // Get the audio clip
                newClip = DownloadHandlerAudioClip.GetContent(www);
                
                if (newClip == null)
                {
                    Debug.LogError("Failed to create audio clip from WAV data");
                }
                else
                {
                    // Report clip information
                    if (debug) Debug.Log($"WAV clip loaded: Length={newClip.length}s, Samples={newClip.samples}, " +
                                        $"Channels={newClip.channels}, Frequency={newClip.frequency}Hz");
                    
                    // Adjust playback speed if needed (to fix sped-up audio)
                    float playbackSpeed = 1.0f;
                    if (newClip.frequency != audioInfo.originalSampleRate && audioInfo.originalSampleRate > 0)
                    {
                        playbackSpeed = (float)audioInfo.originalSampleRate / newClip.frequency;
                        if (debug) Debug.Log($"Adjusting playback speed to {playbackSpeed} to match source rate");
                    }
                    
                    // Set clip and play
                    audioSource.Stop();
                    audioSource.clip = newClip;
                    audioSource.pitch = playbackSpeed;
                    audioSource.Play();
                    
                    // Store reference to current clip
                    currentClip = newClip;
                    
                    // Calculate duration outside of try block
                    float adjustedDuration = newClip.length / playbackSpeed;
                    if (debug) Debug.Log($"Waiting for clip to finish playing, duration: {adjustedDuration}s");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling audio clip: {e.Message}");
            
            // Clean up if needed
            if (newClip != null)
            {
                Destroy(newClip);
                newClip = null;
                currentClip = null;
            }
        }
        
        // If we have a valid clip, wait for it to finish playing
        if (newClip != null)
        {
            float adjustedDuration = newClip.length / audioSource.pitch;
            
            // Wait just slightly less than the full duration to minimize gaps
            yield return new WaitForSeconds(adjustedDuration - 0.02f);
            
            if (debug) Debug.Log("WAV playback completed");
        }
        
        // Clean up temporary file (outside try/catch)
        if (File.Exists(tempFilePath))
        {
            try { File.Delete(tempFilePath); } 
            catch (Exception e) { Debug.LogWarning($"Failed to delete temp file: {e.Message}"); }
        }
    }
    
    /// <summary>
    /// Process PCM data for playback
    /// </summary>
    private void PlayPcmData(byte[] pcmData)
    {
        try
        {
            if (debug) Debug.Log($"Processing raw PCM data, {pcmData.Length} bytes");
            
            // Create a new audio clip
            AudioClip newClip = AudioClip.Create(
                "PCM Audio", 
                pcmData.Length / 2, // 16-bit samples = 2 bytes per sample
                1, // mono
                22050, // sample rate (IBM Watson default)
                false
            );
            
            // Convert PCM data to float samples
            float[] samples = new float[pcmData.Length / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                // Convert 16-bit PCM (little endian) to float
                short value = (short)((pcmData[i * 2 + 1] << 8) | pcmData[i * 2]);
                samples[i] = value / 32768.0f; // Convert to -1.0 to 1.0 range
            }
            
            // Set the audio clip data
            newClip.SetData(samples, 0);
            
            // Play the clip
            audioSource.Stop();
            audioSource.clip = newClip;
            audioSource.pitch = 1.0f;
            audioSource.Play();
            
            // Store reference to current clip
            currentClip = newClip;
            
            if (debug) Debug.Log($"Started playing PCM audio, {samples.Length} samples");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error playing PCM data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Pause audio playback
    /// </summary>
    public void PauseAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            isPaused = true;
        }
    }
    
    /// <summary>
    /// Resume audio playback
    /// </summary>
    public void ResumeAudio()
    {
        if (isPaused)
        {
            audioSource.UnPause();
            isPaused = false;
        }
    }
    
    /// <summary>
    /// Stop all audio playback and clear queues
    /// </summary>
    public void StopAllAudio()
    {
        audioSource.Stop();
        audioDataQueue.Clear();
        isPaused = false;
        
        if (currentClip != null)
        {
            Destroy(currentClip);
            currentClip = null;
        }
    }
    
    /// <summary>
    /// Save audio data to a file for debugging
    /// </summary>
    private string SaveAudioDebugFile(byte[] audioData, string label)
    {
        if (audioData == null || audioData.Length == 0)
        {
            return null;
        }
        
        try
        {
            string fileName = $"audio_{DateTime.Now:yyyyMMdd_HHmmss}_{label}.wav";
            string filePath = Path.Combine(debugFolderPath, fileName);
            
            bool isWav = audioData.Length > 44 && 
                         audioData[0] == 'R' && audioData[1] == 'I' && audioData[2] == 'F' && audioData[3] == 'F' &&
                         audioData[8] == 'W' && audioData[9] == 'A' && audioData[10] == 'V' && audioData[11] == 'E';
            
            if (isWav)
            {
                // Save WAV as-is
                File.WriteAllBytes(filePath, audioData);
            }
            else
            {
                // Create a simple WAV wrapper for PCM data (assuming 16-bit mono)
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int sampleRate = 22050; // Default for IBM Watson TTS
                    short channels = 1;
                    short bitsPerSample = 16;
                    
                    // Calculate WAV format values
                    int byteRate = sampleRate * channels * bitsPerSample / 8;
                    short blockAlign = (short)(channels * bitsPerSample / 8);
                    int dataSize = audioData.Length;
                    int fileSize = 36 + dataSize;
                    
                    // Write RIFF header
                    writer.Write(Encoding.ASCII.GetBytes("RIFF")); // ChunkID
                    writer.Write(fileSize);                         // ChunkSize
                    writer.Write(Encoding.ASCII.GetBytes("WAVE")); // Format
                    
                    // Write fmt chunk
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));  // Subchunk1ID
                    writer.Write(16);                               // Subchunk1Size (16 for PCM)
                    writer.Write((short)1);                         // AudioFormat (1 for PCM)
                    writer.Write(channels);                         // NumChannels
                    writer.Write(sampleRate);                       // SampleRate
                    writer.Write(byteRate);                         // ByteRate
                    writer.Write(blockAlign);                       // BlockAlign
                    writer.Write(bitsPerSample);                    // BitsPerSample
                    
                    // Write data chunk
                    writer.Write(Encoding.ASCII.GetBytes("data"));  // Subchunk2ID
                    writer.Write(dataSize);                         // Subchunk2Size
                    writer.Write(audioData);                        // The actual data
                    
                    // Save the WAV file
                    File.WriteAllBytes(filePath, stream.ToArray());
                }
            }
            
            if (debug) Debug.Log($"Saved debug audio file: {filePath}");
            return filePath;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save debug audio file: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Generate a simple sine wave for testing purposes
    /// </summary>
    public void GenerateTestTone()
    {
        int sampleRate = 22050;
        int samples = sampleRate * 2; // 2 seconds
        
        // Create a simple sine wave
        float[] sampleData = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / sampleRate;
            sampleData[i] = Mathf.Sin(2 * Mathf.PI * 440 * time) * 0.5f; // 440Hz = A4 note
        }
        
        // Create an audio clip
        AudioClip testClip = AudioClip.Create("TestTone", samples, 1, sampleRate, false);
        testClip.SetData(sampleData, 0);
        
        // Play the test tone
        audioSource.Stop();
        audioSource.clip = testClip;
        audioSource.pitch = 1.0f;
        audioSource.Play();
        
        // Store reference to current clip
        if (currentClip != null)
        {
            Destroy(currentClip);
        }
        currentClip = testClip;
        
        Debug.Log("Playing test tone");
    }
}