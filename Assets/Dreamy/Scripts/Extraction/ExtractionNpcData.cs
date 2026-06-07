using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/NPC Data", fileName = "NpcData")]
    public sealed class ExtractionNpcData : ScriptableObject
    {
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private List<ExtractionQuestData> quests = new List<ExtractionQuestData>();

        public string NpcId => string.IsNullOrWhiteSpace(npcId) ? name : npcId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<ExtractionQuestData> Quests => quests;
    }
}
