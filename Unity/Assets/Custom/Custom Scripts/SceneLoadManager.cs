using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;

public class SceneLoadManager : MonoBehaviour
{
    [Header("Configuration")]
    public string sceneConfigUrl = "https://example.com/scene-config.json";
    public GameObject networkObjectPrefab;
    
    [Header("Local Testing")]
    public bool useLocalTestFile = true; // Toggle for local testing
    public TextAsset localSceneConfigFile; // Assign in inspector
    
    [Header("EditorScene Integration")]
    public bool useEditorSceneFile = true; // Toggle for using the file exported by EditorScene
    public string editorSceneFilePath = "/Users/guangqianma/Desktop/M/SceneConfig.json"; // Path to the EditorScene exported file
    
    [Header("Loading Settings")]
    public float objectSpawnDelay = 0.1f; // Delay between spawning objects to prevent network congestion
    
    // Ubiq components
    private NetworkScene networkScene;
    private NetworkContext context;
    private RoomClient roomClient;
    
    // State tracking
    private bool isSceneLoaded = false;
    private string currentSceneId = "";
    private List<GameObject> spawnedObjects = new List<GameObject>();
    
    // This could go at the class level
    private string roomId;
    private bool checkedForMaster = false;
    private float masterCheckInterval = 5.0f; // Check every 5 seconds
    
    void Start()
    {
        if (networkObjectPrefab == null)
        {
            Debug.LogError("NetworkObjectPrefab not assigned!");
            return;
        }
        
        // Get Ubiq components
        networkScene = NetworkScene.Find(this);
        if (networkScene == null)
        {
            Debug.LogError("NetworkScene not found! Make sure this GameObject is a child of a NetworkScene.");
            return;
        }
        
        context = NetworkScene.Register(this);
        roomClient = GetComponentInParent<RoomClient>();
        
        if (roomClient == null)
        {
            Debug.LogError("RoomClient not found! Make sure this GameObject is a child of a RoomClient.");
            return;
        }
        
        // Subscribe to room events
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
    }
    
    void OnDestroy()
    {
        // Clean up event subscription
        if (roomClient != null)
        {
            roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
        }
    }
    
    void OnJoinedRoom(IRoom room)
    {
        Debug.Log("Joined room");
        
        // Store a unique identifier for this session
        roomId = System.Guid.NewGuid().ToString(); // Generate our own ID instead of using room.UUID
        
        // Start checking if we should be master
        StartCoroutine(CheckForMaster());
    }
    
    IEnumerator CheckForMaster()
    {
        while (true)
        {
            // Use Ubiq's roomClient API to detect other peers in the room
            bool anyOtherClientsConnected = false;
            
            if (roomClient != null && roomClient.Peers.Count() > 0)
            {
                // Other peers are present in the room
                anyOtherClientsConnected = true;
                Debug.Log($"Detected {roomClient.Peers.Count()} other client(s) in the room");
            }
            
            if (!anyOtherClientsConnected && !checkedForMaster)
            {
                // We're the only one here, make us master
                Debug.Log("No other clients detected, becoming master");
                checkedForMaster = true;
                StartCoroutine(LoadSceneConfig());
            }
            
            yield return new WaitForSeconds(masterCheckInterval);
        }
    }
    
