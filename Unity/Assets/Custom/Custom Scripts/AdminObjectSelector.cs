using UnityEngine;

// Handles selecting objects in admin mode with visual tractor beam
public class AdminObjectSelector : MonoBehaviour
{
    // Core settings
    public Camera adminCamera;
    public float maxGrabDistance = 10f;
    public LayerMask objectLayer;
    
    // Tractor beam appearance
    public LineRenderer tractorBeam;
    public Color beamIdleColor = new Color(1f, 0.3f, 0.3f, 0.5f); // Red when idle
    public Color beamActiveColor = new Color(0.5f, 1f, 0.5f, 0.8f); // Green when active
    public float beamWidth = 0.08f;
    
    // Beam origin position
    public Vector3 beamOriginOffset = new Vector3(0.3f, -0.3f, 0.5f); // Right, down, forward
    public bool showHandlePoint = true;
    private GameObject handlePoint;

    // Selection tracking
    private GameObject selectedObject;
    private Color originalColor;
    private GameObject targetObject;
    
    // Helper method to get the renderer from an object or its children
    private Renderer GetRendererFromObject(GameObject obj)
    {
        // Try to get renderer directly from the object
        Renderer renderer = obj.GetComponent<Renderer>();
        
        // If no renderer found, try to find one in children
        if (renderer == null)
        {
            renderer = obj.GetComponentInChildren<Renderer>();
        }
        
        return renderer;
    }

    void Start()
    {
        // Create tractor beam if not assigned
        if (tractorBeam == null)
        {
            GameObject beamObj = new GameObject("TractorBeam");
            beamObj.transform.parent = adminCamera.transform;
            tractorBeam = beamObj.AddComponent<LineRenderer>();
            
            // Configure line renderer
            tractorBeam.startWidth = beamWidth;
            tractorBeam.endWidth = beamWidth;
            tractorBeam.positionCount = 2;
            tractorBeam.useWorldSpace = true;
            
            // Set material and rendering properties
            tractorBeam.material = new Material(Shader.Find("Sprites/Default"));
            tractorBeam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tractorBeam.receiveShadows = false;
            tractorBeam.material.SetInt("_ZWrite", 0);
        }
        
        // Set initial beam color
        tractorBeam.startColor = beamIdleColor;
        tractorBeam.endColor = beamIdleColor;
        
        // Create visual handle at beam origin
        if (showHandlePoint)
        {
            handlePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handlePoint.transform.parent = adminCamera.transform;
            handlePoint.transform.localPosition = beamOriginOffset;
            handlePoint.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            
            Destroy(handlePoint.GetComponent<Collider>());
            handlePoint.GetComponent<Renderer>().material.color = beamIdleColor;
        }
    }

    void Update()
    {
        UpdateTractorBeam();
        
        // Object selection with left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = adminCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, objectLayer))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // Find the root object with SceneObjectMetadata if available
                SceneObjectMetadata metadata = hitObject.GetComponent<SceneObjectMetadata>();
                if (metadata == null)
                {
                    metadata = hitObject.GetComponentInParent<SceneObjectMetadata>();
                    if (metadata != null)
                    {
                        hitObject = metadata.gameObject;
                    }
                }
                
                // Handle existing selection
                if (selectedObject != null)
                {
                    // Toggle selection off if clicking same object
                    if (selectedObject == hitObject)
                    {
                        Renderer renderer = GetRendererFromObject(selectedObject);
                        if (renderer != null)
                        {
                            renderer.material.color = originalColor;
                        }
                        selectedObject = null;
                        
                        if (showHandlePoint)
                            handlePoint.GetComponent<Renderer>().material.color = beamIdleColor;
                        return;
                    }
                    // Switch selection if clicking different object
                    else
                    {
                        Renderer renderer = GetRendererFromObject(selectedObject);
                        if (renderer != null)
                        {
                            renderer.material.color = originalColor;
                        }
                    }
                }

                // Select new object
                selectedObject = hitObject;
                
                // Get renderer and store original color
                Renderer newRenderer = GetRendererFromObject(selectedObject);
                if (newRenderer != null)
                {
                    originalColor = newRenderer.material.color;
                    newRenderer.material.color = beamActiveColor;
                }
                
                if (showHandlePoint)
                    handlePoint.GetComponent<Renderer>().material.color = beamActiveColor;
            }
            else if (selectedObject != null)
            {
                // Deselect when clicking empty space
                Renderer renderer = GetRendererFromObject(selectedObject);
                if (renderer != null)
                {
                    renderer.material.color = originalColor;
                }
                selectedObject = null;
                
                if (showHandlePoint)
                    handlePoint.GetComponent<Renderer>().material.color = beamIdleColor;
            }
        }
    }

    void UpdateTractorBeam()
    {
        // Set beam start position at handle point
        Vector3 beamStart = adminCamera.transform.TransformPoint(beamOriginOffset);
        tractorBeam.SetPosition(0, beamStart);
        
        Ray ray = adminCamera.ScreenPointToRay(Input.mousePosition);
        targetObject = null;
        
        // Beam visualization based on selection state
        if (selectedObject != null)
        {
            // Connect beam to selected object
            tractorBeam.enabled = true;
            tractorBeam.startColor = beamActiveColor;
            tractorBeam.endColor = beamActiveColor;
            tractorBeam.SetPosition(1, selectedObject.transform.position);
        }
        else if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, objectLayer))
        {
            // Show beam pointing at potential target
            tractorBeam.enabled = true;
            tractorBeam.startColor = beamIdleColor;
            tractorBeam.endColor = beamIdleColor;
            tractorBeam.SetPosition(1, hit.point);
            targetObject = hit.collider.gameObject;
        }
        else
        {
            // Show beam extending into space
            tractorBeam.enabled = true;
            tractorBeam.startColor = new Color(beamIdleColor.r, beamIdleColor.g, beamIdleColor.b, beamIdleColor.a * 0.8f);
            tractorBeam.endColor = new Color(beamIdleColor.r, beamIdleColor.g, beamIdleColor.b, beamIdleColor.a * 0.4f);
            
            Vector3 endPoint = ray.origin + ray.direction * maxGrabDistance;
            tractorBeam.SetPosition(1, endPoint);
        }
    }

    public GameObject GetSelectedObject()
    {
        return selectedObject;
    }
}