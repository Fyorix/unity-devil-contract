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
        gameObject.SetActive(false);
    }

    public override void OnDrop()
    {
        Debug.Log($"Dropped key: {keyId}");
        gameObject.SetActive(true);
    }
}
