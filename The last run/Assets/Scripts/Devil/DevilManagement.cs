using System;
using UnityEngine;
using UnityEngine.AI;

public class DevilManagement : MonoBehaviour
{
    public Transform targetPlayer;
    private NavMeshAgent agent;

    public float detectionRange;
    public LayerMask obstacleMask;

    public float patrolSpeed = 3f;
    public float chaseSpeed = 3.5f;
    public float timeFactorSpeed = 0.0001f;
    public float maxSpeed = 6f;

    public Time escapeTime;
    private bool playerDetected;


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
    }

    // Update is called once per frame
    void Update()
    {
        playerDetected = IsPlayerInRange();
        agent.speed = playerDetected ? chaseSpeed : patrolSpeed;
        chaseSpeed = Math.Min(chaseSpeed + timeFactorSpeed, maxSpeed);
        patrolSpeed = Math.Min(patrolSpeed + timeFactorSpeed, maxSpeed);

        if (playerDetected && targetPlayer != null)
        {
            agent.SetDestination(targetPlayer.position);
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
}
