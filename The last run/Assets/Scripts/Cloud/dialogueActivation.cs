using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class CinematicDialogTrigger : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public Animator playerAnimator;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public string[] dialogueLines;

    [Header("Escalier")]
    public GameObject stairs;


    [Header("Camera Settings")]
    public Vector3 cinematicCameraPosition = new Vector3(248.01f, 32.07f, 572.26f);
    public Vector3 cinematicCameraRotationEuler = new Vector3(0f, -63.383f, 0f);

    private PlayerMovement playerMovement;
    private CharacterController controller;
    private CameraController cameraController;

    private void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        controller = player.GetComponent<CharacterController>();
        cameraController = playerCamera.GetComponent<CameraController>();

        if (playerAnimator == null)
            playerAnimator = player.GetComponentInChildren<Animator>();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            if (playerMovement != null) playerMovement.enabled = false;
            if (controller != null) controller.enabled = false;
            if (cameraController != null) cameraController.enabled = false;

            playerCamera.transform.position = cinematicCameraPosition;
            playerCamera.transform.rotation = Quaternion.Euler(cinematicCameraRotationEuler);

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isRunning", false);
                playerAnimator.SetBool("isWalking", false);
            }

            if (dialogueLines.Length > 0 && dialoguePanel != null && dialogueText != null)
            {
                StartCoroutine(PlayDialogue());
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private IEnumerator PlayDialogue()
    {
        dialoguePanel.SetActive(true);

        foreach (var line in dialogueLines)
        {
            dialogueText.text = line;

            bool nextPressed = false;
            while (!nextPressed)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    nextPressed = true;
                if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                    nextPressed = true;

                yield return null;
            }
        }

        dialoguePanel.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (controller != null) controller.enabled = true;
        if (cameraController != null) cameraController.enabled = true;
        stairs.GetComponent<MeshRenderer>().enabled = true;
        stairs.GetComponent<MeshCollider>().enabled = true;
    }
}
