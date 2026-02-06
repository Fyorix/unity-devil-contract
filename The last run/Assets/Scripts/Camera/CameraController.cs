using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerBody; // Assign your Player object here
    public Vector3 offset = new Vector3(0, 1.6f, 0); // Height of eyes (1.6m is standard)

    [Header("Look Settings")]
    public float sensitivity = 0.05f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private InputAction lookAction;

    void Awake()
    {
        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");
    }

    void OnEnable() 
    { 
        lookAction.Enable(); 
        // Auto-find player if not assigned, looking for parent or tag
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }
    }
    
    void OnDisable() { lookAction.Disable(); }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // Correct internal rotation to match current facing direction at start
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        yRotation = currentRotation.y;
        xRotation = currentRotation.x;
    }

    void LateUpdate() // LateUpdate avoids jitter when following player movement
    {
        // 1. Follow Position
        if (playerBody != null)
        {
            transform.position = playerBody.position + offset;
        }

        // 2. Rotate Camera
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f); // Use global rotation to decouple from player body rotation
    }
}
