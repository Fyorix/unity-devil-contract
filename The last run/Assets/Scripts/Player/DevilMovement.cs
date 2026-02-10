using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DevilMovement : MonoBehaviour
{
    public Transform targetPlayer;
    public float chaseSpeed = 5f;
    public float gravity = -15f;

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

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- Chase Player ---
        if (targetPlayer != null)
        {
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            
            // Flatten direction to avoid flying/sinking
            directionToPlayer.y = 0f;
            directionToPlayer.Normalize();

            // Move towards player
            controller.Move(directionToPlayer * chaseSpeed * Time.deltaTime);
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
