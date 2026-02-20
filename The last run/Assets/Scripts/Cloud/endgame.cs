using UnityEngine;

public class LevitationTrigger : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public Animator playerAnimator;
    public AudioSource audioSource;

    [Header("Levitation Settings")]
    public float levitationSpeed = 2f;
    public float cameraBackSpeed = 2f;
    public float maxCameraDistance = 6f;

    [Header("Animation")]
    public string animationTriggerName = "Ascend";

    [Header("UI")]
    public GameObject creditOverlay;
    public float delayBeforeCredits = 10f;

    [Header("Camera Offset")]
    public Vector3 lookAtOffset = new Vector3(-1.5f, 0f, 0f);

    private bool isTriggered = false;
    private float currentCameraDistance = 0f;
    private bool levitationStarted = false;

    private PlayerMovement playerMovement;
    private CharacterController controller;
    private CameraController cameraController;

    private float levitationTimer = 0f;
    private bool creditsShown = false;

    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        controller = player.GetComponent<CharacterController>();
        cameraController = playerCamera.GetComponent<CameraController>();
        if (playerAnimator == null)
            playerAnimator = player.GetComponent<Animator>();
        creditOverlay.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player && !isTriggered)
        {
            isTriggered = true;

            audioSource.time = 110;
            if (playerMovement != null)
                playerMovement.enabled = false;
            if (controller != null)
                controller.enabled = false;
            if (cameraController != null)
                cameraController.enabled = false;
            Vector3 rot = playerCamera.transform.eulerAngles;
            rot.x = 0f;
            rot.y = -90f;

            playerCamera.transform.eulerAngles = rot;
            playerCamera.transform.parent = null;
            playerCamera.transform.position -= playerCamera.transform.forward * -3;
            playerAnimator.SetBool("isRunning", false);
            playerAnimator.SetBool("isWalking", false);
        }
    }

    void Update()
    {
        if (!isTriggered) return;

        if (!levitationStarted)
        {
            if (currentCameraDistance < maxCameraDistance)
            {
                float step = cameraBackSpeed * Time.deltaTime;
                playerCamera.transform.position -= playerCamera.transform.forward * step;
                currentCameraDistance += step;
            }
            else
            {
                if (playerAnimator != null)
                {
                    playerAnimator.SetTrigger(animationTriggerName);
                    playerAnimator.SetBool("IsTriggered", true);
                }
                levitationStarted = true;
                if (!creditsShown)
                {
                    Invoke(nameof(ShowCredits), delayBeforeCredits);
                    creditsShown = true;
                }
            }
        }
        else
        {
            player.position += Vector3.up * levitationSpeed * Time.deltaTime;
        }
        Vector3 offsetTarget = player.position + playerCamera.transform.right * -2f;
        playerCamera.transform.LookAt(offsetTarget);
    }

    void ShowCredits()
    {
        if (creditOverlay != null)
            creditOverlay.SetActive(true);
    }
}
