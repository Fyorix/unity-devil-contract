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
    public float walkSpeed = 3f;
    public float backwardWalkSpeed = 3f;
    public float jumpHeight = 0.2f;
    public float gravity = -20f;

    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction gatheringAction;
    private InputAction walkAction;
    private Inventory inventory;
    private float nextDriftLogTime;

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
        
        walkAction = new InputAction("Walk", binding: "<Keyboard>/leftShift");
        walkAction.AddBinding("<Gamepad>/leftTrigger");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        gatheringAction.Enable();
        walkAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        gatheringAction.Disable();
        walkAction.Disable();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep the player grounded and allow for smooth jumping again
        }

        // --- Movement ---
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isWalking = walkAction.IsPressed();

        // Cancel gathering if player starts moving
        // if (isGathering && input.magnitude >= 0.1f)
        // {
        //     animator.SetBool("isGathering", false);
        //     isGathering = false;
        // }

        // Movement & rotation
        if(input.magnitude >= 0.1f)
        {
            // Set animation state based on walking
            if (isWalking)
            {
                animator.SetBool("isRunning", false);
                animator.SetBool("isWalking", true);
            }
            else
            {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
            }

            // Store camera angle before any modifications
            float cameraYAngle = playerCamera.eulerAngles.y;

            // Move relative to camera
            Vector3 moveDir = Quaternion.Euler(0, cameraYAngle, 0) * new Vector3(input.x, 0, input.y);
            
            // Use appropriate speed
            float currentSpeed = moveSpeed;
            if (isWalking)
            {
                currentSpeed = (input.y < -0.1f) ? backwardWalkSpeed : walkSpeed;
            }
            controller.Move(moveDir * currentSpeed * Time.deltaTime);

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
            else if (input.y >= 0 && input.x != 0)
            {
                // Rotate player when moving sideways (A or D without W or S)
                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cameraYAngle;
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Handle backward walking animation
            if (input.y < -0.1f)
            {
                animator.SetBool("isWalkingBackwards", true);
            }
            else
            {
                animator.SetBool("isWalkingBackwards", false);
            }
        }
        else
        {
            animator.SetBool("isRunning", false); // Set animation parameter
            animator.SetBool("isWalking", false); // Reset walking animation
            animator.SetBool("isWalkingBackwards", false); // Reset backward walking animation
        }

        // --- Jump ---
        if (jumpAction.triggered && isGrounded)
        {
            animator.SetTrigger("isJumping"); // Trigger jump animation
        }
        // --- Gathering ---
        if (gatheringAction.triggered && isGrounded)
        {
            Debug.Log("Gathering action triggered!");
            animator.SetTrigger("isGathering"); // Trigger gathering animation
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

    public void DoGather()
    {

        Debug.Log("DoGather called!");
        // Check for nearby gatherable objects
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Gatherable"))
            {
                // Check if player is facing the gatherable object
                Vector3 directionToGatherable = (hitCollider.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToGatherable);
                
                // Only gather if facing within 90 degrees (in front of player)
                if (angle <= 60f)
                {
                    // Add item to inventory
                    // inventory.AddItem(hitCollider.gameObject);
                    // Destroy the gatherable object
                    Destroy(hitCollider.gameObject);
                    break; // Only gather one item per action
                }
            }
        }
    }

}