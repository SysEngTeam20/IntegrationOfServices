using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneConfig
{
    public string sceneId;
    public List<ObjectData> objects = new List<ObjectData>();
}

[Serializable]
public class ObjectData
{
    public string id;
    public string modelUrl;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale = Vector3.one;
}