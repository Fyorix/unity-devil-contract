using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;

public class ContractDialog : MonoBehaviour
{
    [Header("Dialog Messages")]
    public string[] dialogMessages;

    [Tooltip("Key used to advance dialog messages.")]
    public KeyCode advanceKey = KeyCode.Space;
    [Header("UI")]
    [Tooltip("TextMeshProUGUI to display dialog messages (preferred)")]
    public TextMeshProUGUI dialogTMP; // TMP text
    [Tooltip("Duration (s) for dialog text fade in/out")]
    public float uiFadeDuration = 0.3f;
    [Tooltip("Delay between characters for typewriter effect (seconds)")]
    public float typewriterDelay = 0.04f;

    [Tooltip("Optional Skip button (will be destroyed when dialog ends or skipped)")]
    public Button skipButton;

    [Header("Camera")]
    [Tooltip("Local offset from player for dialog camera (x=right, y=up, z=forward)")]
    public Vector3 cameraOffset = new Vector3(2f, 1.5f, 0f);
    [Tooltip("If true, move main camera to side of player during dialog")]
    public bool moveCameraToSide = true;
    [Tooltip("Offset from this transform where the devil should be placed (relative to this GameObject)")]
    public Vector3 devilOffset = new Vector3(0f, 0f, -1.5f);
    // testing fields removed
    Color uiOriginalColor = Color.white;
    Color uiTMPOriginalColor = Color.white;
    Coroutine uiFadeCoroutine = null;
    Coroutine typeCoroutine = null;

    private int currentMessageIndex = 0;
    private bool dialogActive = false;

    // stored for use when releasing
    private PlayerMovement cachedPlayerMovement;
    private Animator cachedPlayerAnimator;
    private DevilManagement cachedDevilManager;
    private GameObject cachedDevilObject;
    private Animator cachedDevilAnimator;
    private Vector3 cachedPreviousDevilPosition;
    private Quaternion cachedPreviousDevilRotation;
    private NavMeshAgent cachedDevilNavAgent;
    private bool cachedDevilNavAgentStopped = false;
    private CharacterController cachedDevilController;
    private bool cachedDevilControllerEnabled = false;
    private Vector3 previousCameraPosition;
    private Quaternion previousCameraRotation;
    private Transform previousCameraParent;
    private Behaviour cachedCameraController;
    private InputAction advanceAction;
    private InputAction skipAction;


    void Awake()
    {
        // create input action for advancing dialog (Space / gamepad South)
        advanceAction = new InputAction("AdvanceDialog", binding: "<Keyboard>/space");
        // create input action for skipping intro (P key)
        skipAction = new InputAction("SkipIntro", binding: "<Keyboard>/p");

        // cache devil references once at startup to avoid repeated lookups later
        var devil = GameObject.FindGameObjectWithTag("Devil");
        if (devil != null)
        {
            cachedDevilObject = devil;
            cachedDevilAnimator = devil.GetComponent<Animator>();
            // cache components here; Start() will handle runtime state (isStopped, enabled, etc.)
            cachedDevilNavAgent = devil.GetComponent<NavMeshAgent>();
            cachedDevilController = devil.GetComponent<CharacterController>();
            cachedDevilManager = devil.GetComponent<DevilManagement>();
        }
    }

    void OnEnable()
    {
        if (advanceAction != null)
            advanceAction.Enable();
        if (skipAction != null)
            skipAction.Enable();
    }

    void OnDisable()
    {
        if (advanceAction != null)
            advanceAction.Disable();
        if (skipAction != null)
            skipAction.Disable();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    /**
        * This script is responsible for disabling player movement when the contract dialog is shown, and re-enabling it after a certain time.
        * The player movement is disabled for 5 seconds to allow the player to dialog with the devil,
        * then devil goes back to its spawn point
    */
    void Start()
    {
        // use cached devil from Awake if present
        var devil = cachedDevilObject;
        Vector3 previousDevilPosition = new Vector3(0, 0, 0);
        var player = GameObject.FindGameObjectWithTag("Player");
        DevilManagement devilManager = null;
        PlayerMovement playerMovement = null;
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
                cachedPlayerMovement = playerMovement;
                cachedPlayerAnimator = player.GetComponent<Animator>();
            }
        }
        if (devil != null)
        {
            previousDevilPosition = devil.transform.position;
            cachedPreviousDevilRotation = devil.transform.rotation;
            devilManager = devil.GetComponent<DevilManagement>();
            if (devilManager != null)
            {
                // we stop the devil's emotes to avoid interrupting the dialog
                devilManager.autoShout = false;
                devilManager.autoLaugh = false;

                devilManager.enabled = false;
            }
            Debug.Log("Placing the devil in front of the player for dialog");
            // compute spawn position opposite the player relative to this transform
            Vector3 desiredPos = transform.position;
            if (player != null)
            {
                Vector3 toPlayer = (player.transform.position - transform.position);
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    Vector3 dirToPlayer = toPlayer.normalized;
                    // use devilOffset.z as distance along opposite direction; x/y as local right/up offsets
                    float backDistance = Mathf.Abs(devilOffset.z);
                    desiredPos = transform.position - dirToPlayer * backDistance + transform.right * devilOffset.x + Vector3.up * devilOffset.y;
                }
                else
                {
                    desiredPos = transform.position + transform.TransformDirection(devilOffset);
                }
            }
            else
            {
                desiredPos = transform.position + transform.TransformDirection(devilOffset);
            }

