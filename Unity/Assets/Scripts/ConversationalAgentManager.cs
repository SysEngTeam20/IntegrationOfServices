using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using System;

public class ConversationalAgentManager : MonoBehaviour
{
    private class AssistantSpeechUnit
    {
        public float startTime;
        public int samples;
        public string speechTargetName;

        public float endTime { get { return startTime + samples/(float)AudioSettings.outputSampleRate; } }
    }

    private NetworkId networkId = new NetworkId(95);
    private NetworkContext context;

    public InjectableAudioSource audioSource;
    public VirtualAssistantController assistantController;
    public AudioSourceVolume volume;

    private string speechTargetName;

    private List<AssistantSpeechUnit> speechUnits = new List<AssistantSpeechUnit>();
    
    // Flag to track if audio from the server should be processed
    private bool isProcessingServerAudio = true;

    [Serializable]
    private struct Message
    {
        public string type;
        public string targetPeer;
        public string audioLength;
    }

    [Serializable]
    private class BufferData
    {
        public string type;
        public int[] data;
    }

    [Serializable]
    private class AudioJsonWrapper
    {
        public string type;
        public int chunkIndex;
        public int totalChunks;
        public BufferData data; // This is a Node.js Buffer object in JSON format
        public int sampleRate;
        public int channels;
        public string format;
    }
    
