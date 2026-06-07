using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionFarmPlot : MonoBehaviour
    {
        [SerializeField] private ExtractionItemData cropItem;
        [SerializeField] private float growSeconds = 30f;
        [SerializeField] private int harvestQuantity = 1;

        private float plantedAt = -1f;

        public bool IsPlanted => cropItem != null && plantedAt >= 0f;
        public bool CanHarvest => IsPlanted && Time.time >= plantedAt + growSeconds;

        public bool Plant(ExtractionItemData crop)
        {
            if (IsPlanted || crop == null)
            {
                return false;
            }

            cropItem = crop;
            plantedAt = Time.time;
            return true;
        }

        public bool TryHarvest(ExtractionBaseStorage storage)
        {
            if (!CanHarvest || storage == null)
            {
                return false;
            }

            bool added = storage.AddItem(cropItem, harvestQuantity);
            if (added)
            {
                cropItem = null;
                plantedAt = -1f;
            }

            return added;
        }

        private void OnValidate()
        {
            growSeconds = Mathf.Max(0f, growSeconds);
            harvestQuantity = Mathf.Max(1, harvestQuantity);
        }
    }
}
