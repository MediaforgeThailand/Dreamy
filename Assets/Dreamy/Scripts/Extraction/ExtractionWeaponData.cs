using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Weapon Data", fileName = "WeaponData")]
    public sealed class ExtractionWeaponData : ScriptableObject
    {
        [SerializeField] private ExtractionItemData item;
        [SerializeField] private Sprite icon;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float attackRange = 0.8f;
        [SerializeField] private float attackRadius = 0.55f;
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private int maxDurability = 40;
        [SerializeField] private int durabilityLossPerAttack = 1;
        [SerializeField] private ExtractionWeaponSkillData activeSkill;

        public ExtractionItemData Item => item;
        public Sprite Icon => icon;
        public float Damage => damage;
        public float AttackRange => attackRange;
        public float AttackRadius => attackRadius;
        public float AttackCooldown => attackCooldown;
        public int MaxDurability => maxDurability;
        public int DurabilityLossPerAttack => durabilityLossPerAttack;
        public ExtractionWeaponSkillData ActiveSkill => activeSkill;

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            attackRange = Mathf.Max(0.05f, attackRange);
            attackRadius = Mathf.Max(0.05f, attackRadius);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            maxDurability = Mathf.Max(1, maxDurability);
            durabilityLossPerAttack = Mathf.Max(0, durabilityLossPerAttack);
        }
    }
}
