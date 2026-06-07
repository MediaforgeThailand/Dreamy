using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionMapUnlockService : MonoBehaviour
    {
        [SerializeField] private List<ExtractionMapData> unlockedMaps = new List<ExtractionMapData>();

        public IReadOnlyList<ExtractionMapData> UnlockedMaps => unlockedMaps;

        public bool IsUnlocked(ExtractionMapData map)
        {
            if (map == null)
            {
                return false;
            }

            return map.UnlockRequirementItem == null || unlockedMaps.Contains(map);
        }

        public bool TryUnlock(ExtractionMapData map, ExtractionBaseStorage storage)
        {
            if (map == null)
            {
                return false;
            }

            if (unlockedMaps.Contains(map))
            {
                return true;
            }

            ExtractionItemData requirement = map.UnlockRequirementItem;
            if (requirement != null)
            {
                if (storage == null || storage.Container.GetQuantity(requirement) <= 0)
                {
                    return false;
                }
            }

            unlockedMaps.Add(map);
            return true;
        }
    }
}
