using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionRecoveryService : MonoBehaviour
    {
        public bool HasRecoverableLoot()
        {
            return ExtractionGameSession.Instance != null
                && ExtractionGameSession.Instance.LostLootRecords.Count > 0;
        }

        public int RecoverableLootCount()
        {
            return ExtractionGameSession.Instance != null
                ? ExtractionGameSession.Instance.LostLootRecords.Count
                : 0;
        }
    }
}
