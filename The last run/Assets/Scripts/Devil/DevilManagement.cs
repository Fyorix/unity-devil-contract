using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class DevilManagement : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPlayer;
    private NavMeshAgent agent;

    public float detectionRange;
    public LayerMask obstacleMask;
    public string onContactSceneName = "playMenuScene";

    [Header("Speed")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float runSpeed = 5f;
    public float timeFactorSpeed = 0.0001f;
    public float maxChasingSpeed = 6f;
    public float maxPatrolingSpeed = 3f;

    [Header("Run Logic")]
    public float runDuration = 4f;
    public float minRunCooldown = 15f;
    public float maxRunCooldown = 30f;

    private bool isRunning = false;
    private float nextRunTime = 0f;
    private float runEndTime = 0f;


    [Header("Patrol")]
    public float escapeCooldown = 3f;
    private float lastTimeSeen;
    private bool isChasing;
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Animations")]
    public Animator animator;

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

    [Header("Jumpscare Setup")]
    public GameObject jumpScareUI; 
    public AudioSource screamSound;
    public float jumpscareDuration = 2.5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetPlayer = player.transform;
        }
        agent.speed = patrolSpeed;
        isChasing = false;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        if (autoShout)
        {
            shoutLoopCoroutine = StartCoroutine(RandomShoutLoop());
        }
        if (autoLaugh)
        {
            laughLoopCoroutine = StartCoroutine(RandomLaughLoop());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPlayerInRange())
        {
            isChasing = true;
            lastTimeSeen = Time.time;
        }
        else
        {
            if (Time.time - lastTimeSeen > escapeCooldown)
            {
                isChasing = false;
            }
        }

        HandleRunLogic();

        if (isChasing)
        {
            agent.speed = isRunning ? runSpeed : chaseSpeed;
        }
        else
        {
            agent.speed = patrolSpeed;
        }

        chaseSpeed = Math.Min(chaseSpeed + timeFactorSpeed, maxChasingSpeed);
        patrolSpeed = Math.Min(patrolSpeed + timeFactorSpeed, maxPatrolingSpeed);

        // If emoting, freeze horizontal movement (still apply gravity)
        if (isEmoting)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            // Break agent
            agent.isStopped = true;
            return;
        }
        else
        {
            agent.isStopped = false;
            float speed = agent.velocity.magnitude;

            animator.SetBool("isRunning", isRunning);
            animator.SetBool("isWalking", !isRunning && speed > 0.1f);


        }


        if (isChasing && targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.position);
        }
        else
        {
            Patrol();
        }
    }

    bool IsPlayerInRange()
    {
        if (targetPlayer == null) return false;

        Vector3 directionToPlayer = targetPlayer.position - transform.position;
        float distance = directionToPlayer.magnitude;

        if (distance > detectionRange)
            return false;

        // Vérifie qu'il n'y a pas d'obstacle entre les deux
        if (!CanSeePlayer())
        {
            return false;
        }

        return true;
    }

    bool CanSeePlayer()
    {
        if (targetPlayer == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPlayer.position + Vector3.up * 1f) - origin;

        float distance = direction.magnitude;

        if (distance > detectionRange)
            return false;

        direction.Normalize();

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance))
        {
            // On détecte seulement si le premier objet touché est le player
            if (hit.transform == targetPlayer)
                return true;
        }

        return false;
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            Debug.Log("Agent at patrol point nb" + currentPatrolIndex);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    // 2. Remplace ta fonction OnCollisionEnter par celle-ci
    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform == targetPlayer)
        {
            // On lance la coroutine pour gérer le délai
            StartCoroutine(ExecuteJumpScare());
        }
    }

    // 3. Ajoute cette coroutine à la fin de ton script
    IEnumerator ExecuteJumpScare()
    {
        // Affiche l'image et joue le son
        if (jumpScareUI != null) jumpScareUI.SetActive(true);
        if (screamSound != null) screamSound.Play();

        // On bloque le démon pour qu'il ne continue pas à avancer
        agent.isStopped = true;
        isEmoting = true; 

        // On attend la fin du temps défini (ex: 2.5s)
        // On utilise WaitForSecondsRealtime au cas où le TimeScale serait modifié
        yield return new WaitForSecondsRealtime(jumpscareDuration);

        // Enfin, on change de scène
        SceneManager.LoadScene(onContactSceneName);
    }

    void HandleRunLogic()
    {
        if (!isChasing)
        {
            isRunning = false;
            return;
        }

        float time = Time.time;

        // Si on court et que le temps est fini
        if (isRunning && time >= runEndTime)
        {
            isRunning = false;
            nextRunTime = time + UnityEngine.Random.Range(minRunCooldown, maxRunCooldown);
        }

        // Si on ne court pas et qu'on peut relancer un run
        if (!isRunning && time >= nextRunTime)
        {
            isRunning = true;
            runEndTime = time + runDuration;
        }
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
            float wait = UnityEngine.Random.Range(minShoutInterval, maxShoutInterval);
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
            float wait = UnityEngine.Random.Range(minLaughInterval, maxLaughInterval);
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