    IEnumerator LoadSceneConfig()
    {
        string json = "";
        
        // First priority: Use EditorScene exported file if enabled
        if (useEditorSceneFile && !string.IsNullOrEmpty(editorSceneFilePath))
        {
            if (File.Exists(editorSceneFilePath))
            {
                try
                {
                    Debug.Log($"Loading scene config from EditorScene file: {editorSceneFilePath}");
                    json = File.ReadAllText(editorSceneFilePath);
                    ProcessSceneConfigJson(json);
                    yield break; // Exit early since we've successfully loaded the config
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load EditorScene file: {e.Message}");
                    // Continue to other methods if this fails
                }
            }
            else
            {
                Debug.LogWarning($"EditorScene file not found at path: {editorSceneFilePath}");
                // Continue to other methods if file doesn't exist
            }
        }
        
        // Second priority: Use local TextAsset if enabled
        if (useLocalTestFile && localSceneConfigFile != null)
        {
            // Load from local TextAsset
            Debug.Log("Loading scene config from local TextAsset");
            json = localSceneConfigFile.text;
            
            // Process the JSON directly
            ProcessSceneConfigJson(json);
        }
        else
        {
            // Last priority: Load from remote URL
            Debug.Log($"Downloading scene config from: {sceneConfigUrl}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(sceneConfigUrl))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    json = request.downloadHandler.text;
                    ProcessSceneConfigJson(json);
                }
                else
                {
                    Debug.LogError($"Failed to load scene config: {request.error}");
                }
            }
        }
    }
    
    // Helper method to process the JSON string
    private void ProcessSceneConfigJson(string json)
    {
        SceneConfig sceneConfig = null;
        
        try
        {
            sceneConfig = JsonUtility.FromJson<SceneConfig>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse scene config: {e.Message}");
            return;
        }
        
        if (sceneConfig != null)
        {
            Debug.Log($"Successfully loaded scene config: {sceneConfig.sceneId}");
            
            // Broadcast to room that we're loading this scene
            context.SendJson(new SceneLoadMessage { 
                sceneId = sceneConfig.sceneId,
                configJson = json
            });
            
            // Process locally too
            ProcessSceneConfig(sceneConfig);
        }
        else
        {
            Debug.LogError("Scene config is null after parsing");
        }
    }
    
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // Handle scene load messages
        var sceneMsg = message.FromJson<SceneLoadMessage>();
        if (!string.IsNullOrEmpty(sceneMsg.configJson))
        {
            Debug.Log($"Received scene configuration: {sceneMsg.sceneId}");
            SceneConfig sceneConfig = JsonUtility.FromJson<SceneConfig>(sceneMsg.configJson);
            ProcessSceneConfig(sceneConfig);
        }
    }
    
    void ProcessSceneConfig(SceneConfig sceneConfig)
    {
        // Check if we're already loaded or loading the same scene
        if (isSceneLoaded && sceneConfig.sceneId == currentSceneId)
        {
            Debug.Log($"Scene {sceneConfig.sceneId} already loaded");
            return;
        }
        
        // If we have a different scene loaded, clear it first
        if (isSceneLoaded && sceneConfig.sceneId != currentSceneId)
        {
            ClearCurrentScene();
        }
        
        Debug.Log($"Processing scene configuration: {sceneConfig.sceneId} with {sceneConfig.objects.Count} objects");
        currentSceneId = sceneConfig.sceneId;
        StartCoroutine(SpawnSceneObjects(sceneConfig));
        isSceneLoaded = true;
    }
    
    void ClearCurrentScene()
    {
        Debug.Log("Clearing current scene");
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        isSceneLoaded = false;
    }
    
    IEnumerator SpawnSceneObjects(SceneConfig sceneConfig)
    {
        Debug.Log($"Spawning {sceneConfig.objects.Count} objects");
        
        foreach (ObjectData objData in sceneConfig.objects)
        {
            // Create networked object container
            GameObject objContainer = Instantiate(networkObjectPrefab);
            spawnedObjects.Add(objContainer);
            
            // Initialize the networked object
            NetworkedSceneObject netObj = objContainer.GetComponent<NetworkedSceneObject>();
            if (netObj != null)
            {
                netObj.Initialize(
                    objData.id,
                    objData.modelUrl,
                    objData.position,
                    objData.rotation,
                    objData.scale
                );
                
                Debug.Log($"Spawned object: {objData.id} at position {objData.position}");
            }
            else
            {
                Debug.LogError("NetworkedSceneObject component not found on prefab!");
            }
            
            // Small delay to avoid overwhelming the network or CPU
            yield return new WaitForSeconds(objectSpawnDelay);
        }
        
        Debug.Log("Finished spawning all objects");
    }
    
    // Message structure for scene loading
    private struct SceneLoadMessage
    {
        public string sceneId;
        public string configJson;
    }
}
