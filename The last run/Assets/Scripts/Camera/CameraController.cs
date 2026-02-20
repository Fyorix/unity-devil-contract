using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 100f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    public Transform playerBody;

    private float xRotation = 0f;

    private InputAction lookAction;

    void Awake()
    {
        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");
    }

    void OnEnable() => lookAction.Enable();
    void OnDisable() => lookAction.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;
    }

    void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>() * sensitivity;

        float mouseX = look.x;
        float mouseY = look.y;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }

    public float GetYaw()
    {
        return playerBody.eulerAngles.y;
    }
}
