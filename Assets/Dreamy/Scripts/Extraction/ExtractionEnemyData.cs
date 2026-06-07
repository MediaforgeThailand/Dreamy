using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Enemy Data", fileName = "EnemyData")]
    public sealed class ExtractionEnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId;
        [SerializeField] private string displayName;
        [SerializeField] private float maxHealth = 40f;
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private float chaseSpeed = 2.2f;
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float attackRange = 0.75f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private ExtractionLootTableData lootTable;

        public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float MaxHealth => maxHealth;
        public float ContactDamage => contactDamage;
        public float ChaseSpeed => chaseSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public ExtractionLootTableData LootTable => lootTable;

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            contactDamage = Mathf.Max(0f, contactDamage);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            detectionRange = Mathf.Max(0f, detectionRange);
            attackRange = Mathf.Max(0.05f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
        }
    }
}
