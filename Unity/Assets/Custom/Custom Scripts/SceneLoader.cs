using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility;

// Handles loading scene data from JSON file or server
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string sceneUrl = ""; // Replace with real backend URL when implemented
    private SceneData loadedSceneData; // Stores the loaded JSON data for exporting

    [Header("Local SceneConfig File")]
    public bool useLocalSceneConfigFile = true; // Toggle for using the local SceneConfig file
    public string localSceneConfigFilePath = "/Users/guangqianma/Desktop/M/SceneConfig.json"; // Path to the local SceneConfig file

    private async void Start()
    {
        await LoadSceneFromJson(sceneUrl);
    }

    // Load scene from JSON configuration
    private async Task LoadSceneFromJson(string jsonUrl)
    {
        try
        {
            /* 
            // BACKEND INTEGRATION (NOT YET IMPLEMENTED)
            // Download scene configuration from server
            string json;
            if (!string.IsNullOrEmpty(jsonUrl))
            {
                Debug.Log($"Downloading scene configuration from: {jsonUrl}");
                json = await DownloadJsonFromServerAsync(jsonUrl);
            }
            else
            {
                Debug.LogWarning("No scene URL provided, using default scene");
                json = await DownloadJsonFromServerAsync("backend scene default");
            }
            */

            // Sample JSON data (remove this when backend is implemented)
            string json;
            
            if (File.Exists(localSceneConfigFilePath))
            {
                json = File.ReadAllText(localSceneConfigFilePath);
                Debug.Log($"Loaded scene config from: {localSceneConfigFilePath}");
            }
            else
            {
                Debug.LogError($"Scene config file not found at: {localSceneConfigFilePath}");
                return;
            }

            loadedSceneData = JsonUtility.FromJson<SceneData>(json);

            if (loadedSceneData == null || loadedSceneData.objects.Count == 0)
            {
                Debug.LogError("No objects found in scene data.");
                return;
            }

            Debug.Log($"Loading scene: {loadedSceneData.sceneId} with {loadedSceneData.objects.Count} objects");

            // Load all objects in parallel
            List<Task> loadingTasks = new List<Task>();
            foreach (SceneObjectData obj in loadedSceneData.objects)
            {
                loadingTasks.Add(LoadGltfModelAsync(obj));
            }

            await Task.WhenAll(loadingTasks);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
        }
    }

    // Load a single GLTF model
    private async Task LoadGltfModelAsync(SceneObjectData objData)
    {
        try
        {
            Debug.Log($"Downloading {objData.modelUrl}");

            // Download model data
            byte[] glbData = await DownloadBinaryFromServerAsync(objData.modelUrl);

            // Import the model
            GameObject model = Importer.LoadFromBytes(glbData);
            if (model == null)
            {
                Debug.LogError($"Failed to parse model: {objData.modelUrl}");
                return;
            }

            // Check for existing mesh components in the hierarchy
            MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
            MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            bool hasMeshComponents = meshFilters.Length > 0;
            bool hasRenderers = meshRenderers.Length > 0 || skinnedMeshes.Length > 0;
            
            // Set up root object for object selector - needs to be on the correct layer
            model.layer = LayerMask.NameToLayer("Selectable") != -1 ? 
                LayerMask.NameToLayer("Selectable") : 0; // Default to 0 if layer doesn't exist
            
            // If no mesh components found, create a fallback
            if (!hasMeshComponents || !hasRenderers)
            {
                Debug.LogWarning($"Missing components in model: {objData.id}. MeshFilters: {meshFilters.Length}, " +
                                $"Renderers: {(meshRenderers.Length + skinnedMeshes.Length)}. Adding fallback components.");
                
                // Check if we need to add components to the root or a child
                GameObject targetObject;
                
                // If model has no children, add components directly to it
                if (model.transform.childCount == 0)
                {
                    targetObject = model;
                }
                else
                {
                    // Otherwise create a child for the mesh
                    targetObject = new GameObject("ModelMesh");
                    targetObject.transform.SetParent(model.transform);
                    targetObject.transform.localPosition = Vector3.zero;
                    targetObject.transform.localRotation = Quaternion.identity;
                    targetObject.transform.localScale = Vector3.one;
                    targetObject.layer = model.layer; // Same layer as parent
                }
                
                // Add mesh components if needed
                if (!hasMeshComponents)
                {
                    MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        meshFilter = targetObject.AddComponent<MeshFilter>();
                        meshFilter.mesh = CreateCubeMesh();
                    }
                }
                
                // Add renderer if needed
                if (!hasRenderers)
                {
                    MeshRenderer renderer = targetObject.GetComponent<MeshRenderer>();
                    if (renderer == null)
                    {
                        renderer = targetObject.AddComponent<MeshRenderer>();
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = new Color(
                            UnityEngine.Random.value, 
                            UnityEngine.Random.value, 
                            UnityEngine.Random.value
                        );
                    }
                }
            }
            else
            {
                // Ensure all child objects with renderers are also set to the correct layer
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.gameObject.layer = model.layer;
                }
            }
            
            // Apply transform data
            model.transform.position = objData.position;
            model.transform.eulerAngles = objData.rotation;
            model.transform.localScale = objData.scale;
            model.name = objData.id;

            // Add metadata component
            SceneObjectMetadata metadata = model.AddComponent<SceneObjectMetadata>();
            metadata.Initialize(objData.id, objData.modelUrl);

            // Add collider if needed
            if (model.GetComponent<Collider>() == null)
            {
                // Try to find a mesh in the hierarchy for the collider
                Mesh colliderMesh = null;
                
                // First check MeshFilters
                MeshFilter firstMeshFilter = model.GetComponentInChildren<MeshFilter>();
                if (firstMeshFilter != null && firstMeshFilter.sharedMesh != null)
                {
                    colliderMesh = firstMeshFilter.sharedMesh;
                }
                // If no MeshFilter found, try SkinnedMeshRenderers
                else
                {
                    SkinnedMeshRenderer firstSkinnedMesh = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (firstSkinnedMesh != null && firstSkinnedMesh.sharedMesh != null)
                    {
                        colliderMesh = firstSkinnedMesh.sharedMesh;
                    }
                }
                
                if (colliderMesh != null)
                {
                    MeshCollider collider = model.AddComponent<MeshCollider>();
                    collider.sharedMesh = colliderMesh;
                    collider.convex = true;
                    Debug.Log($"Added MeshCollider with proper mesh to {objData.id}");
                }
                else
                {
                    // Fallback to a default collider
                    BoxCollider boxCollider = model.AddComponent<BoxCollider>();
                    Debug.Log($"Added BoxCollider to {objData.id} as fallback (no mesh found)");
                }
            }

            Debug.Log($"Loaded model: {objData.id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading model {objData.id}: {e.Message}");
        }
    }

    // Create a simple cube mesh as fallback
    private Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        // Define the 8 vertices of a cube
        Vector3[] vertices = {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };
        
        // Define the triangles (6 faces, 2 triangles per face, 3 indices per triangle)
        int[] triangles = {
            // Front face
            0, 2, 1, 0, 3, 2,
            // Right face
            1, 2, 6, 1, 6, 5,
            // Back face
            5, 6, 7, 5, 7, 4,
            // Left face
            4, 7, 3, 4, 3, 0,
            // Top face
            3, 7, 6, 3, 6, 2,
            // Bottom face
            0, 1, 5, 0, 5, 4
        };
        
        // Define UVs (simple mapping)
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < uv.Length; i++)
        {
            uv[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f);
        }
        
        // Set mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        
        // Recalculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    // Access loaded scene data
    public SceneData GetLoadedSceneData()
    {
        return loadedSceneData;
    }

    /* 
    // BACKEND INTEGRATION (NOT YET IMPLEMENTED)
    
    // Download JSON data from server
    private async Task<string> DownloadJsonFromServerAsync(string url)
    {
        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    throw new Exception(request.error);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error downloading JSON from server: {e.Message}");
            throw;
        }
    }
    */

    // Download binary file (for models)
    private async Task<byte[]> DownloadBinaryFromServerAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
                return request.downloadHandler.data;
            else
                throw new Exception(request.error);
        }
    }
}