using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DevilMovement : MonoBehaviour
{
    public Animator animator;   
    public Transform targetPlayer;
    public float chaseSpeed = 5f;
    public float gravity = -15f;

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Emotes")]
    public float shoutDuration = 2.833f; // seconds for the shout emote, has to match the animation length
    public float laughDuration = 9.75f; // seconds for the laugh emote
    private bool isEmoting = false;
    // Run activation flag: walk by default, run only when activated via API
    private bool runActive = false;
    [Header("Auto Shout")]
    public bool autoShout = true;
    public float minShoutInterval = 40f;
    public float maxShoutInterval = 60f;
    private Coroutine shoutLoopCoroutine;

    [Header("Auto Laugh")]
    public bool autoLaugh = false;
    public float minLaughInterval = 20f;
    public float maxLaughInterval = 30f;
    private Coroutine laughLoopCoroutine;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Auto-find player if not assigned
        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetPlayer = player.transform;
            }
        }
    }

    void Start()
    {
        if (autoShout)
        {
            shoutLoopCoroutine = StartCoroutine(RandomShoutLoop());
        }
        if (autoLaugh)
        {
            laughLoopCoroutine = StartCoroutine(RandomLaughLoop());
        }
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // If emoting, freeze horizontal movement (still apply gravity)
        if (isEmoting)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        // --- Chase Player ---
        if (targetPlayer != null)
        {
            Vector3 directionToPlayer = targetPlayer.position - transform.position;
            directionToPlayer.y = 0f;
            directionToPlayer.Normalize();

            // Walk by default. Running only when activated via API.
            float currentSpeed = runActive ? runSpeed : walkSpeed;
            animator.SetBool("isRunning", runActive);
            animator.SetBool("isWalking", !runActive);

            // Rotate to face player
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }

            // Move towards player
            controller.Move(directionToPlayer * currentSpeed * Time.deltaTime);
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Public method to trigger the shout emote; movement is frozen for shoutDuration seconds.
    public void TriggerShoutEmote()
    {
        if (!isEmoting)
        {
            StartCoroutine(EmoteCoroutine("Shout", shoutDuration));
        }
    }

    // Public method to trigger the laugh emote.
    public void TriggerLaughEmote()
    {
        if (!isEmoting)
        {
            StartCoroutine(EmoteCoroutine("Laugh", laughDuration));
        }
    }

    // API to enable/disable running externally
    public void ActivateRun()
    {
        runActive = true;
    }

    public void DeactivateRun()
    {
        runActive = false;
    }

    IEnumerator EmoteCoroutine(string triggerName, float duration)
    {
        isEmoting = true;
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
        yield return new WaitForSeconds(duration);
        isEmoting = false;
    }

    IEnumerator RandomShoutLoop()
    {
        while (true)
        {
            float wait = Random.Range(minShoutInterval, maxShoutInterval);
            yield return new WaitForSeconds(wait);
            if (!isEmoting)
            {
                TriggerShoutEmote();
            }
        }
    }

    IEnumerator RandomLaughLoop()
    {
        while (true)
        {
            float wait = Random.Range(minLaughInterval, maxLaughInterval);
            yield return new WaitForSeconds(wait);
            if (!isEmoting)
            {
                TriggerLaughEmote();
            }
        }
    }

    void OnDisable()
    {
        if (shoutLoopCoroutine != null)
        {
            StopCoroutine(shoutLoopCoroutine);
            shoutLoopCoroutine = null;
        }
        if (laughLoopCoroutine != null)
        {
            StopCoroutine(laughLoopCoroutine);
            laughLoopCoroutine = null;
        }
    }
}
