using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour
{
    public UIInventorySlot slotPrefab;
    public Transform content;

    [Header("Durability Colors")]
    public Color brokenDurabilityColor = Color.red;
    public Color lowDurabilityColor = Color.magenta;
    [Range(0.01f, 0.99f)] public float lowDurabilityThreshold = 0.1f;

    void Update()
    {
        GameObject player = PlayerMeta.localPlayer;
        if (player)
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();

            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, inventory.slots.Count, content);

            // refresh all items
            for (int i = 0; i < inventory.slots.Count; ++i)
            {
                UIInventorySlot slot = content.GetChild(i).GetComponent<UIInventorySlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                ItemSlot itemSlot = inventory.slots[i];

                if (itemSlot.amount > 0)
                {
                    // refresh valid item
                    slot.button.interactable = itemSlot.item.maxDurability == 0 || itemSlot.item.durability > 0;
                    int icopy = i; // needed for lambdas, otherwise i is Count
                    slot.button.onClick.SetListener(() => {
                        if (itemSlot.item.data is UsableItem &&
                            ((UsableItem)itemSlot.item.data).CanUse(inventory, icopy) == Usability.Usable)
                            inventory.CmdUseItem(icopy);
                    });
                    slot.tooltip.enabled = true;
                    slot.tooltip.text = itemSlot.ToolTip();
                    slot.dragAndDropable.dragable = true;
                    // use durability colors?
                    if (itemSlot.item.maxDurability > 0)
                    {
                        if (itemSlot.item.durability == 0)
                            slot.image.color = brokenDurabilityColor;
                        else if (itemSlot.item.DurabilityPercent() < lowDurabilityThreshold)
                            slot.image.color = lowDurabilityColor;
                        else
                            slot.image.color = Color.white;
                    }
                    else slot.image.color = Color.white;
                    slot.image.sprite = itemSlot.item.image;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    // refresh invalid item
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.amountOverlay.SetActive(false);
                }
            }
        }
    }
}
