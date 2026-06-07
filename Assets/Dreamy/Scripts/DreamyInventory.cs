using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    public enum DreamyItemId
    {
        Wood,
        Gold,
        Food,
        Meat,
        Potion,
        ExpShard,
        Seed,
        Crop,
        CraftedMeal,
        CraftedTool,
        Coin,
        UnlockToken,
        SkillBook,
        Custom
    }

    [Serializable]
    public sealed class DreamyInventorySlot
    {
        [SerializeField] private DreamyItemId itemId;
        [SerializeField] private string displayName;
        [SerializeField] private int quantity;

        public DreamyItemId ItemId => itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? itemId.ToString() : displayName;
        public int Quantity => quantity;

        public DreamyInventorySlot()
        {
        }

        public DreamyInventorySlot(DreamyItemId itemId, string displayName, int quantity)
        {
            this.itemId = itemId;
            this.displayName = displayName;
            this.quantity = Mathf.Max(0, quantity);
        }

        public void Add(int amount)
        {
            quantity = Mathf.Max(0, quantity + amount);
        }

        public bool Remove(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (quantity < amount)
            {
                return false;
            }

            quantity -= amount;
            return true;
        }
    }

    public sealed class DreamyInventory : MonoBehaviour
    {
        [SerializeField] private int maxSlots = 60;
        [SerializeField] private List<DreamyInventorySlot> items = new List<DreamyInventorySlot>();

        public event Action InventoryChanged;

        public IReadOnlyList<DreamyInventorySlot> Items => items;
        public int MaxSlots
        {
            get => maxSlots;
            set
            {
                maxSlots = Mathf.Max(1, value);
                TrimToMaxSlots();
            }
        }

        public bool AddItem(DreamyItemId itemId, int amount, string displayName = null)
        {
            if (amount <= 0)
            {
                return true;
            }

            DreamyInventorySlot existing = FindSlot(itemId);
            if (existing != null)
            {
                existing.Add(amount);
                InventoryChanged?.Invoke();
                return true;
            }

            if (items.Count >= maxSlots)
            {
                return false;
            }

            items.Add(new DreamyInventorySlot(itemId, displayName, amount));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(DreamyItemId itemId, int amount)
        {
            DreamyInventorySlot slot = FindSlot(itemId);
            if (slot == null || !slot.Remove(amount))
            {
                return false;
            }

            items.RemoveAll(item => item.Quantity <= 0);
            InventoryChanged?.Invoke();
            return true;
        }

        public int GetQuantity(DreamyItemId itemId)
        {
            DreamyInventorySlot slot = FindSlot(itemId);
            return slot != null ? slot.Quantity : 0;
        }

        public bool TransferSlotTo(DreamyInventory target, int sourceIndex, int amount)
        {
            if (target == null || sourceIndex < 0 || sourceIndex >= items.Count)
            {
                return false;
            }

            DreamyInventorySlot slot = items[sourceIndex];
            if (slot == null || slot.Quantity <= 0)
            {
                return false;
            }

            int transferAmount = Mathf.Clamp(amount, 1, slot.Quantity);
            if (!target.AddItem(slot.ItemId, transferAmount, slot.DisplayName))
            {
                return false;
            }

            slot.Remove(transferAmount);
            items.RemoveAll(item => item == null || item.Quantity <= 0);
            InventoryChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            items.Clear();
            InventoryChanged?.Invoke();
        }

        private DreamyInventorySlot FindSlot(DreamyItemId itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ItemId == itemId)
                {
                    return items[i];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            maxSlots = Mathf.Max(1, maxSlots);
            TrimToMaxSlots();
        }

        private void TrimToMaxSlots()
        {
            items.RemoveAll(item => item == null || item.Quantity <= 0);
            if (items.Count > maxSlots)
            {
                items.RemoveRange(maxSlots, items.Count - maxSlots);
            }
        }
    }
}
