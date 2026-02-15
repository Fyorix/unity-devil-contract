using System;
using UnityEngine;
using UnityEngine.AI;

public class DevilManagement : MonoBehaviour
{
    [Header("Target")]
    public Transform targetPlayer;
    private NavMeshAgent agent;

    public float detectionRange;
    public LayerMask obstacleMask;

    [Header("Speed")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float timeFactorSpeed = 0.0001f;
    public float maxChasingSpeed = 6f;
    public float maxPatrolingSpeed = 3f;

    [Header("Patrol")]
    public float escapeCooldown = 3f;
    private float lastTimeSeen;
    private bool isChasing;
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;


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

        agent.speed = isChasing ? chaseSpeed : patrolSpeed;
        chaseSpeed = Math.Min(chaseSpeed + timeFactorSpeed, maxChasingSpeed);
        patrolSpeed = Math.Min(patrolSpeed + timeFactorSpeed, maxPatrolingSpeed);

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
}
