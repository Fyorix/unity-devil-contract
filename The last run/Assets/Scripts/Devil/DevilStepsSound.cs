using UnityEngine;

public class DevilStepsSound : MonoBehaviour
{
    [Header("Footstep Sound")]
    public AudioSource footstepAudioSource;
    public GameObject devil;
    public AudioClip footSteps;

    private Vector3 lastPosition;

    void Start()
    {
        footstepAudioSource.clip = footSteps;
        footstepAudioSource.loop = true;
        lastPosition = devil.transform.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(devil.transform.position, lastPosition);

        if (distance > 0.01f)
        {
            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Play();
            }
        }
        else
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
        lastPosition = devil.transform.position;
    }
}
