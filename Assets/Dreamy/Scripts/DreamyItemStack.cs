using System;
using UnityEngine;

namespace Dreamy
{
    [Serializable]
    public sealed class DreamyItemStack
    {
        [SerializeField] private DreamyItemId itemId = DreamyItemId.Wood;
        [SerializeField] private string displayName;
        [SerializeField] private int quantity = 1;

        public DreamyItemId ItemId => itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? itemId.ToString() : displayName;
        public int Quantity => Mathf.Max(1, quantity);
        public bool IsValid => quantity > 0;

        public DreamyItemStack()
        {
        }

        public DreamyItemStack(DreamyItemId itemId, string displayName, int quantity)
        {
            this.itemId = itemId;
            this.displayName = displayName;
            this.quantity = Mathf.Max(1, quantity);
        }

        public void Validate()
        {
            quantity = Mathf.Max(1, quantity);
        }
    }
}
