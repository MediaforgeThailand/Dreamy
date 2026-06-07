using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionBaseUpgradeStation : MonoBehaviour
    {
        [SerializeField] private List<ExtractionBaseUpgradeData> upgrades = new List<ExtractionBaseUpgradeData>();
        [SerializeField] private List<int> tiers = new List<int>();

        public bool TryUpgrade(ExtractionBaseUpgradeData upgrade, ExtractionBaseStorage storage)
        {
            if (upgrade == null || storage == null)
            {
                return false;
            }

            int index = upgrades.IndexOf(upgrade);
            if (index < 0)
            {
                upgrades.Add(upgrade);
                tiers.Add(0);
                index = upgrades.Count - 1;
            }

            if (tiers[index] >= upgrade.MaxTier || !storage.Container.ConsumeItems(upgrade.Cost))
            {
                return false;
            }

            tiers[index]++;
            return true;
        }

        private void OnValidate()
        {
            while (tiers.Count < upgrades.Count)
            {
                tiers.Add(0);
            }

            if (tiers.Count > upgrades.Count)
            {
                tiers.RemoveRange(upgrades.Count, tiers.Count - upgrades.Count);
            }
        }
    }
}
