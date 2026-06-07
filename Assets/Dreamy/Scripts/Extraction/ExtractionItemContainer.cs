using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [Serializable]
    public sealed class ExtractionItemContainer
    {
        [SerializeField] private int slotLimit = 24;
        [SerializeField] private List<ExtractionItemStack> items = new List<ExtractionItemStack>();

        public event Action Changed;

        public IReadOnlyList<ExtractionItemStack> Items => items;
        public int SlotLimit => slotLimit;
        public bool IsEmpty => items.Count == 0;

        public bool AddItem(ExtractionItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return true;
            }

            ExtractionItemStack existing = FindStack(item);
            if (existing != null)
            {
                existing.Add(quantity);
                RemoveInvalidStacks();
                Changed?.Invoke();
                return true;
            }

            if (items.Count >= slotLimit)
            {
                return false;
            }

            items.Add(new ExtractionItemStack(item, quantity));
            Changed?.Invoke();
            return true;
        }

        public bool AddItems(IEnumerable<ExtractionItemStack> stacks)
        {
            if (stacks == null)
            {
                return true;
            }

            List<ExtractionItemStack> snapshot = CreateSnapshot();
            foreach (ExtractionItemStack stack in stacks)
            {
                if (stack == null || !AddItem(stack.Item, stack.Quantity))
                {
                    SetFromSnapshot(snapshot);
                    return false;
                }
            }

            return true;
        }

        public bool RemoveItem(ExtractionItemData item, int quantity)
        {
            ExtractionItemStack stack = FindStack(item);
            if (stack == null || !stack.Remove(quantity))
            {
                return false;
            }

            RemoveInvalidStacks();
            Changed?.Invoke();
            return true;
        }

        public int GetQuantity(ExtractionItemData item)
        {
            ExtractionItemStack stack = FindStack(item);
            return stack != null ? stack.Quantity : 0;
        }

        public bool HasItems(IEnumerable<ExtractionItemStack> costs)
        {
            if (costs == null)
            {
                return true;
            }

            foreach (ExtractionItemStack cost in costs)
            {
                if (cost != null && GetQuantity(cost.Item) < cost.Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        public bool ConsumeItems(IEnumerable<ExtractionItemStack> costs)
        {
            if (!HasItems(costs))
            {
                return false;
            }

            if (costs == null)
            {
                return true;
            }

            foreach (ExtractionItemStack cost in costs)
            {
                if (cost != null)
                {
                    RemoveItem(cost.Item, cost.Quantity);
                }
            }

            return true;
        }

        public bool TransferAllTo(ExtractionItemContainer destination)
        {
            if (destination == null || items.Count == 0)
            {
                return false;
            }

            List<ExtractionItemStack> snapshot = CreateSnapshot();
            if (destination.AddItems(snapshot))
            {
                Clear();
                return true;
            }

            return false;
        }

        public List<ExtractionItemStack> CreateSnapshot()
        {
            List<ExtractionItemStack> snapshot = new List<ExtractionItemStack>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].IsValid)
                {
                    snapshot.Add(items[i].Clone());
                }
            }

            return snapshot;
        }

        public void SetFromSnapshot(IEnumerable<ExtractionItemStack> snapshot)
        {
            items.Clear();
            if (snapshot != null)
            {
                foreach (ExtractionItemStack stack in snapshot)
                {
                    if (stack != null && stack.IsValid)
                    {
                        items.Add(stack.Clone());
                    }
                }
            }

            TrimToSlotLimit();
            Changed?.Invoke();
        }

        public void Clear()
        {
            items.Clear();
            Changed?.Invoke();
        }

        public void Validate()
        {
            slotLimit = Mathf.Max(1, slotLimit);
            RemoveInvalidStacks();
            TrimToSlotLimit();
        }

        private ExtractionItemStack FindStack(ExtractionItemData item)
        {
            if (item == null)
            {
                return null;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Item == item)
                {
                    return items[i];
                }
            }

            return null;
        }

        private void RemoveInvalidStacks()
        {
            items.RemoveAll(stack => stack == null || !stack.IsValid);
        }

        private void TrimToSlotLimit()
        {
            if (items.Count > slotLimit)
            {
                items.RemoveRange(slotLimit, items.Count - slotLimit);
            }
        }
    }
}
