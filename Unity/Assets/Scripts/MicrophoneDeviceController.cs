using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls microphone muting at the Unity API level by starting/stopping 
/// microphone recording when the agent is speaking.
/// This offers a third approach to solving the echo problem.
/// </summary>
public class MicrophoneDeviceController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ConversationalAgentManager that determines when mic should be muted")]
    public ConversationalAgentManager agentManager;

    [Header("Settings")]
    [Tooltip("How often to check if the agent is speaking (in seconds)")]
    public float checkFrequency = 0.1f;
    
    [Tooltip("Enable debug logs for troubleshooting")]
    public bool debugLog = true;
    
    [Tooltip("Name of the microphone device to use. Leave empty to use default device.")]
    public string microphoneDeviceName = "";
    
    [Tooltip("Whether to use a dummy AudioClip to keep audio recording active even when muted")]
    public bool useDummyClip = true;
    
    [Tooltip("Sample rate to use for recording (Hz)")]
    public int sampleRate = 44100;
    
    private AudioClip dummyClip;
    private bool isRecording = false;
    private bool lastSpeakingState = false;
    private string currentDeviceName;
    
    // For diagnostics
    public bool IsMicrophoneActive => isRecording && Microphone.IsRecording(currentDeviceName);

    private void Start()
    {
        if (!agentManager)
        {
            agentManager = FindObjectOfType<ConversationalAgentManager>();
            if (!agentManager)
            {
                Debug.LogError("MicrophoneDeviceController: No ConversationalAgentManager found");
                enabled = false;
                return;
            }
        }
        
        // Start the checking coroutine
        StartCoroutine(CheckAgentSpeakingState());
        
        // Create a dummy audio clip if needed
        if (useDummyClip)
        {
            // 1 second dummy clip
            dummyClip = AudioClip.Create("DummyClip", sampleRate, 1, sampleRate, false);
        }
        
        if (debugLog)
        {
            LogAvailableMicrophones();
            Debug.Log("MicrophoneDeviceController initialized");
        }
    }

    private void OnDestroy()
    {
        // Make sure microphone is stopped when this script is destroyed
        StopRecording();
    }
    
    private void LogAvailableMicrophones()
    {
        string[] devices = Microphone.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("MicrophoneDeviceController: No microphone devices detected!");
        }
        else
        {
            string deviceList = "Available microphones:";
            foreach (string device in devices)
            {
                deviceList += $"\n- \"{device}\"";
            }
            Debug.Log(deviceList);
        }
    }

    private IEnumerator CheckAgentSpeakingState()
    {
        yield return new WaitForSeconds(1.0f); // Initial delay
        
        // Start recording initially
        StartRecording();
        
        while (true)
        {
            yield return new WaitForSeconds(checkFrequency);
            
            if (!agentManager) continue;
            
            bool shouldProcessAudio = agentManager.ShouldProcessMicrophoneInput();
            
            // Only update if the state has changed
            if (shouldProcessAudio != lastSpeakingState)
            {
                if (shouldProcessAudio)
                {
                    StartRecording();
                }
                else
                {
                    StopRecording();
                }
                
                lastSpeakingState = shouldProcessAudio;
                
                if (debugLog)
                {
                    Debug.Log($"MicrophoneDeviceController: Microphone {(shouldProcessAudio ? "started" : "stopped")}");
                }
            }
            
            // Verify that microphone state is what we expect
            if (debugLog && (isRecording != Microphone.IsRecording(currentDeviceName)))
            {
                Debug.LogWarning($"MicrophoneDeviceController: Expected mic recording state {isRecording} but actual state is {Microphone.IsRecording(currentDeviceName)}");
            }
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return;
        
        // If no specific device is specified, use the default (first) device
        string deviceToUse = string.IsNullOrEmpty(microphoneDeviceName) ? null : microphoneDeviceName;
        
        // Check if the requested device exists
        if (!string.IsNullOrEmpty(deviceToUse))
        {
            bool deviceFound = false;
            foreach (string device in Microphone.devices)
            {
                if (device == deviceToUse)
                {
                    deviceFound = true;
                    break;
                }
            }
            
            if (!deviceFound)
            {
                Debug.LogWarning($"MicrophoneDeviceController: Specified device '{deviceToUse}' not found. Using default device.");
                deviceToUse = null;
            }
        }
        
        try
        {
            if (useDummyClip)
            {
                Microphone.Start(deviceToUse, true, 1, sampleRate);
            }
            else
            {
                // No need for a clip, just start the microphone
                Microphone.Start(deviceToUse, true, 10, sampleRate);
            }
            
            isRecording = true;
            currentDeviceName = deviceToUse;
            
            if (debugLog)
            {
                Debug.Log($"MicrophoneDeviceController: Started recording on device '{(string.IsNullOrEmpty(deviceToUse) ? "(default)" : deviceToUse)}'");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MicrophoneDeviceController: Error starting microphone: {e.Message}");
            isRecording = false;
        }
    }
    
    public void StopRecording()
    {
        if (!isRecording) return;
        
        try
        {
            Microphone.End(currentDeviceName);
            isRecording = false;
            
            if (debugLog)
            {
                Debug.Log($"MicrophoneDeviceController: Stopped recording on device '{(string.IsNullOrEmpty(currentDeviceName) ? "(default)" : currentDeviceName)}'");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MicrophoneDeviceController: Error stopping microphone: {e.Message}");
        }
    }

    [ContextMenu("Start Recording")]
    public void ManuallyStartRecording()
    {
        StartRecording();
    }

    [ContextMenu("Stop Recording")]
    public void ManuallyStopRecording()
    {
        StopRecording();
    }
    
    [ContextMenu("Log Microphone Status")]
    public void LogMicrophoneStatus()
    {
        string[] devices = Microphone.devices;
        string status = "Microphone status:";
        
        foreach (string device in devices)
        {
            bool isActive = Microphone.IsRecording(device);
            status += $"\n- '{device}': {(isActive ? "RECORDING" : "NOT RECORDING")}";
        }
        
        status += $"\n\nController status: {(isRecording ? "RECORDING" : "NOT RECORDING")}";
        status += $"\nCurrent device: '{(string.IsNullOrEmpty(currentDeviceName) ? "(default)" : currentDeviceName)}'";
        
        Debug.Log(status);
    }
} 