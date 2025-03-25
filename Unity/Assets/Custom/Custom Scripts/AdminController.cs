using UnityEngine;

// Controls first-person movement and camera for admin mode
public class AdminController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;
    
    // Mode settings
    [Header("Mode Settings")]
    public KeyCode toggleModeKey = KeyCode.Tab;
    public bool startInUIMode = false;
    private bool isInUIMode = false;
    
    // Reference to other components
    [Header("References")]
    public AdminObjectSelector objectSelector;
    public Camera mainCamera;  // Reference to the main camera
    
    [Header("Layer Settings")]
    public string interactableLayerName = "Selectable";  // Layer for interactable objects
    private int interactableLayer;
    private int savedCullingMask;  // To store the original culling mask
    
    // Camera rotation tracking
    private float rotationX = 0f;
    private float rotationY = 0f;
    public Transform cameraTransform; // Camera to rotate

    void Start()
    {
        // Set up the layer for interactable objects
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        
        // If mainCamera isn't assigned, try to find it
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Save original culling mask
        if (mainCamera != null)
        {
            savedCullingMask = mainCamera.cullingMask;
        }
        
        // Set initial mode
        isInUIMode = startInUIMode;
        UpdateCursorState();
    }

    void Update()
    {
        // Toggle between UI mode and tractor beam mode
        if (Input.GetKeyDown(toggleModeKey))
        {
            ToggleMode();
        }
        
        // When in UI mode, we don't control the camera or move
        if (isInUIMode)
            return;
            
        // Camera rotation
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Limit vertical look angle
        rotationY += Input.GetAxis("Mouse X") * lookSpeed;

        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f); // Vertical camera rotation
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f); // Horizontal body rotation

        // WASD movement
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // E/Q for vertical movement
        float moveY = 0f;
        if (Input.GetKey(KeyCode.E)) moveY = moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) moveY = -moveSpeed * Time.deltaTime;

        // Apply movement in local space
        transform.Translate(new Vector3(moveX, moveY, moveZ), Space.Self);
    }
    
    // Toggle between UI and tractor beam modes
    public void ToggleMode()
    {
        isInUIMode = !isInUIMode;
        UpdateCursorState();
    }
    
    // Update cursor visibility based on current mode
    private void UpdateCursorState()
    {
        if (isInUIMode)
        {
            // UI Mode: Show cursor and unlock
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Disable tractor beam
            if (objectSelector != null)
            {
                objectSelector.enabled = false;
            }
            
            // Modify camera culling mask to ignore interactable objects if needed
            if (mainCamera != null && interactableLayer >= 0)
            {
                // Remove the interactable layer from culling mask (objects will still render but not interact)
                // Alternatively, to hide objects entirely: mainCamera.cullingMask &= ~(1 << interactableLayer);
            }
            
            // Disable physics raycasts for interactable objects
            if (interactableLayer >= 0)
            {
                Physics.IgnoreLayerCollision(0, interactableLayer, true); // Ignore collisions between default and interactable
            }
            
            // Disable all object manipulation components
            DisableAdminComponents();
            
            Debug.Log("UI Mode Active - Use mouse to interact with UI elements");
        }
        else
        {
            // Tractor Beam Mode: Hide cursor and lock
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Enable tractor beam
            if (objectSelector != null)
            {
                objectSelector.enabled = true;
            }
            
            // Restore camera culling mask
            if (mainCamera != null)
            {
                mainCamera.cullingMask = savedCullingMask;
            }
            
            // Re-enable physics raycasts
            if (interactableLayer >= 0)
            {
                Physics.IgnoreLayerCollision(0, interactableLayer, false); // Allow collisions again
            }
            
            // Enable all object manipulation components
            EnableAdminComponents();
            
            Debug.Log("Tractor Beam Mode Active - Use mouse to select and manipulate objects");
        }
    }
    
    // Helper method to disable all admin components
    private void DisableAdminComponents()
    {
        // Disable object manipulation components
        var movers = FindObjectsOfType<AdminObjectMover>();
        foreach(var mover in movers) mover.enabled = false;
        
        var rotators = FindObjectsOfType<AdminObjectRotator>();
        foreach(var rotator in rotators) rotator.enabled = false;
        
        var scalers = FindObjectsOfType<AdminObjectScaler>();
        foreach(var scaler in scalers) scaler.enabled = false;
        
        // Keep AdminModeController enabled for key input, but disable any other
        // admin-related components that might interfere with UI
    }
    
    // Helper method to enable all admin components
    private void EnableAdminComponents()
    {
        // Enable object manipulation components
        var movers = FindObjectsOfType<AdminObjectMover>();
        foreach(var mover in movers) mover.enabled = true;
        
        var rotators = FindObjectsOfType<AdminObjectRotator>();
        foreach(var rotator in rotators) rotator.enabled = true;
        
        var scalers = FindObjectsOfType<AdminObjectScaler>();
        foreach(var scaler in scalers) scaler.enabled = true;
    }
    
    // Public accessor for current mode
    public bool IsInUIMode()
    {
        return isInUIMode;
    }
}