using UnityEngine;

// Controls first-person movement and camera for admin mode
public class AdminController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;

    // Camera rotation tracking
    private float rotationX = 0f;
    private float rotationY = 0f;
    public Transform cameraTransform; // Camera to rotate

    void Start()
    {
        // Lock cursor for camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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

        // Toggle cursor lock with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}