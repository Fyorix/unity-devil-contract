using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public string itemName = "Item";
    public string itemDescription = "";

    public abstract void OnPickup(Inventory inventory);
    public abstract void OnDrop();
}