            // handle NavMeshAgent if present (Devil uses NavMeshAgent in DevilManagement)
            var nav = devil.GetComponent<NavMeshAgent>();
            if (nav != null)
            {
                cachedDevilNavAgent = nav;
                cachedDevilNavAgentStopped = nav.isStopped;
                nav.isStopped = true;
                nav.Warp(desiredPos);
                if (player != null)
                    devil.transform.LookAt(player.transform.position + Vector3.up * 1.0f);
            }
            else
            {
                // fallback: disable CharacterController if present, then teleport transform
                var devilController = devil.GetComponent<CharacterController>();
                if (devilController != null)
                {
                    cachedDevilController = devilController;
                    cachedDevilControllerEnabled = devilController.enabled;
                    devilController.enabled = false;
                }
                devil.transform.position = desiredPos;
                if (player != null)
                {
                    devil.transform.LookAt(player.transform.position + Vector3.up * 1.0f);
                }
            }
            cachedDevilManager = devilManager;
            cachedPreviousDevilPosition = previousDevilPosition;

            // move the main camera to the side of the player so the player and devil are visible
            if (moveCameraToSide && Camera.main != null && player != null)
            {
                // save previous camera transform and parent
                previousCameraParent = Camera.main.transform.parent;
                previousCameraPosition = Camera.main.transform.position;
                previousCameraRotation = Camera.main.transform.rotation;

                // detach camera so external camera controllers won't override our position
                Camera.main.transform.parent = null;

                // try to find a CameraController component and disable it
                Component camComp = Camera.main.GetComponent("CameraController");
                if (camComp is Behaviour camBeh)
                {
                    cachedCameraController = camBeh;
                    camBeh.enabled = false;
                }

                Vector3 side = player.transform.right * cameraOffset.x + Vector3.up * cameraOffset.y + player.transform.forward * cameraOffset.z;
                Camera.main.transform.position = transform.position + side;
                Camera.main.transform.LookAt(transform.position + Vector3.up * 1.0f);
            }
        }

        // prepare dialog UI state 
        if (dialogTMP != null)
        {
            uiTMPOriginalColor = dialogTMP.color;
            Color tc = uiTMPOriginalColor;
            tc.a = 0f;
            dialogTMP.color = tc;
            dialogTMP.gameObject.SetActive(false);
        }

        // skip button (if assigned) will be destroyed when dialog ends or is skipped


        // begin dialog sequence
        StartDialog();
    }

    void StartDialog()
    {
        if (dialogMessages == null || dialogMessages.Length == 0)
        {
            // nothing to show; release immediately
            StartCoroutine(waitThenRelease(cachedPlayerMovement, cachedDevilManager, cachedPreviousDevilPosition));
            return;
        }

        dialogActive = true;
        currentMessageIndex = 0;
        ShowCurrentMessage();

    }

    void ShowCurrentMessage()
    {
        string msg = dialogMessages[currentMessageIndex];
        if (dialogTMP == null)
        {
            return;
        }
        dialogTMP.text = "";
        dialogTMP.gameObject.SetActive(true);
        StartUIFade(1f);
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }
        typeCoroutine = StartCoroutine(TypeText(msg));
        // common post-message checks: 'game' restore and speaker-based animator triggers
        try
        {
            if (!string.IsNullOrEmpty(msg))
            {
                // parse speaker before ':' once
                string trimmed = msg.TrimStart();
                int colon = trimmed.IndexOf(':');
                string speaker = colon > 0 ? trimmed.Substring(0, colon).Trim() : null;

                // only start restore or trigger animations when we have a cached devil
                if (cachedDevilObject != null && trimmed.StartsWith("game", StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(RestoreDevilToSpawnAfterDelay(msg));
                }

                if (cachedPlayerAnimator != null && !string.IsNullOrEmpty(speaker) && string.Equals(speaker, "Vous", StringComparison.OrdinalIgnoreCase))
                {
                    // use 'startPraying' trigger to begin the emote
                    cachedPlayerAnimator.SetTrigger("startPraying");
                    Debug.Log("ContractDialog: triggered 'startPraying' on player Animator.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ContractDialog: error during post-message checks: {ex.Message}");
        }
        
        if (DoorHint.Instance != null)
        {
            // show message indefinitely until advanced
            DoorHint.Instance.ShowMessage(msg, 999f);
        }
        else
        {
            Debug.Log("Dialog: " + msg);
        }
    }

    IEnumerator TypeText(string msg)
    {
        if (dialogTMP == null) yield break;
        int len = msg.Length;
        for (int i = 1; i <= len; i++)
        {
            dialogTMP.text = msg.Substring(0, i);
            yield return new WaitForSeconds(typewriterDelay);
        }
        typeCoroutine = null;
    }

    IEnumerator RestoreDevilToSpawnAfterDelay(string msg)
    {
        if (string.IsNullOrEmpty(msg)) yield break;
        // no-op if devil wasn't cached at startup
        if (cachedDevilObject == null) yield break;

        // only handle messages that start with 'game'
        string t = msg.TrimStart();
        if (!t.StartsWith("game", StringComparison.OrdinalIgnoreCase)) yield break;

        // wait one second before restoring
        yield return new WaitForSeconds(1f);

        // use NavMeshAgent.Warp if available (cached in Awake/Start)
        if (cachedDevilNavAgent != null)
        {
            try
            {
                cachedDevilNavAgent.Warp(cachedPreviousDevilPosition);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RestoreDevil: NavMeshAgent.Warp failed: {ex.Message}");
                cachedDevilObject.transform.position = cachedPreviousDevilPosition;
            }
        }
        else
        {
            // fallback: temporarily disable CharacterController if present
            if (cachedDevilController != null)
            {
                bool prev = cachedDevilController.enabled;
                cachedDevilController.enabled = false;
                cachedDevilObject.transform.position = cachedPreviousDevilPosition;
                cachedDevilController.enabled = prev;
            }
            else
            {
                cachedDevilObject.transform.position = cachedPreviousDevilPosition;
            }
        }

        // restore rotation
        cachedDevilObject.transform.rotation = cachedPreviousDevilRotation;
    }


    void StartUIFade(float targetAlpha)
    {
        if (dialogTMP == null) return;

        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
            uiFadeCoroutine = null;
        }
        if (targetAlpha > 0f)
        {
            if (dialogTMP != null) dialogTMP.gameObject.SetActive(true);
        }
        uiFadeCoroutine = StartCoroutine(FadeTextTo(targetAlpha));
    }

    IEnumerator FadeTextTo(float targetAlpha)
    {
        float startAlphaTMP = dialogTMP != null ? dialogTMP.color.a : 0f;
        float elapsed = 0f;
        while (elapsed < uiFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / uiFadeDuration);
            if (dialogTMP != null)
            {
                float a2 = Mathf.Lerp(startAlphaTMP, targetAlpha, t);
                Color c2 = uiTMPOriginalColor;
                c2.a = a2;
                dialogTMP.color = c2;
            }
            yield return null;
        }

        if (dialogTMP != null)
        {
            Color final2 = uiTMPOriginalColor;
            final2.a = targetAlpha;
            dialogTMP.color = final2;
            if (targetAlpha == 0f) dialogTMP.gameObject.SetActive(false);
        }
        uiFadeCoroutine = null;
    }

    IEnumerator waitThenRelease(PlayerMovement playerMovement, DevilManagement devilManager, Vector3 previousDevilPosition)
    {
        // release after dialog finished
        // small delay to allow the last message to fade
        yield return new WaitForSeconds(0.2f);
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

            if (devilManager != null)
        {
            devilManager.enabled = true;
            // we reset the devil's position to its spawn point
            if (cachedDevilNavAgent != null)
            {
                // use NavMeshAgent.Warp to move agent safely
                cachedDevilNavAgent.Warp(previousDevilPosition);
            }
            else
            {
                devilManager.transform.position = previousDevilPosition;
            }
            // we re-enable the devil's emotes
            devilManager.autoShout = true;
            devilManager.autoLaugh = true;
            // clear dialog UI if used
            if (dialogTMP != null)
                dialogTMP.text = "";
            // restore devil rotation
            if (devilManager != null)
                devilManager.transform.rotation = cachedPreviousDevilRotation;

            // restore CharacterController enabled state if we toggled it
            if (cachedDevilController != null)
            {
                cachedDevilController.enabled = cachedDevilControllerEnabled;
                cachedDevilController = null;
            }
            // restore NavMeshAgent state if we modified it
            if (cachedDevilNavAgent != null)
            {
                cachedDevilNavAgent.isStopped = cachedDevilNavAgentStopped;
                cachedDevilNavAgent = null;
            }
            // restore the main camera position/rotation saved earlier
            if (moveCameraToSide && Camera.main != null)
            {
                // restore parent first (if any) so reparented camera picks up hierarchy properly
                Camera.main.transform.parent = previousCameraParent;
                Camera.main.transform.position = previousCameraPosition;
                Camera.main.transform.rotation = previousCameraRotation;

                if (cachedCameraController != null)
                    cachedCameraController.enabled = true;
            }
        }
    }


    // Update is called once per frame
    void Update()
    {

        if (!dialogActive)
            return;

        // immediate skip when P is pressed
        if (skipAction != null && skipAction.triggered)
        {
            StopPraying();
            SkipDialog();
            return;
        }

        bool advanced = false;
        if (advanceAction != null)
        {
            advanced = advanceAction.triggered;
        }
        else if (Keyboard.current != null)
        {
            // fallback to any key
            advanced = Keyboard.current.anyKey.wasPressedThisFrame;
        }

        if (advanced)
        {
            // if currently typing a message, finish it immediately
            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
                typeCoroutine = null;
                // show full message immediately
                if (dialogTMP != null) dialogTMP.text = dialogMessages[currentMessageIndex];
                return;
            }

            currentMessageIndex++;
            if (currentMessageIndex >= dialogMessages.Length)
            {
                Debug.Log("Dialog finished.");
                dialogActive = false;
                // clear the DoorHint message quickly
                if (dialogTMP != null)
                {
                    dialogTMP.text = "";
                    StartUIFade(0f);
                }
                // destroy skip button when dialog ends
                DestroySkipButton();
                if (DoorHint.Instance != null)
                    DoorHint.Instance.ShowMessage("", 0.1f);
                StartCoroutine(waitThenRelease(cachedPlayerMovement, cachedDevilManager, cachedPreviousDevilPosition));
            }
            else
            {
                Debug.Log($"Dialog advanced to {currentMessageIndex}: {dialogMessages[currentMessageIndex]}");
                ShowCurrentMessage();
            }
        }
    }

    void OnDestroy()
    {
        if (advanceAction != null)
        {
            advanceAction.Disable();
            advanceAction.Dispose();
            advanceAction = null;
        }
        if (skipAction != null)
        {
            skipAction.Disable();
            skipAction.Dispose();
            skipAction = null;
        }
    }

    public void SkipDialog()
    {
        Debug.Log("Dialog skipped by user.");
        // ensure praying animation/trigger is stopped when skipping
        StopPraying();

        dialogActive = false;
        // stop typing if running
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }
        if (dialogTMP != null)
        {
            dialogTMP.text = "";
            StartUIFade(0f);
        }
        if (DoorHint.Instance != null)
            DoorHint.Instance.ShowMessage("", 0.1f);
        // destroy configured skip button when skipping
        DestroySkipButton();
        StartCoroutine(waitThenRelease(cachedPlayerMovement, cachedDevilManager, cachedPreviousDevilPosition));
    }

    // destroy any cached skip buttons
    private void DestroySkipButton()
    {
        if (skipButton == null) return;
        try
        {
            if (skipButton.gameObject != null)
                Destroy(skipButton.gameObject);
            else
                Destroy(skipButton);
        }
        catch (Exception) { }
        skipButton = null;
    }

    // stop the praying animation/trigger on the player Animator (safe no-op if missing)
    private void StopPraying()
    {
        if (cachedPlayerAnimator == null) return;
        try
        {
            // clear any start trigger and fire the stop trigger so Animator can transition out
            cachedPlayerAnimator.ResetTrigger("startPraying");
            cachedPlayerAnimator.SetTrigger("stopPraying");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"StopPraying: failed to send stopPraying trigger: {ex.Message}");
        }
    }
}
