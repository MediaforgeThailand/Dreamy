using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionLostLoot : MonoBehaviour
    {
        [SerializeField] private List<ExtractionItemStack> lostItems = new List<ExtractionItemStack>();

        public IReadOnlyList<ExtractionItemStack> LostItems => lostItems;

        public void SetLostItems(IEnumerable<ExtractionItemStack> items)
        {
            lostItems.Clear();
            if (items == null)
            {
                return;
            }

            foreach (ExtractionItemStack item in items)
            {
                if (item != null && item.IsValid)
                {
                    lostItems.Add(item.Clone());
                }
            }
        }
    }
}
