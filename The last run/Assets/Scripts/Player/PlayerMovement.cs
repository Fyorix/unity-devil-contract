using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Inventory))]
public class PlayerMovement : MonoBehaviour
{
    // Reference to the Animator
    public Animator animator; 
    public Transform playerCamera;
    public float moveSpeed = 6f;
    public float jumpHeight = 0.4f;
    public float gravity = -15f;

    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction gatheringAction;
    private Inventory inventory;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        Cursor.lockState = CursorLockMode.Locked;

        // --- Init Input Actions (Code-only approach) ---
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        
        gatheringAction = new InputAction("Gather", binding: "<Keyboard>/e");
        gatheringAction.AddBinding("<Gamepad>/buttonWest");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        gatheringAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        gatheringAction.Disable();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- Movement ---
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Movement & rotation
        if(input.magnitude >= 0.1f)
        {
            animator.SetBool("isRunning", true); // Set animation parameter

            // Store camera angle before any modifications
            float cameraYAngle = playerCamera.eulerAngles.y;

            // Move relative to camera
            Vector3 moveDir = Quaternion.Euler(0, cameraYAngle, 0) * new Vector3(input.x, 0, input.y);
            controller.Move(moveDir * moveSpeed * Time.deltaTime);

            // Only rotate player if moving forward (positive Y input)
            if (input.y > 0.1f)
            {
                // Calculate target angle relative to camera
                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cameraYAngle;
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Make camera follow player rotation (preserve camera pitch)
                Vector3 cameraEuler = playerCamera.eulerAngles;
                cameraEuler.y = transform.eulerAngles.y;
                playerCamera.eulerAngles = cameraEuler;
            }
        }
        else
        {
            animator.SetBool("isRunning", false); // Set animation parameter
        }

        // --- Jump ---
        if (jumpAction.triggered && isGrounded)
        {
            animator.SetTrigger("isJumping"); // Trigger jump animation
            // velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        // --- Gathering ---
        if (gatheringAction.triggered && isGrounded)
        {
            // Check for nearby gatherable objects
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Gatherable"))
                {
                    animator.SetTrigger("isGathering"); // Trigger gathering animation
                    // Turn the player to face the gatherable object
                    // Add item to inventory
                    // inventory.AddItem(hitCollider.gameObject);
                    // Optionally, destroy the gatherable object
                    Destroy(hitCollider.gameObject);
                    break; // Only gather one item per action
                }
            }
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void DoJump()
    {
        Debug.Log("DoJump called!");
        // This method can be called from the Animator using an Animation Event
        // It ensures the jump logic is executed at the correct time during the animation
        Debug.Log("Applying jump velocity!");
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}