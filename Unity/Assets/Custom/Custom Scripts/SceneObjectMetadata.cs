using UnityEngine;

// Stores metadata for imported objects to maintain links with scene data.
public class SceneObjectMetadata : MonoBehaviour
{
    public string objectId;  // Unique ID from JSON
    public string modelUrl;  // Source model URL

    // Initializes the metadata with the object's original data.
    public void Initialize(string id, string url)
    {
        objectId = id;
        modelUrl = url;
    }
}
