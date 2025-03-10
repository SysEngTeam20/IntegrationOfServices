using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using System;

public class NetworkedSceneObject : MonoBehaviour, INetworkSpawnable
{
    // Required by INetworkSpawnable
    public NetworkId NetworkId { get; set; }
    
    private NetworkContext context;
    private Transform modelContainer;
    private string objectId;
    private string modelUrl;
    
    // Transform synchronization values
    private Vector3 lastSyncedPosition;
    private Quaternion lastSyncedRotation;
    private Vector3 lastSyncedScale;
    private float syncInterval = 0.1f;
    private float lastSyncTime = 0;
    
    // Whether this object can be interacted with
    public bool interactive = false;
    
    void Awake()
    {
        // Find or create model container
        modelContainer = transform.Find("ModelContainer");
        if (modelContainer == null)
        {
            GameObject container = new GameObject("ModelContainer");
            container.transform.SetParent(transform, false);
            modelContainer = container.transform;
        }
    }
    
    public void Initialize(string id, string url, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        objectId = id;
        modelUrl = url;
        
        // Set transform values
        transform.position = position;
        transform.eulerAngles = rotation;
        transform.localScale = scale;
        
        // Store initial transform state
        lastSyncedPosition = transform.position;
        lastSyncedRotation = transform.rotation;
        lastSyncedScale = transform.localScale;
        
        // Create a valid NetworkId using Ubiq's IdGenerator
        NetworkId = IdGenerator.GenerateFromName(objectId);
        
        Debug.Log($"Created NetworkId {NetworkId} for object {objectId}");
        
        // Register with Ubiq's networking
        context = NetworkScene.Register(this);
        
        // Start downloading the model
        StartCoroutine(ModelDownloader.DownloadAndAttachModel(modelUrl, modelContainer));
    }
    
    void Update()
    {
        // Only sync if interactive and we've moved
        if (!interactive) return;
        
        if (Time.time - lastSyncTime > syncInterval)
        {
            if (HasTransformChanged())
            {
                SyncTransform();
                lastSyncTime = Time.time;
            }
        }
    }
    
    private bool HasTransformChanged()
    {
        return Vector3.SqrMagnitude(transform.position - lastSyncedPosition) > 0.001f ||
               Quaternion.Angle(transform.rotation, lastSyncedRotation) > 0.5f ||
               Vector3.SqrMagnitude(transform.localScale - lastSyncedScale) > 0.001f;
    }
    
    private void SyncTransform()
    {
        // Update synced values
        lastSyncedPosition = transform.position;
        lastSyncedRotation = transform.rotation;
        lastSyncedScale = transform.localScale;
        
        // Send transform update over network
        context.SendJson(new TransformMessage
        {
            position = transform.position,
            rotation = transform.eulerAngles,
            scale = transform.localScale
        });
    }
    
    // This is called when a message is received for this object
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var transformMsg = message.FromJson<TransformMessage>();
        
        // Update transform based on network data
        transform.position = transformMsg.position;
        transform.eulerAngles = transformMsg.rotation;
        transform.localScale = transformMsg.scale;
        
        // Update synced values
        lastSyncedPosition = transform.position;
        lastSyncedRotation = transform.rotation;
        lastSyncedScale = transform.localScale;
    }
    
    // Transform message structure
    private struct TransformMessage
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}
