using System.Collections.Generic;
using UnityEngine;

// *** ACTIVITY DATA STRUCTURES ***

// List of activities (for /api/activities)
[System.Serializable]
public class ActivitiesList {
    public List<ActivityData> items;
}

// API Response wrapper for activities
[System.Serializable]
public class ActivityListResponse {
    public List<ActivityInfo> data;
    public bool success;
    public string message;
}

// Info about an activity from API response
[System.Serializable]
public class ActivityInfo {
    public string id;
    public string name;
    public string description;
    public string createdAt;
    public string updatedAt;
}

// API Response wrapper for scenes
[System.Serializable]
public class SceneListResponse {
    public List<SceneConfiguration> data;
    public bool success;
    public string message;
}

// Main activity data (from /api/activities/{activityId})
[System.Serializable]
public class ActivityData {
    public string _id;
    public string title;
    public string description;
    public string bannerUrl;
    public string format;  // "AR" or "VR"
    public string platform;  // "headset" or "web"
    public string orgId;
    public List<SceneInfo> scenes;
    public string createdAt;
    public string updatedAt;
}

// Brief scene information in activity response
[System.Serializable]
public class SceneInfo {
    public string id;
    public string name;
    public int order;
    public SceneConfigWrapper config;
}

// Wrapper for the empty objects array in activity response
[System.Serializable]
public class SceneConfigWrapper {
    public List<object> objects = new List<object>();
}

// *** SCENE CONFIGURATION DATA STRUCTURES ***

// Full scene configuration (from /api/scenes-configuration/{sceneId})
[System.Serializable]
public class SceneConfiguration {
    public string scene_id;
    public List<SceneObject> objects;
    public string orgId;  // Required for API requests
    public string createdAt;
    public string updatedAt;
    public string _id;
    public string id;  // Alternative ID field
    public string name; // Name of the scene
}

// Individual 3D object data
[System.Serializable]
public class SceneObject {
    public string object_id;
    public string modelUrl;
    public Vector3Serializable position;
    public Vector3Serializable rotation;
    public Vector3Serializable scale;
}

// *** ASSET DATA STRUCTURES ***

// List of assets (for /api/assets)
[System.Serializable]
public class AssetsList {
    public List<AssetData> items;
}

// Individual asset data
[System.Serializable]
public class AssetData {
    public string _id;
    public string name;
    public string type;
    public long size;
    public string url;
    public string orgId;
    public string createdAt;
    public string updatedAt;
    public bool isDocument;
    
    // Helper property to get the full download URL
    public string GetFullUrl(string baseStorageUrl) => $"{baseStorageUrl}/{url}";
}

// *** UTILITY DATA STRUCTURES ***

// Serializable Vector3 for JSON compatibility
[System.Serializable]
public class Vector3Serializable {
    public float x;
    public float y;
    public float z;
    
    // Conversion methods to/from Unity Vector3
    public static Vector3Serializable FromVector3(Vector3 v) => new Vector3Serializable { x = v.x, y = v.y, z = v.z };
    public Vector3 ToVector3() => new Vector3(x, y, z);
}
