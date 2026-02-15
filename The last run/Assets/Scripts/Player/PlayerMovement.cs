using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    [Header("Gathering")]
    public float gatherRadius = 2f;
    [Range(0f, 180f)]
    public float gatherAngle = 90f; // degrees from camera forward
    public LayerMask gatherLayer = ~0; // default: everything

    [Header("Finish")]
    public float finishDetectRadius = 1.5f;
    public LayerMask finishLayer = ~0;
    public string finishSceneName = "";
    [Tooltip("Number of keys required to exit (0 = no requirement).")]
    public int requiredKeyCount = 2;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isTransitioning = false;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction gatheringAction;
    private InputAction walkAction;
    private Inventory inventory;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        Cursor.lockState = CursorLockMode.Locked;

        // Auto-assign camera: prefer Camera.main, fallback to any Camera in scene
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindObjectOfType<Camera>();
            }

            if (cam != null)
            {
                playerCamera = cam.transform;
            }
            else
            {
                Debug.LogWarning("PlayerMovement: no Camera found in scene to assign to playerCamera. Using player transform as fallback in calculations.");
            }
        }

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

        // Get camera direction (fallback to player transform if no camera assigned)
        float cameraYAngle = (playerCamera != null) ? playerCamera.eulerAngles.y : transform.eulerAngles.y;

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

            // Move relative to camera
            Vector3 moveDir = Quaternion.Euler(0, cameraYAngle, 0) * new Vector3(input.x, 0, input.y);
            
            // Use appropriate speed
            float currentSpeed = moveSpeed;
            if (isWalking)
            {
                currentSpeed = (input.y < -0.1f) ? backwardWalkSpeed : walkSpeed;
            }
            else if (input.y < -0.1f)
            {
                currentSpeed = moveSpeed * 0.5f;
            }
            controller.Move(moveDir * currentSpeed * Time.deltaTime);

            // Handle backward walking animation
            if (input.y < -0.1f)
            {
                animator.SetBool("isWalkingBackwards", true);
                // When walking backwards, face camera direction (don't turn around)
                Quaternion targetRotation = Quaternion.Euler(0, cameraYAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                animator.SetBool("isWalkingBackwards", false);
                // When moving forward or sideways, rotate to match movement direction
                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cameraYAngle;
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // When not moving, rotate player to face camera direction
            Quaternion targetRotation = Quaternion.Euler(0, cameraYAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            animator.SetBool("isRunning", false); // Set animation parameter
            animator.SetBool("isWalking", false); // Reset walking animation
            animator.SetBool("isWalkingBackwards", false); // Reset backward walking animation
        }

        // --- Gathering ---
        if (gatheringAction.triggered && isGrounded)
        {
            if (inventory != null && inventory.GetItemCount() < inventory.maxSlots && TryFindClosestGatherable(out _))
            {
                Debug.Log("Gathering action triggered!");
                animator.SetTrigger("isGathering"); // Trigger gathering animation
            }
            else
            {
                Debug.Log("No gatherable key in range.");
            }
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Finish detection ---
        if (!isTransitioning)
        {
            Collider[] finishColliders = Physics.OverlapSphere(transform.position, finishDetectRadius, finishLayer);
            foreach (var col in finishColliders)
            {
                if (col.CompareTag("Finish"))
                {
                    // Require a minimum number of keys (if any) before allowing exit
                    if (!HasRequiredKeyCount())
                    {
                        Debug.Log("Cannot exit: missing required keys.");
                        break;
                    }

                    isTransitioning = true;
                    if (!string.IsNullOrEmpty(finishSceneName))
                    {
                        Debug.Log($"Finish reached. Loading scene '{finishSceneName}'...");
                        SceneManager.LoadScene(finishSceneName);
                    }
                    else
                    {
                        Debug.Log("Finish reached. Loading next scene (build index)...");
                        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
                        if (nextIndex < SceneManager.sceneCountInBuildSettings)
                        {
                            SceneManager.LoadScene(nextIndex);
                        }
                        else
                        {
                            Debug.LogWarning("No next scene in build settings to load.");
                        }
                    }
                    break;
                }
            }
        }
    }

    public void DoGather()
    {
        Debug.Log("DoGather called!");
        if (inventory == null)
        {
            return;
        }

        if (inventory.GetItemCount() >= inventory.maxSlots)
        {
            Debug.Log("Inventory is full!");
            return;
        }

        if (!TryFindClosestGatherable(out Collider closest))
        {
            return;
        }

        Key key = closest.GetComponent<Key>();
        if (key != null)
        {
            if (inventory.AddItem(key))
            {
                Destroy(closest.gameObject);
            }
        }
    }

    bool TryFindClosestGatherable(out Collider closest)
    {
        // Check for nearby gatherable objects (configurable radius/angle)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, gatherRadius, gatherLayer);
        closest = null;
        float closestDistance = float.MaxValue;

        if (hitColliders.Length == 0)
        {
            Debug.Log("TryFindClosestGatherable: no colliders in gather radius (" + gatherRadius + ")");
        }

        foreach (var hitCollider in hitColliders)
        {
            if (!hitCollider.CompareTag("Gatherable"))
            {
                continue;
            }

            // Use camera transform if available, otherwise fall back to player transform
            Vector3 camPos = (playerCamera != null) ? playerCamera.position : transform.position;
            Vector3 camForward = (playerCamera != null) ? playerCamera.forward : transform.forward;
            Vector3 dirTo = hitCollider.transform.position - camPos;
            float angle = Vector3.Angle(camForward, dirTo);
            if (angle > gatherAngle)
            {
                // Skip items outside the camera-facing cone
                continue;
            }

            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hitCollider;
            }
        }

        if (closest == null)
        {
            Debug.Log("TryFindClosestGatherable: no valid gatherable found (colliders: " + hitColliders.Length + ")");
        }

        return closest != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gatherRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, finishDetectRadius);
    }

    bool HasRequiredKeyCount()
    {
        if (requiredKeyCount <= 0)
            return true;
        if (inventory == null)
            return false;
        return inventory.GetItemCount() >= requiredKeyCount;
    }

}