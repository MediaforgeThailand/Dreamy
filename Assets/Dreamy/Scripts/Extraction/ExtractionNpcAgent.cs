using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionNpcAgent : MonoBehaviour
    {
        [SerializeField] private ExtractionNpcData npcData;

        public ExtractionNpcData NpcData => npcData;

        public void OfferQuests(ExtractionQuestLog questLog)
        {
            if (npcData == null || questLog == null)
            {
                return;
            }

            for (int i = 0; i < npcData.Quests.Count; i++)
            {
                questLog.TryAcceptQuest(npcData.Quests[i]);
            }
        }
    }
}
