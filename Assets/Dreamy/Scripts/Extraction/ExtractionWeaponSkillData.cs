using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Weapon Skill", fileName = "WeaponSkill")]
    public sealed class ExtractionWeaponSkillData : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private int durabilityCost = 3;
        [SerializeField] private float staminaCost = 24f;

        public string SkillId => string.IsNullOrWhiteSpace(skillId) ? name : skillId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float Cooldown => cooldown;
        public float Radius => radius;
        public float DamageMultiplier => damageMultiplier;
        public int DurabilityCost => durabilityCost;
        public float StaminaCost => staminaCost;

        private void OnValidate()
        {
            cooldown = Mathf.Max(0f, cooldown);
            radius = Mathf.Max(0.05f, radius);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            durabilityCost = Mathf.Max(0, durabilityCost);
            staminaCost = Mathf.Max(0f, staminaCost);
        }
    }
}
