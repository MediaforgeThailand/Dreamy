using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [Serializable]
    public sealed class ExtractionLootEntry
    {
        [SerializeField] private ExtractionItemData item;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 1;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

        public ExtractionItemData Item => item;

        public bool TryRoll(out ExtractionItemStack stack)
        {
            stack = null;
            if (item == null || UnityEngine.Random.value > dropChance)
            {
                return false;
            }

            int quantity = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
            if (quantity <= 0)
            {
                return false;
            }

            stack = new ExtractionItemStack(item, quantity);
            return true;
        }

        public void Validate()
        {
            minQuantity = Mathf.Max(0, minQuantity);
            maxQuantity = Mathf.Max(minQuantity, maxQuantity);
            dropChance = Mathf.Clamp01(dropChance);
        }
    }

    [CreateAssetMenu(menuName = "Dreamy/Extraction/Loot Table", fileName = "LootTable")]
    public sealed class ExtractionLootTableData : ScriptableObject
    {
        [SerializeField] private List<ExtractionLootEntry> entries = new List<ExtractionLootEntry>();

        public IReadOnlyList<ExtractionLootEntry> Entries => entries;

        public List<ExtractionItemStack> Roll()
        {
            List<ExtractionItemStack> result = new List<ExtractionItemStack>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].TryRoll(out ExtractionItemStack stack))
                {
                    result.Add(stack);
                }
            }

            return result;
        }

        private void OnValidate()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i]?.Validate();
            }
        }
    }
}
