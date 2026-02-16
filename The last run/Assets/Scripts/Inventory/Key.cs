using UnityEngine;

public class Key : Item
{
    public string keyId = "key_default";

    void Start()
    {
        itemName = "Key";
        itemDescription = $"A key with ID: {keyId}";
    }

    public override void OnPickup(Inventory inventory)
    {
        Debug.Log($"Picked up key: {keyId}");
        // Disable physics and hide when picked up
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        gameObject.SetActive(false);
    }

    public override void OnDrop()
    {
        Debug.Log($"Dropped key: {keyId}");
        // Re-enable physics and collider when dropped so gravity applies
        gameObject.SetActive(true);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
