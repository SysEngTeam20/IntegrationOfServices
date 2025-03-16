using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;
using System.Reflection;

/// <summary>
/// Alternative approach that directly disables WebRTC components when the agent is speaking.
/// This script searches for audio-related components in the scene and disables them when needed.
/// </summary>
public class WebRTCComponentMuter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ConversationalAgentManager that determines when mic should be muted")]
    public ConversationalAgentManager agentManager;
    
    [Tooltip("Reference to the InjectableAudioSource of the agent - this will be excluded from muting")]
    public InjectableAudioSource agentAudioSource;

    [Header("Settings")]
    [Tooltip("How often to check if the agent is speaking (in seconds)")]
    public float checkFrequency = 0.1f;
    
    [Tooltip("Enable debug logs for troubleshooting")]
    public bool debugLog = true;
    
    [Tooltip("Search entire scene hierarchy for components (if false, only searches common WebRTC parent objects)")]
    public bool searchEntireScene = false;
    
    [Tooltip("Delay in seconds after re-enabling components to let WebRTC initialize")]
    public float reinitializationDelay = 0.5f;
    
    [Tooltip("Try to reinitialize certain components when enabling")]
    public bool attemptReinitialization = true;
    
    [Tooltip("GameObject name fragments to exclude from disabling (e.g., Agent, Assistant)")]
    public string[] excludeNameContains = new string[] { "Agent", "Assistant", "Virtual", "Inject", "Playback" };

    // Components that will be disabled when agent is speaking
    private List<Component> audioInputComponents = new List<Component>();
    private Dictionary<Component, bool> originalStates = new Dictionary<Component, bool>();
    private List<GameObject> excludedObjects = new List<GameObject>();
    private bool lastSpeakingState = false;
    private bool initialized = false;
    private bool isReinitializing = false;

    // Component type names that are likely to be involved in audio capture (INPUT only)
    private readonly string[] audioInputComponentTypeNames = new string[]
    {
        // Input/microphone related types
        "MicrophoneCapture",
        "AudioCapture",
        "VoipPeerConnectionInputSender",
        "AudioInput",
        "MicrophoneInput",
        "WebRTCInput",
        "PeerConnectionMicrophone",
        "VoipInput",
        "RecordingInput"
    };
    
    // Component type names that are likely output-related and should be avoided
    private readonly string[] audioOutputComponentTypeNames = new string[]
    {
        "InjectableAudioSource",
        "AudioSourceSpeechIndicator",
        "AudioSourceVolume",
        "AudioOutput",
        "Speaker",
        "AgentAudio",
        "Playback"
    };

    private void Start()
    {
        if (!agentManager)
        {
            agentManager = FindObjectOfType<ConversationalAgentManager>();
            if (!agentManager)
            {
                Debug.LogError("WebRTCComponentMuter: No ConversationalAgentManager found");
                enabled = false;
                return;
            }
        }
        
        // If agentAudioSource is not set, try to find it
        if (!agentAudioSource && agentManager)
        {
            agentAudioSource = agentManager.GetComponent<InjectableAudioSource>();
            if (!agentAudioSource)
            {
                agentAudioSource = agentManager.GetComponentInChildren<InjectableAudioSource>();
                if (!agentAudioSource)
                {
                    agentAudioSource = FindObjectOfType<InjectableAudioSource>();
                }
            }
            
            if (agentAudioSource && debugLog)
            {
                Debug.Log($"WebRTCComponentMuter: Found agent audio source: {agentAudioSource.name}");
            }
        }
        
        // Build the excluded objects list
        BuildExcludedObjectsList();

        // Don't immediately find components - wait until first use
        // to ensure all WebRTC components are fully initialized
        StartCoroutine(InitializeWithDelay(1.0f));
        
        // Start the checking coroutine
        StartCoroutine(CheckAgentSpeakingState());
        
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter initialized");
        }
    }
    
    private void BuildExcludedObjectsList()
    {
        excludedObjects.Clear();
        
        // Add agent audio source and its parent if available
        if (agentAudioSource)
        {
            excludedObjects.Add(agentAudioSource.gameObject);
            if (agentAudioSource.transform.parent)
            {
                excludedObjects.Add(agentAudioSource.transform.parent.gameObject);
            }
        }
        
        // Add agent manager and its parent if available
        if (agentManager)
        {
            excludedObjects.Add(agentManager.gameObject);
            if (agentManager.transform.parent)
            {
                excludedObjects.Add(agentManager.transform.parent.gameObject);
            }
        }
        
        if (debugLog)
        {
            Debug.Log($"WebRTCComponentMuter: Added {excludedObjects.Count} GameObjects to exclusion list");
        }
    }

    private IEnumerator InitializeWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FindAudioComponents();
        initialized = true;
    }

    private void OnDestroy()
    {
        // Make sure all components are re-enabled when the script is destroyed
        RestoreOriginalStates();
    }

    private void FindAudioComponents()
    {
        audioInputComponents.Clear();
        originalStates.Clear();
        
        if (searchEntireScene)
        {
            // Search all objects in scene
            var allObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in allObjects)
            {
                if (IsAudioInputComponent(component) && !IsExcluded(component.gameObject))
                {
                    AddAudioComponent(component);
                }
            }
        }
        else
        {
            // Look in common WebRTC parent objects
            var voipManagers = FindObjectsOfType<VoipPeerConnectionManager>();
            foreach (var manager in voipManagers)
            {
                SearchGameObjectForAudioComponents(manager.gameObject);
            }
            
            // Also look in NetworkScene objects
            var networkScenes = FindObjectsOfType<Ubiq.Messaging.NetworkScene>();
            foreach (var scene in networkScenes)
            {
                SearchGameObjectForAudioComponents(scene.gameObject);
            }
            
            // Look for VoipPeerConnection objects
            var peerConnections = FindObjectsOfType<VoipPeerConnection>();
            foreach (var connection in peerConnections)
            {
                SearchGameObjectForAudioComponents(connection.gameObject);
            }
        }
        
        if (debugLog)
        {
            Debug.Log($"WebRTCComponentMuter: Found {audioInputComponents.Count} audio INPUT components to manage");
            foreach (var component in audioInputComponents)
            {
                Debug.Log($"- {component.GetType().Name} on {component.gameObject.name}");
            }
        }
    }
    
    private void SearchGameObjectForAudioComponents(GameObject go)
    {
        if (IsExcluded(go)) return;
        
        var components = go.GetComponentsInChildren<Component>(true);
        foreach (var component in components)
        {
            if (component != null && IsAudioInputComponent(component) && !IsExcluded(component.gameObject))
            {
                AddAudioComponent(component);
            }
        }
    }
    
    private bool IsAudioInputComponent(Component component)
    {
        if (component == null) return false;
        
        string typeName = component.GetType().Name;
        
        // First check if it's in our output exclusion list
        foreach (var outputTypeName in audioOutputComponentTypeNames)
        {
            if (typeName.Contains(outputTypeName))
            {
                return false; // This is an output component, don't include it
            }
        }
        
        // Then check if it's an input component
        foreach (var inputTypeName in audioInputComponentTypeNames)
        {
            if (typeName.Contains(inputTypeName))
            {
                return true;
            }
        }
        
        // For AudioSource components, only include those likely to be microphone-related
        if (typeName.Contains("AudioSource"))
        {
            string gameObjectName = component.gameObject.name.ToLower();
            return gameObjectName.Contains("mic") || 
                   gameObjectName.Contains("input") || 
                   gameObjectName.Contains("recording") ||
                   gameObjectName.Contains("capture");
        }
        
        return false;
    }
    
    private bool IsExcluded(GameObject go)
    {
        if (go == null) return true;
        
        // Check if it's in our explicit excluded list
        if (excludedObjects.Contains(go)) return true;
        
        // Check if the name contains any of our exclusion terms
        string goName = go.name.ToLower();
        foreach (var excludeName in excludeNameContains)
        {
            if (!string.IsNullOrEmpty(excludeName) && goName.Contains(excludeName.ToLower()))
            {
                return true;
            }
        }
        
        // Also check its parent tree
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            if (excludedObjects.Contains(parent.gameObject)) return true;
            
            string parentName = parent.name.ToLower();
            foreach (var excludeName in excludeNameContains)
            {
                if (!string.IsNullOrEmpty(excludeName) && parentName.Contains(excludeName.ToLower()))
                {
                    return true;
                }
            }
            
            parent = parent.parent;
        }
        
        return false;
    }
    
    private void AddAudioComponent(Component component)
    {
        if (!audioInputComponents.Contains(component))
        {
            audioInputComponents.Add(component);
            
            // Store the original state (enabled/disabled)
            var behaviour = component as Behaviour;
            if (behaviour != null)
            {
                originalStates[component] = behaviour.enabled;
            }
        }
    }

    private IEnumerator CheckAgentSpeakingState()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkFrequency);
            
            if (!agentManager) continue;
            if (!initialized) continue;
            if (isReinitializing) continue; // Skip checks during reinitialization
            
            bool shouldProcessAudio = agentManager.ShouldProcessMicrophoneInput();
            
            // Only update if the state has changed
            if (shouldProcessAudio != lastSpeakingState)
            {
                if (shouldProcessAudio)
                {
                    EnableAudioComponents();
                }
                else
                {
                    DisableAudioComponents();
                }
                
                lastSpeakingState = shouldProcessAudio;
                
                if (debugLog)
                {
                    Debug.Log($"WebRTCComponentMuter: Microphone components {(shouldProcessAudio ? "enabled" : "disabled")}");
                }
            }
            
            // Debug monitoring - check if agent audio source is still enabled
            if (debugLog && agentAudioSource != null)
            {
                AudioSource audioSource = agentAudioSource.GetComponent<AudioSource>();
                if (audioSource != null && !audioSource.enabled && !shouldProcessAudio)
                {
                    Debug.LogWarning("WebRTCComponentMuter: Agent AudioSource was disabled, re-enabling it!");
                    audioSource.enabled = true;
                }
            }
        }
    }
    
    private void DisableAudioComponents()
    {
        foreach (var component in audioInputComponents)
        {
            if (component == null) continue;
            if (IsExcluded(component.gameObject)) continue;
            
            var behaviour = component as Behaviour;
            if (behaviour != null)
            {
                // Store the current state before changing it
                originalStates[component] = behaviour.enabled;
                behaviour.enabled = false;
                
                if (debugLog)
                {
                    Debug.Log($"WebRTCComponentMuter: Disabled {component.GetType().Name} on {component.gameObject.name}");
                }
            }
        }
    }
    
    private void EnableAudioComponents()
    {
        StartCoroutine(EnableAudioComponentsWithReinitialization());
    }
    
    private IEnumerator EnableAudioComponentsWithReinitialization()
    {
        isReinitializing = true;
        
        // First, re-enable all components
        foreach (var component in audioInputComponents)
        {
            if (component == null) continue;
            
            var behaviour = component as Behaviour;
            if (behaviour != null)
            {
                // Restore the original state
                if (originalStates.TryGetValue(component, out bool originalState))
                {
                    behaviour.enabled = originalState;
                    
                    if (debugLog && originalState)
                    {
                        Debug.Log($"WebRTCComponentMuter: Re-enabled {component.GetType().Name} on {component.gameObject.name}");
                    }
                }
            }
        }
        
        // Wait for components to initialize
        yield return new WaitForSeconds(reinitializationDelay);
        
        // Try to reinitialize certain components if needed
        if (attemptReinitialization)
        {
            ReinitializeAudioComponents();
        }
        
        // One more short delay to allow everything to settle
        yield return new WaitForSeconds(0.2f);
        
        isReinitializing = false;
        
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter: Audio component reinitialization completed");
        }
    }
    
    private void ReinitializeAudioComponents()
    {
        // First try to reinitialize any VoipPeerConnectionManager components
        var voipManagers = FindObjectsOfType<VoipPeerConnectionManager>();
        foreach (var manager in voipManagers)
        {
            if (!IsExcluded(manager.gameObject))
            {
                CallReinitializationMethod(manager, "ReinitializeMicrophone", "ReinitializeAudio", "Restart", "ResetConnection");
            }
        }
        
        // Try to reinitialize VoipPeerConnection components
        var peerConnections = FindObjectsOfType<VoipPeerConnection>();
        foreach (var connection in peerConnections)
        {
            if (!IsExcluded(connection.gameObject))
            {
                CallReinitializationMethod(connection, "ReinitializeMicrophone", "ReinitializeAudio", "Restart", "ResetConnection");
            }
        }
        
        // Try to reinitialize any other audio components with specific methods
        foreach (var component in audioInputComponents)
        {
            if (component == null || IsExcluded(component.gameObject)) continue;
            
            CallReinitializationMethod(component, "ReinitializeMicrophone", "ReinitializeAudio", "Restart", "ResetConnection", "Init", "Initialize");
        }
        
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter: Attempted to reinitialize audio components");
        }
    }
    
    private void CallReinitializationMethod(Component component, params string[] methodNames)
    {
        if (component == null) return;
        
        Type type = component.GetType();
        
        foreach (string methodName in methodNames)
        {
            MethodInfo method = type.GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (method != null)
            {
                try
                {
                    method.Invoke(component, null);
                    if (debugLog)
                    {
                        Debug.Log($"WebRTCComponentMuter: Called {methodName}() on {component.GetType().Name}");
                    }
                    return; // Successfully called a method, so we're done with this component
                }
                catch (Exception e)
                {
                    if (debugLog)
                    {
                        Debug.LogWarning($"WebRTCComponentMuter: Error calling {methodName}() on {component.GetType().Name}: {e.Message}");
                    }
                }
            }
        }
    }
    
    private void RestoreOriginalStates()
    {
        foreach (var component in audioInputComponents)
        {
            if (component == null) continue;
            
            var behaviour = component as Behaviour;
            if (behaviour != null)
            {
                // Restore the original state
                if (originalStates.TryGetValue(component, out bool originalState))
                {
                    behaviour.enabled = originalState;
                }
            }
        }
    }

    [ContextMenu("Refresh Audio Components")]
    public void RefreshAudioComponents()
    {
        FindAudioComponents();
    }

    [ContextMenu("Enable Audio Components")]
    public void ManuallyEnableAudioComponents()
    {
        EnableAudioComponents();
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter: Manually enabled audio components");
        }
    }

    [ContextMenu("Disable Audio Components")]
    public void ManuallyDisableAudioComponents()
    {
        DisableAudioComponents();
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter: Manually disabled audio components");
        }
    }
    
    [ContextMenu("Force Reinitialize Audio")]
    public void ForceReinitializeAudio()
    {
        StartCoroutine(ForceReinitializeAudioCoroutine());
    }
    
    private IEnumerator ForceReinitializeAudioCoroutine()
    {
        if (debugLog)
        {
            Debug.Log("WebRTCComponentMuter: Forcing audio reinitialization...");
        }
        
        // First disable all components
        DisableAudioComponents();
        
        // Wait a moment
        yield return new WaitForSeconds(0.5f);
        
        // Then re-enable with reinitialization
        EnableAudioComponents();
    }
}