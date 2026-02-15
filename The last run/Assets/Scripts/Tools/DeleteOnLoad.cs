using UnityEngine;

public class DestroyThisComponentOnStart : MonoBehaviour
{
    void Start()
    {
        Destroy(this);
    }
}