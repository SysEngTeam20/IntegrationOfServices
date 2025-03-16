using UnityEngine;

// Data structure for individual scene objects
[System.Serializable]
public class SceneObjectData
{
    public string id;
    public string modelUrl; // URL to the .glb file
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}