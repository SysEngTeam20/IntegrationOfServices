using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;
using Ubiq.Messaging;
using Ubiq.Rooms;
using System.Reflection;

/// <summary>
/// Controls microphone input in WebRTC connections based on whether the conversational agent is speaking.
/// This script intercepts audio at the WebRTC level to prevent echo and feedback.
/// </summary>
public class WebRTCMicrophoneController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ConversationalAgentManager that determines when mic should be muted")]
    public ConversationalAgentManager agentManager;

    [Header("Settings")]
    [Tooltip("How often to check if the agent is speaking (in seconds)")]
    public float checkFrequency = 0.1f;
    
    [Tooltip("Enable debug logs for troubleshooting")]
    public bool debugLog = true;

    private VoipPeerConnectionManager peerConnectionManager;
    private List<VoipPeerConnection> managedConnections = new List<VoipPeerConnection>();
    private RoomClient roomClient;
    private bool lastSpeakingState = false;

    private void Start()
    {
        if (!agentManager)
        {
            agentManager = FindObjectOfType<ConversationalAgentManager>();
            if (!agentManager)
            {
                Debug.LogError("WebRTCMicrophoneController: No ConversationalAgentManager found");
                enabled = false;
                return;
            }
        }

        peerConnectionManager = GetComponentInParent<VoipPeerConnectionManager>();
        if (!peerConnectionManager)
        {
            peerConnectionManager = FindObjectOfType<VoipPeerConnectionManager>();
            if (!peerConnectionManager)
            {
                Debug.LogError("WebRTCMicrophoneController: No VoipPeerConnectionManager found");
                enabled = false;
                return;
            }
        }

        roomClient = GetComponentInParent<RoomClient>();
        if (!roomClient)
        {
            roomClient = FindObjectOfType<RoomClient>();
            if (!roomClient)
            {
                Debug.LogWarning("WebRTCMicrophoneController: No RoomClient found");
            }
        }

        // Subscribe to connection events
        peerConnectionManager.OnPeerConnection.AddListener(OnNewPeerConnection);

        // Start the checking coroutine
        StartCoroutine(CheckAgentSpeakingState());

        if (debugLog)
        {
            Debug.Log("WebRTCMicrophoneController initialized");
        }
    }

    private void OnDestroy()
    {
        // Make sure all connections are enabled when the script is destroyed
        foreach (var connection in managedConnections)
        {
            SetConnectionMicrophoneState(connection, true);
        }
        
        if (peerConnectionManager)
        {
            peerConnectionManager.OnPeerConnection.RemoveListener(OnNewPeerConnection);
        }
    }

    private void OnNewPeerConnection(VoipPeerConnection connection)
    {
        if (!managedConnections.Contains(connection))
        {
            managedConnections.Add(connection);
            
            // Apply current state to the new connection
            bool shouldProcessAudio = agentManager ? agentManager.ShouldProcessMicrophoneInput() : true;
            SetConnectionMicrophoneState(connection, shouldProcessAudio);
            
            if (debugLog)
            {
                Debug.Log($"WebRTCMicrophoneController: Managing new peer connection to {connection.PeerUuid}");
            }
        }
    }

    private IEnumerator CheckAgentSpeakingState()
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
                    Debug.Log($"WebRTCMicrophoneController: Microphone {(shouldProcessAudio ? "enabled" : "disabled")}");
                }
            }
        }
    }

    private void UpdateAllConnectionsState(bool enabled)
    {
        // Filter out any null connections that might be in the list
        managedConnections.RemoveAll(c => c == null);
        
        foreach (var connection in managedConnections)
        {
            SetConnectionMicrophoneState(connection, enabled);
        }
    }

    private void SetConnectionMicrophoneState(VoipPeerConnection connection, bool enabled)
    {
        if (connection == null) return;
        
        try
        {
            // Use reflection to access the WebRTC implementation
            // This gets the private field 'impl' which holds the WebRTC implementation
            var implField = typeof(VoipPeerConnection).GetField("impl", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (implField != null)
            {
                var impl = implField.GetValue(connection);
                if (impl != null)
                {
                    // See if we can find a method to enable/disable the microphone
                    var methodInfo = impl.GetType().GetMethod("SetMicrophoneActive", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(impl, new object[] { enabled });
                        if (debugLog)
                        {
                            Debug.Log($"WebRTCMicrophoneController: Set connection {connection.PeerUuid} microphone to {enabled}");
                        }
                        return;
                    }
                    
                    // If there's no direct method, try to find properties we can use
                    var property = impl.GetType().GetProperty("IsMicrophoneActive", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(impl, enabled);
                        if (debugLog)
                        {
                            Debug.Log($"WebRTCMicrophoneController: Set connection {connection.PeerUuid} microphone to {enabled} via property");
                        }
                        return;
                    }
                }
            }
            
            // If we can't directly control the microphone, we'll log a warning
            if (debugLog)
            {
                Debug.LogWarning("WebRTCMicrophoneController: Could not access WebRTC implementation to control microphone");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"WebRTCMicrophoneController: Error setting microphone state: {e.Message}");
        }
    }

    [ContextMenu("Enable Microphone")]
    public void EnableMicrophone()
    {
        UpdateAllConnectionsState(true);
        if (debugLog)
        {
            Debug.Log("WebRTCMicrophoneController: Manually enabled microphone");
        }
    }

    [ContextMenu("Disable Microphone")]
    public void DisableMicrophone()
    {
        UpdateAllConnectionsState(false);
        if (debugLog)
        {
            Debug.Log("WebRTCMicrophoneController: Manually disabled microphone");
        }
    }
} 