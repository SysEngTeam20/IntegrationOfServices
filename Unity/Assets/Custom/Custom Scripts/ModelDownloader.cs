using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility; // Add this package via Package Manager or Asset Store
using System.Collections.Generic;
using System.Reflection;

public class ModelDownloader : MonoBehaviour
{
    // Debugging flags
    public static bool verboseLogging = true;
    
    // Pre-cached materials for builds
    private static Material standardMaterial;
    private static Material urpMaterial;
    private static Material fallbackMaterial;
    
    // Static constructor to initialize materials
    static ModelDownloader()
    {
        InitializeMaterials();
    }
    
    // Initialize materials from Resources if possible
    private static void InitializeMaterials()
    {
        // Try to load from Resources first (most reliable for builds)
        standardMaterial = Resources.Load<Material>("StandardMaterial");
        urpMaterial = Resources.Load<Material>("URPMaterial");
        
        if (standardMaterial != null)
        {
            Debug.Log("Successfully loaded StandardMaterial from Resources");
        }
        
        if (urpMaterial != null)
        {
            Debug.Log("Successfully loaded URPMaterial from Resources");
        }
        
        // Fallback to finding shaders if resources not available
        if (standardMaterial == null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null && standardShader.name != "Hidden/InternalErrorShader")
            {
                standardMaterial = new Material(standardShader);
                Debug.Log("Created Standard material from shader");
            }
        }
        
