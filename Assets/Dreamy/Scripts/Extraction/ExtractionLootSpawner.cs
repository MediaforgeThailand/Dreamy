using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionLootSpawner : MonoBehaviour
    {
        [SerializeField] private ExtractionLootTableData lootTable;
        [SerializeField] private ExtractionLootPickup pickupPrefab;
        [SerializeField] private float scatterRadius = 0.35f;

        public void SpawnConfiguredLoot(Vector3 position)
        {
            SpawnLoot(lootTable, position);
        }

        public void SpawnLoot(ExtractionLootTableData table, Vector3 position)
        {
            if (table == null)
            {
                return;
            }

            List<ExtractionItemStack> rolledLoot = table.Roll();
            if (rolledLoot.Count == 0)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            ExtractionLootPickup pickup = CreatePickup(position + (Vector3)offset);
            pickup.SetItems(rolledLoot);
        }

        public ExtractionLootPickup SpawnItems(IEnumerable<ExtractionItemStack> items, Vector3 position)
        {
            ExtractionLootPickup pickup = CreatePickup(position);
            pickup.SetItems(items);
            return pickup;
        }

        private ExtractionLootPickup CreatePickup(Vector3 position)
        {
            ExtractionLootPickup pickup = pickupPrefab != null
                ? Instantiate(pickupPrefab, position, Quaternion.identity)
                : CreateDefaultPickup(position);
            return pickup;
        }

        private static ExtractionLootPickup CreateDefaultPickup(Vector3 position)
        {
            GameObject loot = new GameObject("Extraction Loot Drop");
            loot.transform.position = position;
            CircleCollider2D collider = loot.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
            collider.isTrigger = true;
            SpriteRenderer renderer = loot.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(Color.yellow);
            renderer.sortingOrder = 20;
            return loot.AddComponent<ExtractionLootPickup>();
        }

        private void OnValidate()
        {
            scatterRadius = Mathf.Max(0f, scatterRadius);
        }
    }
}
