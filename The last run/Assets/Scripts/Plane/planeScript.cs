using UnityEngine;

public class planeScript : MonoBehaviour
{

    public Transform player;
    [Header("Timing")]
    public float timeBeforeChase = 5f;

    [Header("Flight")]
    public float cruiseSpeed = 20f;
    public float chaseSpeed = 35f;
    public float turnSpeed = 2f;
    public float descendSpeed = 10f;

    private float timer;
    private bool chasing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeBeforeChase)
            chasing = true;

        if (!chasing)
            Cruise();
        else
            Chase();
    }

    void Cruise()
    {
        // avance tout droit
        transform.position += transform.forward * cruiseSpeed * Time.deltaTime;
    }

    void Chase()
    {
        // direction vers le joueur
        Vector3 targetDir = (player.position - transform.position).normalized;

        // rotation progressive (effet avion)
        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );

        // avancer
        transform.position += transform.forward * chaseSpeed * Time.deltaTime;

        // descente progressive
        if (transform.position.y > player.position.y + 2f)
        {
            transform.position += Vector3.down * descendSpeed * Time.deltaTime;
        }
    }
}