    // Keep track of chunked audio data
    private Dictionary<int, byte[]> audioChunks = new Dictionary<int, byte[]>();
    private int expectedTotalChunks = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this, networkId);
    }

    // Update is called once per frame
    void Update()
    {
        while(speechUnits.Count > 0)
        {
            if (Time.time > speechUnits[0].endTime)
            {
                speechUnits.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        if (assistantController)
        {
            var speechTarget = null as string;
            if (speechUnits.Count > 0)
            {
                speechTarget = speechUnits[0].speechTargetName;
            }

            assistantController.UpdateAssistantSpeechStatus(speechTarget, volume.volume);
        }
    }

    private void ProcessJsonAudio(string jsonString)
    {
        Debug.Log("Processing JSON audio data");
        
        // Print more details about the JSON
        int maxJsonLength = Math.Min(100, jsonString.Length);
        Debug.Log($"JSON data starts with: {jsonString.Substring(0, maxJsonLength)}...");
        
        // First try standard JsonUtility
        try
        {
            var jsonData = JsonUtility.FromJson<AudioJsonWrapper>(jsonString);
            if (jsonData != null)
            {
                // Debug all fields
                Debug.Log($"JSON fields - Type: {jsonData.type}, ChunkIndex: {jsonData.chunkIndex}, " +
                          $"TotalChunks: {jsonData.totalChunks}, SampleRate: {jsonData.sampleRate}, " +
                          $"Channels: {jsonData.channels}, Format: {jsonData.format}");
                
                // Check if we have a Buffer object in the data field
                if (jsonData.data != null && jsonData.data.type == "Buffer" && jsonData.data.data != null && jsonData.data.data.Length > 0)
                {
                    Debug.Log($"Found Node.js Buffer with {jsonData.data.data.Length} bytes");
                    
                    // Convert int array to byte array
                    byte[] audioBytes = new byte[jsonData.data.data.Length];
                    for (int i = 0; i < audioBytes.Length; i++)
                    {
                        audioBytes[i] = (byte)jsonData.data.data[i];
                    }
                    
                    // Print first few bytes to debug
                    string bytesDebug = "First 20 bytes: ";
                    for (int i = 0; i < Math.Min(20, audioBytes.Length); i++)
                    {
                        bytesDebug += audioBytes[i].ToString("X2") + " ";
                    }
                    Debug.Log(bytesDebug);
                    
                    // Check if this is chunked audio data
                    if (jsonData.type == "AudioData" && jsonData.totalChunks > 1)
                    {
                        Debug.Log($"Processing audio chunk {jsonData.chunkIndex + 1} of {jsonData.totalChunks}: {audioBytes.Length} bytes");
                        expectedTotalChunks = jsonData.totalChunks;
                        
                        // Store the chunk
                        audioChunks[jsonData.chunkIndex] = audioBytes;
                        
                        // Check if we have all chunks
                        if (audioChunks.Count == expectedTotalChunks)
                        {
                            Debug.Log($"All {expectedTotalChunks} chunks received, combining them...");
                            // Combine all chunks
                            byte[] combinedAudio = CombineAudioChunks();
                            if (combinedAudio != null && combinedAudio.Length > 0)
                            {
                                // Create a copy of the combined data for safe keeping
                                byte[] safeCopy = new byte[combinedAudio.Length];
                                Buffer.BlockCopy(combinedAudio, 0, safeCopy, 0, combinedAudio.Length);
                                
                                // Check if this is a WAV file (for debugging)
                                if (combinedAudio.Length > 44 && 
                                    combinedAudio[0] == 'R' && combinedAudio[1] == 'I' && combinedAudio[2] == 'F' && combinedAudio[3] == 'F' &&
                                    combinedAudio[8] == 'W' && combinedAudio[9] == 'A' && combinedAudio[10] == 'V' && combinedAudio[11] == 'E')
                                {
                                    Debug.Log("Combined audio is a valid WAV file");
                                    
                                    // Parse WAV length from header for better logging
                                    int dataSize = -1;
                                    for (int i = 12; i < combinedAudio.Length - 8; i++)
                                    {
                                        if (combinedAudio[i] == 'd' && combinedAudio[i + 1] == 'a' && 
                                            combinedAudio[i + 2] == 't' && combinedAudio[i + 3] == 'a')
                                        {
                                            dataSize = BitConverter.ToInt32(combinedAudio, i + 4);
                                            break;
                                        }
                                    }
                                    
                                    if (dataSize > 0)
                                    {
                                        Debug.Log($"WAV data chunk size: {dataSize} bytes");
                                    }
                                }
                                
                                // Send to audio source
                                audioSource.InjectPcm(safeCopy);
                                Debug.Log($"Injected combined audio: {safeCopy.Length} bytes");
                            }
                            else
                            {
                                Debug.LogError("Failed to combine audio chunks");
                            }
                            audioChunks.Clear();
                        }
                        
                        return;
                    }
                    else
                    {
                        // Non-chunked audio data
                        Debug.Log($"Processing single audio buffer: {audioBytes.Length} bytes");
                        
                        // Create a copy of the data for safe keeping
                        byte[] safeCopy = new byte[audioBytes.Length];
                        Buffer.BlockCopy(audioBytes, 0, safeCopy, 0, audioBytes.Length);
                        
                        audioSource.InjectPcm(safeCopy);
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("No valid Buffer data found in JSON");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON with JsonUtility: {e.Message}");
        }
        
        // If JsonUtility failed, try manual JSON parsing to extract the Buffer data
        try
        {
            int bufferDataIndex = jsonString.IndexOf("\"data\":{\"type\":\"Buffer\",\"data\":[");
            if (bufferDataIndex >= 0)
            {
                Debug.Log("Found Buffer data via manual parsing");
                
                // Extract the array content
                int arrayStartIndex = jsonString.IndexOf('[', bufferDataIndex);
                int arrayEndIndex = jsonString.IndexOf(']', arrayStartIndex);
                
                if (arrayStartIndex > 0 && arrayEndIndex > arrayStartIndex)
                {
                    string arrayContent = jsonString.Substring(arrayStartIndex + 1, arrayEndIndex - arrayStartIndex - 1);
                    string[] numberStrings = arrayContent.Split(',');
                    
                    byte[] audioBytes = new byte[numberStrings.Length];
                    for (int i = 0; i < numberStrings.Length; i++)
                    {
                        if (int.TryParse(numberStrings[i], out int value))
                        {
                            audioBytes[i] = (byte)value;
                        }
                    }
                    
                    Debug.Log($"Manually extracted {audioBytes.Length} bytes from Buffer data");
                    
                    // Create a copy of the data for safe keeping
                    byte[] safeCopy = new byte[audioBytes.Length];
                    Buffer.BlockCopy(audioBytes, 0, safeCopy, 0, audioBytes.Length);
                    
                    audioSource.InjectPcm(safeCopy);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in manual Buffer parsing: {e.Message}");
        }
        
        // If we've tried everything and failed, generate a test tone
        Debug.LogWarning("Failed to extract audio data from JSON, generating test tone");
        audioSource.GenerateTestTone();
    }
    
    private byte[] CombineAudioChunks()
    {
        if (audioChunks.Count == 0 || expectedTotalChunks == 0)
        {
            return null;
        }
        
        // Check if we have all chunks
        for (int i = 0; i < expectedTotalChunks; i++)
        {
            if (!audioChunks.TryGetValue(i, out byte[] chunk) || chunk == null)
            {
                Debug.LogWarning($"Missing audio chunk {i}");
                return null;
            }
        }
        
        // Check if the first chunk is a WAV file
        byte[] firstChunk = audioChunks[0];
        bool isFirstChunkWav = firstChunk.Length > 44 && 
                           firstChunk[0] == 'R' && firstChunk[1] == 'I' && firstChunk[2] == 'F' && firstChunk[3] == 'F' &&
                           firstChunk[8] == 'W' && firstChunk[9] == 'A' && firstChunk[10] == 'V' && firstChunk[11] == 'E';
                           
        // If all chunks are complete WAV files, we'll use only the first chunk with its header
        // For other chunks we'll determine if they have WAV headers and skip them
        if (isFirstChunkWav && expectedTotalChunks > 1)
        {
            Debug.Log("This is a chunked WAV file");
            
            // If we have WAV files split into chunks, we need to:
            // 1. Find the data chunk in the first WAV
            // 2. Append all subsequent chunks' data after that
            
            // Find the data chunk position in the first WAV
            int dataChunkPos = -1;
            for (int i = 12; i < firstChunk.Length - 8; i++)
            {
                if (firstChunk[i] == 'd' && firstChunk[i + 1] == 'a' && 
                    firstChunk[i + 2] == 't' && firstChunk[i + 3] == 'a')
                {
                    dataChunkPos = i;
                    break;
                }
            }
            
            if (dataChunkPos == -1)
            {
                Debug.LogWarning("Could not find data chunk in WAV header, returning first chunk as-is");
                return firstChunk;
            }
            
            // Get the data size from the WAV header
            int originalDataSize = BitConverter.ToInt32(firstChunk, dataChunkPos + 4);
            int dataOffset = dataChunkPos + 8; // Skip "data" + size fields
            
            Debug.Log($"Found data chunk at position {dataChunkPos}, data size = {originalDataSize}, data offset = {dataOffset}");
            
            // Calculate the total size of all chunks' data
            int totalDataSize = 0;
            for (int i = 0; i < expectedTotalChunks; i++)
            {
                byte[] chunk = audioChunks[i];
                
                if (i == 0)
                {
                    // For first chunk, we only count the data portion
                    totalDataSize += chunk.Length - dataOffset;
                }
                else 
                {
                    // For subsequent chunks, check if they have WAV headers
                    bool isChunkWav = chunk.Length > 44 && 
                                  chunk[0] == 'R' && chunk[1] == 'I' && chunk[2] == 'F' && chunk[3] == 'F' &&
                                  chunk[8] == 'W' && chunk[9] == 'A' && chunk[10] == 'V' && chunk[11] == 'E';
                    
                    if (isChunkWav)
                    {
                        // Find the data chunk in this WAV chunk
                        int chunkDataPos = -1;
                        for (int j = 12; j < chunk.Length - 8; j++)
                        {
                            if (chunk[j] == 'd' && chunk[j + 1] == 'a' && 
                                chunk[j + 2] == 't' && chunk[j + 3] == 'a')
                            {
                                chunkDataPos = j;
                                break;
                            }
                        }
                        
                        if (chunkDataPos != -1)
                        {
                            int chunkDataOffset = chunkDataPos + 8;
                            totalDataSize += chunk.Length - chunkDataOffset;
                            Debug.Log($"Chunk {i}: Found data chunk at position {chunkDataPos}, adding {chunk.Length - chunkDataOffset} bytes");
                        }
                        else
                        {
                            totalDataSize += chunk.Length; // Add the whole chunk if we can't find the data segment
                            Debug.Log($"Chunk {i}: No data chunk found, adding all {chunk.Length} bytes");
                        }
                    }
                    else
                    {
                        totalDataSize += chunk.Length; // Add the whole chunk since it's not a WAV
                        Debug.Log($"Chunk {i}: Not a WAV file, adding all {chunk.Length} bytes");
                    }
                }
            }
            
            // Create a new buffer for the combined WAV
            int headerSize = dataOffset;
            int newFileSize = headerSize + totalDataSize;
            byte[] result = new byte[newFileSize];
            
            // Copy the header from the first chunk
            Buffer.BlockCopy(firstChunk, 0, result, 0, headerSize);
            
            // Update RIFF chunk size (file size - 8) 
            int riffSize = newFileSize - 8;
            byte[] riffSizeBytes = BitConverter.GetBytes(riffSize);
            Buffer.BlockCopy(riffSizeBytes, 0, result, 4, 4);
            
            // Update data chunk size
            byte[] dataSizeBytes = BitConverter.GetBytes(totalDataSize);
            Buffer.BlockCopy(dataSizeBytes, 0, result, dataChunkPos + 4, 4);
            
            // Copy data from chunks
            int destPos = headerSize;
            
            // Copy data from first chunk
            int firstChunkDataSize = firstChunk.Length - headerSize;
            Buffer.BlockCopy(firstChunk, headerSize, result, destPos, firstChunkDataSize);
            destPos += firstChunkDataSize;
            
            // Copy data from other chunks
            for (int i = 1; i < expectedTotalChunks; i++)
            {
                byte[] chunk = audioChunks[i];
                bool isChunkWav = chunk.Length > 44 && 
                               chunk[0] == 'R' && chunk[1] == 'I' && chunk[2] == 'F' && chunk[3] == 'F' &&
                               chunk[8] == 'W' && chunk[9] == 'A' && chunk[10] == 'V' && chunk[11] == 'E';
                
                int sourceOffset = 0;
                int copyLength = chunk.Length;
                
                if (isChunkWav)
                {
                    // Find the data chunk in this WAV chunk
                    int chunkDataPos = -1;
                    for (int j = 12; j < chunk.Length - 8; j++)
                    {
                        if (chunk[j] == 'd' && chunk[j + 1] == 'a' && 
                            chunk[j + 2] == 't' && chunk[j + 3] == 'a')
                        {
                            chunkDataPos = j;
                            break;
                        }
                    }
                    
                    if (chunkDataPos != -1)
                    {
                        sourceOffset = chunkDataPos + 8;
                        copyLength = chunk.Length - sourceOffset;
                    }
                }
                
                Buffer.BlockCopy(chunk, sourceOffset, result, destPos, copyLength);
                destPos += copyLength;
            }
            
            Debug.Log($"Combined {expectedTotalChunks} WAV chunks into a single WAV file of {result.Length} bytes");
            
            return result;
        }
        else
        {
            // Simple binary data - just concatenate the chunks
            // Calculate total size
            int totalSize = 0;
            for (int i = 0; i < expectedTotalChunks; i++)
            {
                totalSize += audioChunks[i].Length;
            }
            
            // Combine chunks
            byte[] result = new byte[totalSize];
            int position = 0;
            
            for (int i = 0; i < expectedTotalChunks; i++)
            {
                byte[] chunk = audioChunks[i];
                Buffer.BlockCopy(chunk, 0, result, position, chunk.Length);
                position += chunk.Length;
            }
            
            Debug.Log($"Combined {expectedTotalChunks} binary chunks into {totalSize} bytes");
            
            return result;
        }
    }
    
    public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
    {
        Debug.Assert(audioSource);

        // If the data is less than 100 bytes, then we have have received the audio info header
        if (data.data.Length < 100)
        {
            // Try to parse the data as a message, if it fails, then we have received the audio data
            Message message;
            try
            {
                message = data.FromJson<Message>();
                speechTargetName = message.targetPeer;
                Debug.Log("Received audio for peer: " + message.targetPeer + " with length: " + message.audioLength);
                return;
            }
            catch (Exception e)
            {
                Debug.Log("Received audio data");
            }
        }

        if (data.data.Length < 200)
        {
            return;
        }

        var speechUnit = new AssistantSpeechUnit();
        var prevUnit = speechUnits.Count > 0 ? speechUnits[speechUnits.Count-1] : null;
        speechUnit.startTime = prevUnit != null ? prevUnit.endTime : Time.time;
        speechUnit.samples = data.data.Length/2;
        speechUnit.speechTargetName = speechTargetName;
        speechUnits.Add(speechUnit);

        byte[] originalData = data.data.ToArray();
        
        // First check if this is JSON data
        if (originalData.Length > 0 && originalData[0] == '{')
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(originalData);
            Debug.Log("Detected JSON data: " + (jsonString.Length > 50 ? jsonString.Substring(0, 50) + "..." : jsonString));
            
            // Process the JSON audio data
            ProcessJsonAudio(jsonString);
            return;
        }
        
        // If it's not JSON, process as raw audio data
        if (originalData.Length > 12)
        {
            string headerInfo = "";
            if (originalData[0] == 'R' && originalData[1] == 'I' && originalData[2] == 'F' && originalData[3] == 'F' &&
                originalData[8] == 'W' && originalData[9] == 'A' && originalData[10] == 'V' && originalData[11] == 'E')
            {
                headerInfo = "WAV file detected (RIFF header)";
            }
            else
            {
                headerInfo = $"Raw data - First 4 bytes: {originalData[0]:X2} {originalData[1]:X2} {originalData[2]:X2} {originalData[3]:X2}";
            }
            
            Debug.Log($"Raw audio data to process: {originalData.Length} bytes. {headerInfo}");
            audioSource.InjectPcm(originalData);
        }
        else
        {
            Debug.LogWarning("Received data too small to be valid audio");
        }
    }
    
    // Called when a client wants to know if they are being spoken to
    public bool IsBeingSpokenTo(string peerName)
    {
        // Check if any active speech units match this peer's name
        foreach (var unit in speechUnits)
        {
            if (unit.speechTargetName == peerName)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Enable or disable processing of audio from the server
    public void SetServerAudioProcessingState(bool enabled)
    {
        isProcessingServerAudio = enabled;
        Debug.Log($"Server audio processing: {(enabled ? "enabled" : "disabled")}");
    }
    
    // Method for other scripts to check if agent is currently speaking
    public bool IsAgentSpeaking()
    {
        return speechUnits.Count > 0;
    }
}