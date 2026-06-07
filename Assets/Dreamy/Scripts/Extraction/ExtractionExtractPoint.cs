using System;
using UnityEngine;

namespace Dreamy.Extraction
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExtractionExtractPoint : MonoBehaviour
    {
        [SerializeField] private ExtractionBaseStorage baseStorage;
        [SerializeField] private bool extractOnTriggerEnter = true;

        public event Action Extracted;

        private void Awake()
        {
            Collider2D hitbox = GetComponent<Collider2D>();
            hitbox.isTrigger = true;
            if (baseStorage == null)
            {
                ExtractionGameSession session = ExtractionGameSession.GetOrCreate();
                baseStorage = session != null && session.BaseStorage != null
                    ? session.BaseStorage
                    : UnityEngine.Object.FindAnyObjectByType<ExtractionBaseStorage>();
            }
        }

        public void SetBaseStorage(ExtractionBaseStorage storage)
        {
            baseStorage = storage;
        }

        public bool TryExtract(ExtractionRunInventory runInventory)
        {
            if (runInventory == null || baseStorage == null || runInventory.Container.IsEmpty)
            {
                return false;
            }

            bool transferred = runInventory.Container.TransferAllTo(baseStorage.Container);
            if (transferred)
            {
                Extracted?.Invoke();
            }

            return transferred;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!extractOnTriggerEnter)
            {
                return;
            }

            TryExtract(other.GetComponentInParent<ExtractionRunInventory>());
        }
    }
}
