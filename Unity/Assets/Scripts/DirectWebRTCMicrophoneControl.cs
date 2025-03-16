using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;
using System.Reflection;

/// <summary>
/// A direct approach to controlling the microphone in WebRTC connections.
/// This script specifically targets the VoipPeerConnection objects without
/// any intermediary components or complex logic.
/// </summary>
public class DirectWebRTCMicrophoneControl : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ConversationalAgentManager that determines when mic should be muted")]
    public ConversationalAgentManager agentManager;

    [Header("Settings")]
    [Tooltip("How often to check if the agent is speaking (in seconds)")]
    public float checkFrequency = 0.1f;
    
    [Tooltip("Enable debug logs for troubleshooting")]
    public bool debugLog = true;
    
    [Tooltip("Immediately look for WebRTC components on startup")]
    public bool findComponentsOnStart = true;
    
    // State tracking
    private bool microphoneMuted = false;
    private bool lastSpeakingState = false;
    
    // Store all peer connections
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private VoipPeerConnectionManager peerConnectionManager;

    private void Start()
    {
        if (!agentManager)
        {
            agentManager = FindObjectOfType<ConversationalAgentManager>();
            if (!agentManager)
            {
                Debug.LogError("DirectWebRTCMicrophoneControl: No ConversationalAgentManager found!");
                enabled = false;
                return;
            }
        }
        
        // Find the peer connection manager and subscribe to new connections
        peerConnectionManager = FindObjectOfType<VoipPeerConnectionManager>();
        if (peerConnectionManager)
        {
            peerConnectionManager.OnPeerConnection.AddListener(OnNewPeerConnection);
            
            if (debugLog)
            {
                Debug.Log("DirectWebRTCMicrophoneControl: Found VoipPeerConnectionManager");
            }
        }
        else
        {
            Debug.LogWarning("DirectWebRTCMicrophoneControl: No VoipPeerConnectionManager found!");
        }
        
        if (findComponentsOnStart)
        {
            FindPeerConnections();
        }
        
        // Start the checking coroutine
        StartCoroutine(CheckSpeakingState());
        
        if (debugLog)
        {
            Debug.Log("DirectWebRTCMicrophoneControl initialized");
        }
    }
    
    private void OnDestroy()
    {
        // Re-enable microphone on all connections when destroyed
        UpdateAllConnectionsState(true);
        
        // Unsubscribe from peer connection events
        if (peerConnectionManager)
        {
            peerConnectionManager.OnPeerConnection.RemoveListener(OnNewPeerConnection);
        }
    }
    
    private void OnNewPeerConnection(VoipPeerConnection connection)
    {
        if (!peerConnections.Contains(connection))
        {
            peerConnections.Add(connection);
            
            // Apply current state to the new connection
            bool shouldProcessAudio = agentManager ? agentManager.ShouldProcessMicrophoneInput() : true;
            SetConnectionMicrophoneState(connection, shouldProcessAudio);
            
            if (debugLog)
            {
                Debug.Log($"DirectWebRTCMicrophoneControl: Managing new peer connection to {connection.PeerUuid}");
            }
        }
    }
    
    private void FindPeerConnections()
    {
        peerConnections.Clear();
        var connections = FindObjectsOfType<VoipPeerConnection>();
        
        foreach (var connection in connections)
        {
            if (!peerConnections.Contains(connection))
            {
                peerConnections.Add(connection);
                
                if (debugLog)
                {
                    Debug.Log($"DirectWebRTCMicrophoneControl: Found existing peer connection to {connection.PeerUuid}");
                }
            }
        }
        
        if (debugLog)
        {
            Debug.Log($"DirectWebRTCMicrophoneControl: Found {peerConnections.Count} peer connections");
        }
    }
    
    private IEnumerator CheckSpeakingState()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkFrequency);
            
            if (!agentManager) continue;
            
            bool shouldProcessAudio = agentManager.ShouldProcessMicrophoneInput();
            
            // Only update if the state has changed
            if (shouldProcessAudio != lastSpeakingState)
            {
                UpdateAllConnectionsState(shouldProcessAudio);
                lastSpeakingState = shouldProcessAudio;
                
                if (debugLog)
                {
                    Debug.Log($"DirectWebRTCMicrophoneControl: Microphone {(shouldProcessAudio ? "enabled" : "disabled")}");
                }
            }
        }
    }
    
    private void UpdateAllConnectionsState(bool enabled)
    {
        // Filter out any null connections
        peerConnections.RemoveAll(c => c == null);
        
        foreach (var connection in peerConnections)
        {
            SetConnectionMicrophoneState(connection, enabled);
        }
        
        // If we changed the microphone state, update our tracking
        microphoneMuted = !enabled;
    }
    
    private void SetConnectionMicrophoneState(VoipPeerConnection connection, bool enabled)
    {
        if (connection == null) return;
        
        try
        {
            // First try direct reflection to access the WebRTC implementation
            var implField = typeof(VoipPeerConnection).GetField("impl", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (implField != null)
            {
                var impl = implField.GetValue(connection);
                if (impl != null)
                {
                    // Try multiple methods that might control the microphone
                    var methodNames = new string[] { 
                        "SetMicrophoneActive", 
                        "EnableMicrophone", 
                        "SetMicrophoneEnabled",
                        "SetAudioEnabled" 
                    };
                    
                    bool methodFound = false;
                    foreach (var methodName in methodNames)
                    {
                        var methodInfo = impl.GetType().GetMethod(methodName, 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                        if (methodInfo != null)
                        {
                            methodInfo.Invoke(impl, new object[] { enabled });
                            
                            if (debugLog)
                            {
                                Debug.Log($"DirectWebRTCMicrophoneControl: Set connection {connection.PeerUuid} microphone to {enabled} via method {methodName}");
                            }
                            
                            methodFound = true;
                            break;
                        }
                    }
                    
                    // If we couldn't find a method, try finding a property
                    if (!methodFound)
                    {
                        var propertyNames = new string[] {
                            "IsMicrophoneActive",
                            "MicrophoneEnabled",
                            "AudioEnabled"
                        };
                        
                        foreach (var propertyName in propertyNames)
                        {
                            var property = impl.GetType().GetProperty(propertyName, 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                
                            if (property != null && property.CanWrite)
                            {
                                property.SetValue(impl, enabled);
                                
                                if (debugLog)
                                {
                                    Debug.Log($"DirectWebRTCMicrophoneControl: Set connection {connection.PeerUuid} microphone to {enabled} via property {propertyName}");
                                }
                                
                                methodFound = true;
                                break;
                            }
                        }
                    }
                    
                    if (!methodFound && debugLog)
                    {
                        Debug.LogWarning($"DirectWebRTCMicrophoneControl: Could not find a way to control microphone for connection {connection.PeerUuid}");
                    }
                }
            }
            else if (debugLog)
            {
                Debug.LogWarning("DirectWebRTCMicrophoneControl: Could not access WebRTC implementation field");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DirectWebRTCMicrophoneControl: Error setting microphone state: {e.Message}");
        }
    }
    
    [ContextMenu("Force Enable Microphone")]
    public void EnableMicrophone()
    {
        UpdateAllConnectionsState(true);
        Debug.Log("DirectWebRTCMicrophoneControl: Manually enabled microphone");
    }
    
    [ContextMenu("Force Disable Microphone")]
    public void DisableMicrophone()
    {
        UpdateAllConnectionsState(false);
        Debug.Log("DirectWebRTCMicrophoneControl: Manually disabled microphone");
    }
    
    [ContextMenu("Find Peer Connections")]
    public void RefreshPeerConnections()
    {
        FindPeerConnections();
    }
    
    [ContextMenu("Log Microphone State")]
    public void LogMicrophoneState()
    {
        string state = "=== MICROPHONE STATE ===\n";
        state += $"Muted: {microphoneMuted}\n";
        state += $"Last agent speaking state: {!lastSpeakingState}\n";
        state += $"Managing {peerConnections.Count} peer connections:\n";
        
        foreach (var connection in peerConnections)
        {
            if (connection != null)
            {
                state += $"- Connection to {connection.PeerUuid}\n";
            }
        }
        
        Debug.Log(state);
    }
} 