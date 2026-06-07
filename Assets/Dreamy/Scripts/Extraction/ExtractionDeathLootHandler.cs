using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [RequireComponent(typeof(ExtractionHealth))]
    [RequireComponent(typeof(ExtractionRunInventory))]
    public sealed class ExtractionDeathLootHandler : MonoBehaviour
    {
        [SerializeField] private ExtractionLootPickup lostLootPrefab;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -0.2f);
        [SerializeField] private ExtractionRoomFlowController roomFlow;

        private ExtractionHealth health;
        private ExtractionRunInventory runInventory;

        private void Awake()
        {
            health = GetComponent<ExtractionHealth>();
            runInventory = GetComponent<ExtractionRunInventory>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied()
        {
            List<ExtractionItemStack> lostItems = runInventory.CreateSnapshot();
            ExtractionGameSession.GetOrCreate().RecordLostLoot(
                lostItems,
                roomFlow != null ? roomFlow.CurrentRoom : null,
                roomFlow != null ? roomFlow.MapData : null,
                roomFlow != null ? roomFlow.CurrentDepth : 0);

            if (lostItems.Count == 0)
            {
                runInventory.Clear();
                return;
            }

            Vector3 position = transform.position + (Vector3)spawnOffset;
            ExtractionLootPickup pickup = lostLootPrefab != null
                ? Instantiate(lostLootPrefab, position, Quaternion.identity)
                : CreateDefaultLostLoot(position);
            pickup.SetItems(lostItems);

            ExtractionLostLoot lostLoot = pickup.GetComponent<ExtractionLostLoot>();
            if (lostLoot == null)
            {
                lostLoot = pickup.gameObject.AddComponent<ExtractionLostLoot>();
            }

            lostLoot.SetLostItems(lostItems);
            runInventory.Clear();
        }

        public void Configure(ExtractionRoomFlowController flow)
        {
            roomFlow = flow;
        }

        private static ExtractionLootPickup CreateDefaultLostLoot(Vector3 position)
        {
            GameObject lostLoot = new GameObject("Extraction Lost Loot");
            lostLoot.transform.position = position;
            CircleCollider2D collider = lostLoot.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;
            SpriteRenderer renderer = lostLoot.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(new Color(1f, 0.55f, 0.1f, 1f));
            renderer.sortingOrder = 25;
            lostLoot.AddComponent<ExtractionLostLoot>();
            return lostLoot.AddComponent<ExtractionLootPickup>();
        }
    }
}
