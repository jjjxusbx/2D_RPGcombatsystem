using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Item", menuName = "Demo/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int buyPrice = 1;
    public int sellPrice = 1;
    public int maxStack = 99;
    public bool canBuy = true;
    public bool canSell = true;
}

[Serializable]
public class InventorySlot
{
    public ItemSO item;
    public int quantity;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Data")]
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int inventorySize = 20;

    [Header("Events")]
    public UnityEvent onInventoryChanged;
    public UnityEvent<int> onMoneyChanged;

    public event Action InventoryChanged;
    public event Action<int> MoneyChanged;

    [SerializeField] private int currentMoney = 1000;

    public int CurrentMoney => currentMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSlotCapacity();
    }

    private void OnValidate()
    {
        EnsureSlotCapacity();
    }

    public bool BuyItem(ItemSO item, int count)
    {
        if (item == null || count <= 0 || !item.canBuy || item.buyPrice < 0)
        {
            return false;
        }

        long totalCost = (long)item.buyPrice * count;
        if (totalCost > int.MaxValue || currentMoney < totalCost)
        {
            return false;
        }

        EnsureSlotCapacity();

        int remaining = count;
        List<(int index, int addCount)> plan = new List<(int, int)>();

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.item != item)
            {
                continue;
            }

            int stackLimit = Mathf.Max(1, item.maxStack);
            int space = Mathf.Max(0, stackLimit - slot.quantity);
            if (space <= 0)
            {
                continue;
            }

            int add = Mathf.Min(space, remaining);
            plan.Add((i, add));
            remaining -= add;
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.item != null)
            {
                continue;
            }

            int stackLimit = Mathf.Max(1, item.maxStack);
            int add = Mathf.Min(stackLimit, remaining);
            plan.Add((i, add));
            remaining -= add;
        }

        if (remaining > 0)
        {
            return false;
        }

        foreach ((int index, int addCount) in plan)
        {
            InventorySlot slot = slots[index];
            if (slot.item == null)
            {
                slot.item = item;
                slot.quantity = 0;
            }

            slot.quantity += addCount;
        }

        currentMoney -= (int)totalCost;
        NotifyMoneyChanged();
        NotifyInventoryChanged();
        return true;
    }

    public void SellItem(int slotIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureSlotCapacity();

        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return;
        }

        InventorySlot slot = slots[slotIndex];
        if (slot.item == null || !slot.item.canSell || slot.item.sellPrice < 0)
        {
            return;
        }

        if (slot.quantity < count)
        {
            return;
        }

        long totalGain = (long)slot.item.sellPrice * count;
        if (totalGain > int.MaxValue || currentMoney > int.MaxValue - totalGain)
        {
            return;
        }

        slot.quantity -= count;
        if (slot.quantity <= 0)
        {
            slot.quantity = 0;
            slot.item = null;
        }

        currentMoney += (int)totalGain;
        NotifyMoneyChanged();
        NotifyInventoryChanged();
    }

    public int GetUsedSlotCount()
    {
        EnsureSlotCapacity();
        int used = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].quantity > 0)
            {
                used++;
            }
        }

        return used;
    }

    private void EnsureSlotCapacity()
    {
        int size = Mathf.Max(0, inventorySize);

        if (slots == null)
        {
            slots = new List<InventorySlot>(size);
        }

        while (slots.Count < size)
        {
            slots.Add(new InventorySlot());
        }

        while (slots.Count > size && IsEmptySlot(slots[slots.Count - 1]))
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    private static bool IsEmptySlot(InventorySlot slot)
    {
        return slot == null || slot.item == null || slot.quantity <= 0;
    }

    private void NotifyInventoryChanged()
    {
        onInventoryChanged?.Invoke();
        InventoryChanged?.Invoke();
    }

    private void NotifyMoneyChanged()
    {
        onMoneyChanged?.Invoke(currentMoney);
        MoneyChanged?.Invoke(currentMoney);
    }
}

