using UnityEngine;
using UnityEngine.UI;

public class KeySlotsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Image[] slotImages;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite filledSlotSprite;

    [Header("Settings")]
    [SerializeField] private int maxSlotsToShow = 2;

    void OnEnable()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<Inventory>();
        }

        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += Refresh;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= Refresh;
        }
    }

    void Refresh()
    {
        if (slotImages == null || slotImages.Length == 0)
        {
            return;
        }

        int keyCount = 0;
        if (playerInventory != null)
        {
            foreach (var item in playerInventory.GetAllItems())
            {
                if (item is Key)
                {
                    keyCount++;
                }
            }
        }

        int slots = Mathf.Min(maxSlotsToShow, slotImages.Length);
        for (int i = 0; i < slots; i++)
        {
            if (slotImages[i] == null)
            {
                continue;
            }

            bool filled = i < keyCount;
            slotImages[i].sprite = filled ? filledSlotSprite : emptySlotSprite;
            slotImages[i].enabled = slotImages[i].sprite != null;
        }

        // Ensure any remaining slot images beyond `slots` are disabled
        for (int i = slots; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;
            slotImages[i].sprite = null;
            slotImages[i].enabled = false;
        }
    }
}
