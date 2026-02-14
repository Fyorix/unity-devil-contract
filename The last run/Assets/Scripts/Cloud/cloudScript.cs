using UnityEngine;

public class CloudScroll : MonoBehaviour
{
    public float speedX = 0.02f;
    public float speedY = 0.01f;

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float offsetX = Time.time * speedX;
        float offsetY = Time.time * speedY;
        rend.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
    }
}