        if (urpMaterial == null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null && urpShader.name != "Hidden/InternalErrorShader")
            {
                urpMaterial = new Material(urpShader);
                Debug.Log("Created URP material from shader");
            }
        }
        
        // Create a fallback material
        fallbackMaterial = new Material(Shader.Find("Unlit/Color"));
        if (fallbackMaterial.shader == null || fallbackMaterial.shader.name == "Hidden/InternalErrorShader")
        {
            fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }
    
    // Map of model names to colors for consistent visualization
    private static Dictionary<string, Color> modelColorMap = new Dictionary<string, Color>()
    {
        { "avocado", new Color(0.2f, 0.8f, 0.2f) },  // Avocado green
        { "boombox", new Color(0.8f, 0.2f, 0.8f) },  // Boombox purple
        { "duck", new Color(1.0f, 0.8f, 0.0f) },     // Duck yellow
        { "helmet", new Color(0.5f, 0.5f, 0.5f) },   // Helmet gray
        { "chair", new Color(0.8f, 0.6f, 0.2f) }     // Chair brown
    };
    
    // Default fallback color if model not in dictionary
    private static Color defaultModelColor = new Color(0.3f, 0.6f, 0.9f); // Blue
    
    // Public method to download and attach a 3D model from a URL
    public static IEnumerator DownloadAndAttachModel(string url, Transform parent)
    {
        // Get model name from URL for identifying the model
        string modelName = GetModelNameFromUrl(url).ToLower();
        Debug.Log($"Downloading model: {modelName} from {url}");
        
        // Create temporary loading indicator
        GameObject loadingIndicator = CreateSimplePrimitive(parent, Color.yellow, PrimitiveType.Sphere, 0.2f);
        
        // Try to load the actual model in both editor and builds
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            
            // Destroy the loading indicator
            if (loadingIndicator != null) GameObject.Destroy(loadingIndicator);
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                byte[] glbData = www.downloadHandler.data;
                Debug.Log($"Successfully downloaded model from {url}");
                
                // Clean up any existing children
                foreach (Transform child in parent)
                {
                    if (child.gameObject != loadingIndicator)
                        GameObject.Destroy(child.gameObject);
                }
                
                try
                {
                    Debug.Log("Attempting to load full model in build");
                    
                    // Configure import settings
                    ImportSettings importSettings = new ImportSettings();
                    ConfigureImportSettings(importSettings);
                    
                    // Try to import the model with our settings
                    GameObject modelObject = Importer.LoadFromBytes(glbData, importSettings);
                    
                    if (modelObject != null)
                    {
                        Debug.Log($"Successfully imported model: {modelName}");
                        // Attach to parent
                        modelObject.transform.SetParent(parent, false);
                        modelObject.transform.localPosition = Vector3.zero;
                        modelObject.transform.localRotation = Quaternion.identity;
                        modelObject.transform.localScale = Vector3.one;
                        
                        // Add colliders if needed
                        if (modelObject.GetComponentInChildren<Collider>() == null)
                        {
                            foreach (MeshRenderer renderer in modelObject.GetComponentsInChildren<MeshRenderer>())
                            {
                                if (renderer.GetComponent<Collider>() == null)
                                {
                                    renderer.gameObject.AddComponent<MeshCollider>();
                                }
                            }
                        }
                        
                        // Fix any material issues
                        FixModelMaterials(modelObject);
                        
                        yield break; // Successfully loaded model
                    }
                    else
                    {
                        Debug.LogError($"Failed to import GLB model from {url}, falling back to primitive");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error importing GLB model: {e.Message}, falling back to primitive");
                    if (verboseLogging)
                    {
                        Debug.LogError($"Stack trace: {e.StackTrace}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Error downloading model: {www.error}, falling back to primitive");
            }
        }
        
        // If we got here, model loading failed - create a primitive instead
        Debug.Log("Creating primitive as fallback for failed model load");
        CreateModelPrimitive(modelName, parent);
    }
    
    // Configure GLTF import settings for better compatibility
    private static void ConfigureImportSettings(ImportSettings settings)
    {
        if (settings == null) return;
        
        // Try to set properties via reflection to handle different versions of GLTFUtility
        try {
            // Common settings
            typeof(ImportSettings).GetField("generateMipMaps")?.SetValue(settings, true);
            typeof(ImportSettings).GetField("useNormalmaps")?.SetValue(settings, true);
            
            // Get the AnimationSettings type if it exists
            var animSettingsType = typeof(ImportSettings).Assembly.GetType("Siccity.GLTFUtility.AnimationSettings");
            if (animSettingsType != null)
            {
                // Try to create new AnimationSettings
                var animSettings = System.Activator.CreateInstance(animSettingsType);
                
                // Set useLegacyClips if it exists
                animSettingsType.GetField("useLegacyClips")?.SetValue(animSettings, true);
                
                // Set the animationSettings field if it exists
                typeof(ImportSettings).GetField("animationSettings")?.SetValue(settings, animSettings);
            }
            
            Debug.Log("Successfully configured GLTF import settings");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to configure GLTF import settings: {e.Message}");
            // Continue with default settings
        }
    }
    
    // Get a simplified name from the URL for identification
    private static string GetModelNameFromUrl(string url)
    {
        string filename = System.IO.Path.GetFileNameWithoutExtension(url);
        return filename.ToLower();
    }
    
    // Create a primitive that represents the model
    private static void CreateModelPrimitive(string modelName, Transform parent)
    {
        Color color = GetColorForModel(modelName);
        PrimitiveType shape = GetShapeForModel(modelName);
        float scale = 0.5f;
        
        // Create the primitive
        GameObject primitive = CreateSimplePrimitive(parent, color, shape, scale);
        
        // Special case for boombox - add a top part
        if (modelName.Contains("boom"))
        {
            // Create a second smaller cube on top to make it look like a boombox
            CreateSimplePrimitive(parent, color, PrimitiveType.Cube, 
                new Vector3(0.8f, 0.2f, 0.6f), new Vector3(0, 0.3f, 0));
        }
        
        Debug.Log($"Created {shape} primitive for model: {modelName} with color: {color}");
    }
    
    // Helper to create a simple primitive with given parameters
    private static GameObject CreateSimplePrimitive(Transform parent, Color color, PrimitiveType type, float scale)
    {
        return CreateSimplePrimitive(parent, color, type, new Vector3(scale, scale, scale), Vector3.zero);
    }
    
    // Helper to create a simple primitive with given parameters
    private static GameObject CreateSimplePrimitive(Transform parent, Color color, PrimitiveType type, 
                                                    Vector3 scale, Vector3 localPosition = default)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = scale;
        
        // Set material using our pre-cached materials
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material newMaterial = null;
            
            // Try to use our pre-cached materials in order of preference
            if (standardMaterial != null)
            {
                newMaterial = new Material(standardMaterial);
                Debug.Log("Using cached Standard material");
            }
            else if (urpMaterial != null)
            {
                newMaterial = new Material(urpMaterial);
                Debug.Log("Using cached URP material");
            }
            else if (fallbackMaterial != null)
            {
                newMaterial = new Material(fallbackMaterial);
                Debug.Log("Using cached fallback material");
            }
            else
            {
                // Create a basic material with the default renderer's shader
                newMaterial = new Material(renderer.material.shader);
                Debug.Log("Using default renderer material");
            }
            
            // Set the color based on shader type
            if (newMaterial != null)
            {
                // Try all common color properties
                newMaterial.color = color;
                newMaterial.SetColor("_Color", color);
                newMaterial.SetColor("_BaseColor", color);
                newMaterial.SetColor("_EmissionColor", color * 0.5f);
                
                // Apply the material
                renderer.material = newMaterial;
                
                Debug.Log($"Applied material with color {color} to {obj.name}");
            }
        }
        
        return obj;
    }
    
    // Determine what color to use based on model name
    private static Color GetColorForModel(string modelName)
    {
        // Try to find a predefined color for this model
        foreach (var entry in modelColorMap)
        {
            if (modelName.Contains(entry.Key))
            {
                return entry.Value;
            }
        }
        
        // Default color if no match
        return defaultModelColor;
    }
    
    // Determine what shape to use based on model name
    private static PrimitiveType GetShapeForModel(string modelName)
    {
        if (modelName.Contains("avocado") || 
            modelName.Contains("ball") || 
            modelName.Contains("sphere"))
        {
            return PrimitiveType.Sphere;
        }
        else if (modelName.Contains("cylinder") || 
                modelName.Contains("tube") || 
                modelName.Contains("can"))
        {
            return PrimitiveType.Cylinder;
        }
        else if (modelName.Contains("capsule") || 
                modelName.Contains("pill"))
        {
            return PrimitiveType.Capsule;
        }
        
        // Default for most objects
        return PrimitiveType.Cube;
    }
    
    // Helper method to fix material issues with loaded models
    private static void FixModelMaterials(GameObject modelObject)
    {
        Debug.Log("Fixing model materials");
        Renderer[] renderers = modelObject.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial == null || 
                renderer.sharedMaterial.shader == null || 
                renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                Debug.Log($"Fixing material on {renderer.gameObject.name}");
                
                // Try to use our cached materials
                Material fixedMaterial = null;
                if (standardMaterial != null)
                {
                    fixedMaterial = new Material(standardMaterial);
                }
                else if (urpMaterial != null)
                {
                    fixedMaterial = new Material(urpMaterial);
                }
                else if (fallbackMaterial != null)
                {
                    fixedMaterial = new Material(fallbackMaterial);
                }
                else
                {
                    fixedMaterial = new Material(Shader.Find("Standard"));
                }
                
                // If original material had a color, preserve it
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                {
                    Color origColor = renderer.sharedMaterial.color;
                    fixedMaterial.color = origColor;
                    fixedMaterial.SetColor("_BaseColor", origColor); // For URP
                }
                
                renderer.material = fixedMaterial;
            }
        }
    }
    
    private static void AddColliders(GameObject modelObject)
    {
        if (modelObject.GetComponentInChildren<Collider>() == null)
        {
            foreach (MeshRenderer renderer in modelObject.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.GetComponent<Collider>() == null)
                {
                    renderer.gameObject.AddComponent<MeshCollider>();
                }
            }
        }
    }
}
