using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    [Serializable]
    public sealed class ExtractionItemStack
    {
        [SerializeField] private ExtractionItemData item;
        [SerializeField] private int quantity = 1;

        public ExtractionItemData Item => item;
        public int Quantity => quantity;
        public bool IsValid => item != null && quantity > 0;

        public ExtractionItemStack()
        {
        }

        public ExtractionItemStack(ExtractionItemData item, int quantity)
        {
            this.item = item;
            this.quantity = Mathf.Max(0, quantity);
        }

        public ExtractionItemStack Clone()
        {
            return new ExtractionItemStack(item, quantity);
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
}
