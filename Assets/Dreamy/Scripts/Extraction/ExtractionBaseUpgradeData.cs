using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Base Upgrade", fileName = "BaseUpgrade")]
    public sealed class ExtractionBaseUpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private string displayName;
        [SerializeField] private List<ExtractionItemStack> cost = new List<ExtractionItemStack>();
        [SerializeField] private int maxTier = 1;

        public string UpgradeId => string.IsNullOrWhiteSpace(upgradeId) ? name : upgradeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<ExtractionItemStack> Cost => cost;
        public int MaxTier => maxTier;

        private void OnValidate()
        {
            maxTier = Mathf.Max(1, maxTier);
            cost.RemoveAll(item => item == null || !item.IsValid);
        }
    }
}
