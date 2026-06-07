using System.Collections.Generic;
using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionRunInventory : MonoBehaviour
    {
        [SerializeField] private ExtractionItemContainer container = new ExtractionItemContainer();

        public event Action Changed;

        public ExtractionItemContainer Container => container;
        public IReadOnlyList<ExtractionItemStack> Items => container.Items;

        private void OnEnable()
        {
            container.Changed += HandleContainerChanged;
        }

        private void OnDisable()
        {
            container.Changed -= HandleContainerChanged;
        }

        public bool AddItem(ExtractionItemData item, int quantity)
        {
            return container.AddItem(item, quantity);
        }

        public bool AddItems(IEnumerable<ExtractionItemStack> items)
        {
            return container.AddItems(items);
        }

        public List<ExtractionItemStack> CreateSnapshot()
        {
            return container.CreateSnapshot();
        }

        public void Clear()
        {
            container.Clear();
        }

        private void OnValidate()
        {
            container.Validate();
        }

        private void HandleContainerChanged()
        {
            Changed?.Invoke();
        }
    }
}
