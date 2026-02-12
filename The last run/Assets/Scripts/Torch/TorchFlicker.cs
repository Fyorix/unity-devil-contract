using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    public Light torchLight;
    public float minIntensity = 2.5f;
    public float maxIntensity = 4f;

    void Update()
    {
        torchLight.intensity = Random.Range(minIntensity, maxIntensity);
    }
}