using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Inventory))]
public class PlayerMovement : MonoBehaviour
{
    public Animator animator;
    public CameraController cameraController;


    [Header("Footstep Sound")]
    public AudioSource footstepAudioSource;

    private float stepTimer = 0f;


    public float moveSpeed = 6f;
    public float walkSpeed = 3f;
    public float backwardWalkSpeed = 3f;
    public float rotationSpeed = 10f;

    public float jumpHeight = 0.2f;
    public float gravity = -20f;

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
    private InputAction gatherAction;
    private InputAction walkAction;
    private InputAction finishAction;
    private Inventory inventory;
    private bool waitingForFinishInput = false;
    private string pendingFinishSceneName = null;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        Cursor.lockState = CursorLockMode.Locked;
        // Camera is provided via `cameraController`. If not, code will fall back to Camera.main where needed.

        // --- Init Input Actions (Code-only approach) ---
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        gatherAction = new InputAction("Gather", binding: "<Keyboard>/e");
        gatherAction.AddBinding("<Gamepad>/buttonWest");

        finishAction = new InputAction("Finish", binding: "<Keyboard>/f");

        walkAction = new InputAction("Walk", binding: "<Keyboard>/leftShift");
        walkAction.AddBinding("<Gamepad>/leftTrigger");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        gatherAction.Enable();
        walkAction.Enable();
        finishAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        gatherAction.Disable();
        walkAction.Disable();
        finishAction.Disable();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleFinishInput();
    }

    void HandleFinishInput()
    {
        if (finishAction != null && finishAction.triggered && waitingForFinishInput && !isTransitioning)
        {
            // proceed to load pending scene if we have a valid name
            if (!string.IsNullOrEmpty(pendingFinishSceneName))
            {
                isTransitioning = true;
                Debug.Log($"Finish input pressed. Loading scene '{pendingFinishSceneName}'...");
                SceneManager.LoadScene(pendingFinishSceneName);
            }
            else
            {
                Debug.LogError("Finish input pressed but no finish scene was specified (pendingFinishSceneName is empty). No scene will be loaded.");
            }
        }
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isWalking = walkAction.IsPressed();

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (input.magnitude < 0.1f)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isWalkingBackwards", false);
            if (footstepAudioSource.isPlaying)
            {
                Debug.Log("stop walking");
                footstepAudioSource.Stop();
            }
            return;
        }
        else if (isGrounded)
        {
            if (!footstepAudioSource.isPlaying)
            {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
                footstepAudioSource.Play();
            }

            float currentSpeed = moveSpeed;
            if (isWalking)
            {
                currentSpeed = (input.y < -0.1f) ? backwardWalkSpeed : walkSpeed;
            }
            else if (input.y < -0.1f)
            {
                currentSpeed = moveSpeed * 0.5f;
            }

            Vector3 moveDir =
            transform.right * input.x +
            transform.forward * input.y;

            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
            if (input.y < -0.1f)
            {
                animator.SetBool("isWalkingBackwards", true);
            }
            else
            {
                animator.SetBool("isWalkingBackwards", false);
            }
        }

        float speed = isWalking ? walkSpeed : moveSpeed;

        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        controller.Move(move.normalized * speed * Time.deltaTime);

        animator.SetBool("isRunning", !isWalking);
        animator.SetBool("isWalking", isWalking);
    }


    void HandleJump()
    {
        if (jumpAction.triggered && controller.isGrounded)
        {
            // When not moving, rotate player to face camera direction
            animator.SetTrigger("isJumping");
            animator.SetBool("isRunning", false); // Set animation parameter
            animator.SetBool("isWalking", false); // Reset walking animation
            animator.SetBool("isWalkingBackwards", false); // Reset backward walking animation
        }

        // --- Gathering ---
        if (gatherAction.triggered && isGrounded)
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
    }

    void ApplyGravity()
    {
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
                        int missing = requiredKeyCount;
                        if (inventory != null)
                            missing = Mathf.Max(0, requiredKeyCount - inventory.GetItemCount());
                        string msg = $"Missing {missing} key{(missing == 1 ? "" : "s")} to open the door";
                        Debug.Log("Cannot exit: missing required keys.");
                        if (DoorHint.Instance != null)
                            DoorHint.Instance.ShowMessage(msg, 2f);
                        break;
                    }
                    // Player meets key requirement — wait for input to finish
                    if (!waitingForFinishInput)
                    {
                        waitingForFinishInput = true;
                        if (!string.IsNullOrEmpty(finishSceneName))
                        {
                            pendingFinishSceneName = finishSceneName;
                        }
                        else
                        {
                            // No scene name specified; record nothing and require explicit config
                            pendingFinishSceneName = null;
                        }
                        string hint = "Press F to exit";
                        Debug.Log("Finish reached. Waiting for player input to exit.");
                        if (DoorHint.Instance != null)
                            DoorHint.Instance.ShowMessage(hint, 999f);
                    }
                    break;
                }
            }
            // If no finish collider found in this check, clear waiting state
            if (finishColliders.Length == 0 && waitingForFinishInput)
            {
                waitingForFinishInput = false;
                pendingFinishSceneName = null;
                if (DoorHint.Instance != null)
                    DoorHint.Instance.ShowMessage("", 0.1f);
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
            Debug.Log("No gatherable found in DoGather.");
            return;
        }

        Key key = closest.GetComponent<Key>();
        if (key != null)
        {
            Debug.Log("Gathering key: " + key.name);
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

            // Determine if player is facing the gatherable or standing on it.
            // Preference: allow gather when within direct proximity, otherwise require player-facing within `gatherAngle`.
            Vector3 dirTo = hitCollider.transform.position - transform.position;
            float distance = dirTo.magnitude;

            // If player is essentially on the object, accept immediately
            const float onTopThreshold = 0.5f; // meters
            if (distance > onTopThreshold)
            {
                // Not on top: require player to be facing the object within gatherAngle
                float angle = Vector3.Angle(transform.forward, dirTo);
                if (angle > gatherAngle)
                    continue; // skip if not facing within allowed angle
            }

            // Use distance for choosing the closest valid gatherable
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

    public void DoJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

}

