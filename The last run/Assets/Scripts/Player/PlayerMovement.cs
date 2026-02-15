using UnityEngine;
using UnityEngine.InputSystem;

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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction gatherAction;
    private InputAction walkAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

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

        walkAction = new InputAction("Walk", binding: "<Keyboard>/leftShift");
        walkAction.AddBinding("<Gamepad>/leftTrigger");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        gatherAction.Enable();
        walkAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        gatherAction.Disable();
        walkAction.Disable();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        ApplyGravity();
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
                footstepAudioSource.Play();
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
            animator.SetTrigger("isJumping");
        }

        if (gatherAction.triggered && controller.isGrounded)
        {
            animator.SetTrigger("isGathering");
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void DoJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}
