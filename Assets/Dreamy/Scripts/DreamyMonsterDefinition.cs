using UnityEngine;

namespace Dreamy
{
    [CreateAssetMenu(menuName = "Dreamy/Prototype/Monster Definition", fileName = "DreamyMonsterDefinition")]
    public sealed class DreamyMonsterDefinition : ScriptableObject
    {
        [SerializeField] private string monsterId;
        [SerializeField] private string displayName;
        [SerializeField] private int level = 1;
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float chaseSpeed = 2.1f;
        [SerializeField] private float detectionRange = 7.5f;
        [SerializeField] private float attackRange = 0.92f;
        [SerializeField] private float knockbackResistance = 2.5f;
        [SerializeField] private float pixelsPerUnit = 192f;
        [SerializeField] private float visualScale = 1.45f;
        [SerializeField] private Texture2D idleSheet;
        [SerializeField] private Texture2D runSheet;
        [SerializeField] private Texture2D walkSheet;
        [SerializeField] private Texture2D attackSheet;
        [SerializeField] private Texture2D hitSheet;
        [SerializeField] private Texture2D deathSheet;
        [SerializeField] private int idleFrameCount = 1;
        [SerializeField] private int runFrameCount = 1;
        [SerializeField] private int walkFrameCount = 1;
        [SerializeField] private int attackFrameCount = 1;
        [SerializeField] private int hitFrameCount = 1;
        [SerializeField] private int deathFrameCount = 1;
        [SerializeField] private float idleFramesPerSecond = 6f;
        [SerializeField] private float moveFramesPerSecond = 8f;
        [SerializeField] private float attackFramesPerSecond = 10f;

        public string MonsterId => string.IsNullOrWhiteSpace(monsterId) ? name : monsterId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public int Level => Mathf.Max(1, level);
        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float Damage => Mathf.Max(0f, damage);
        public float ChaseSpeed => Mathf.Max(0f, chaseSpeed);
        public float DetectionRange => Mathf.Max(0.1f, detectionRange);
        public float AttackRange => Mathf.Max(0.1f, attackRange);
        public float KnockbackResistance => Mathf.Max(0f, knockbackResistance);
        public float PixelsPerUnit => Mathf.Max(1f, pixelsPerUnit);
        public float VisualScale => visualScale > 0f ? Mathf.Clamp(visualScale, 0.35f, 3.5f) : 1.45f;
        public Texture2D IdleSheet => idleSheet;
        public Texture2D RunSheet => runSheet;
        public Texture2D WalkSheet => walkSheet;
        public Texture2D AttackSheet => attackSheet;
        public Texture2D HitSheet => hitSheet;
        public Texture2D DeathSheet => deathSheet;
        public int IdleFrameCount => Mathf.Max(1, idleFrameCount);
        public int RunFrameCount => Mathf.Max(1, runFrameCount);
        public int WalkFrameCount => Mathf.Max(1, walkFrameCount);
        public int AttackFrameCount => Mathf.Max(1, attackFrameCount);
        public int HitFrameCount => Mathf.Max(1, hitFrameCount);
        public int DeathFrameCount => Mathf.Max(1, deathFrameCount);
        public float IdleFramesPerSecond => Mathf.Max(0.1f, idleFramesPerSecond);
        public float MoveFramesPerSecond => Mathf.Max(0.1f, moveFramesPerSecond);
        public float AttackFramesPerSecond => Mathf.Max(0.1f, attackFramesPerSecond);

        public Texture2D MoveSheet => runSheet != null ? runSheet : walkSheet;
        public int MoveFrameCount => runSheet != null ? RunFrameCount : WalkFrameCount;
        public bool HasIdleAnimation => idleSheet != null;
        public bool HasMoveAnimation => MoveSheet != null;
        public bool HasAttackAnimation => attackSheet != null;
        public bool IsCombatReady => HasIdleAnimation && HasMoveAnimation && HasAttackAnimation;

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
            maxHealth = Mathf.Max(1f, maxHealth);
            damage = Mathf.Max(0f, damage);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            attackRange = Mathf.Max(0.1f, attackRange);
            knockbackResistance = Mathf.Max(0f, knockbackResistance);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            visualScale = Mathf.Clamp(visualScale <= 0f ? 1.45f : visualScale, 0.35f, 3.5f);
            idleFrameCount = Mathf.Max(1, idleFrameCount);
            runFrameCount = Mathf.Max(1, runFrameCount);
            walkFrameCount = Mathf.Max(1, walkFrameCount);
            attackFrameCount = Mathf.Max(1, attackFrameCount);
            hitFrameCount = Mathf.Max(1, hitFrameCount);
            deathFrameCount = Mathf.Max(1, deathFrameCount);
            idleFramesPerSecond = Mathf.Max(0.1f, idleFramesPerSecond);
            moveFramesPerSecond = Mathf.Max(0.1f, moveFramesPerSecond);
            attackFramesPerSecond = Mathf.Max(0.1f, attackFramesPerSecond);
        }
    }
}
