using System;
using UnityEngine;

namespace Dreamy
{
    [CreateAssetMenu(fileName = DefaultResourceName, menuName = "Dreamy/Combat Tuning Profile")]
    public sealed class DreamyCombatTuningProfile : ScriptableObject
    {
        public const string DefaultResourceName = "DreamyCombatTuningProfile";
        public const int NormalAttackCount = 3;

        [Header("Global Combat")]
        [SerializeField] private float baseDamage = 18f;
        [SerializeField] private float normalAttackMinimumDuration = 0.38f;
        [SerializeField] private float specialAttackMinimumDuration = 0.85f;
        [SerializeField] private float normalStaminaCost = 8f;
        [SerializeField] private float specialStaminaCost = 30f;
        [SerializeField] private float comboResetDelay = 0.4f;
        [SerializeField] private float comboInputBufferWindow = 0.2f;
        [SerializeField] private float specialCooldown = 4.5f;

        [Header("Normal Combo")]
        [SerializeField] private DreamyCombatActionTuning[] normalAttacks =
        {
            DreamyCombatActionTuning.CreateDefault("A1", "Attack 1", 0, 0, 9, 0, 1f, 1f, 1.15f, 0.92f, 0.28f, 0.45f, 5.2f, 1f, 0f, 0f),
            DreamyCombatActionTuning.CreateDefault("A2", "Attack 2", 1, 9, 6, 1, 1f, 1.15f, 1.24f, 0.99f, 0.28f, 0.22f, 5.2f, 1f, 0f, 0f),
            DreamyCombatActionTuning.CreateDefault("A3", "Attack 3", 2, 15, 8, 2, 0.5f, 2.25f, 1.43f, 1.12f, 0.3f, 0.18f, 5.2f, 0.5f, 1.65f, 0f)
        };

        [SerializeField] private DreamyCombatComboStep[] comboSequence =
        {
            new DreamyCombatComboStep("1", 0),
            new DreamyCombatComboStep("1-1", 0),
            new DreamyCombatComboStep("1-2", 1),
            new DreamyCombatComboStep("1-1-1", 0),
            new DreamyCombatComboStep("1-2-2", 1),
            new DreamyCombatComboStep("1-2-3", 2)
        };

        [Header("Special")]
        [SerializeField] private DreamyCombatActionTuning specialAttack =
            DreamyCombatActionTuning.CreateDefault("SKL", "Super Smash", 3, 0, 15, 3, 1f, 2.4f, 1.75f, 1.35f, 0.34f, 0.52f, 8.5f, 1f, 0f, 0f);

        public float BaseDamage => baseDamage;
        public float NormalAttackMinimumDuration => normalAttackMinimumDuration;
        public float SpecialAttackMinimumDuration => specialAttackMinimumDuration;
        public float NormalStaminaCost => normalStaminaCost;
        public float SpecialStaminaCost => specialStaminaCost;
        public float ComboResetDelay => comboResetDelay;
        public float ComboInputBufferWindow => comboInputBufferWindow;
        public float SpecialCooldown => specialCooldown;
        public DreamyCombatActionTuning SpecialAttack => specialAttack;
        public int ComboStepCount => comboSequence != null ? comboSequence.Length : 0;

        public static DreamyCombatTuningProfile LoadDefault()
        {
            return Resources.Load<DreamyCombatTuningProfile>(DefaultResourceName);
        }

        public DreamyCombatActionTuning GetNormalAttack(int attackPartIndex)
        {
            EnsureDefaults();
            int index = Mathf.Clamp(attackPartIndex, 0, normalAttacks.Length - 1);
            return normalAttacks[index];
        }

        public DreamyCombatActionTuning GetComboAction(int comboStepIndex)
        {
            EnsureDefaults();
            if (comboSequence.Length == 0)
            {
                return GetNormalAttack(0);
            }

            int stepIndex = Mathf.Clamp(comboStepIndex, 0, comboSequence.Length - 1);
            return GetNormalAttack(comboSequence[stepIndex].ActionIndex);
        }

        public int GetComboActionIndex(int comboStepIndex)
        {
            EnsureDefaults();
            if (comboSequence.Length == 0)
            {
                return 0;
            }

            int stepIndex = Mathf.Clamp(comboStepIndex, 0, comboSequence.Length - 1);
            return Mathf.Clamp(comboSequence[stepIndex].ActionIndex, 0, NormalAttackCount - 1);
        }

        public void ResetToDefaults()
        {
            baseDamage = 18f;
            normalAttackMinimumDuration = 0.38f;
            specialAttackMinimumDuration = 0.85f;
            normalStaminaCost = 8f;
            specialStaminaCost = 30f;
            comboResetDelay = 0.4f;
            comboInputBufferWindow = 0.2f;
            specialCooldown = 4.5f;
            normalAttacks = new[]
            {
                DreamyCombatActionTuning.CreateDefault("A1", "Attack 1", 0, 0, 9, 0, 1f, 1f, 1.15f, 0.92f, 0.28f, 0.45f, 5.2f, 1f, 0f, 0f),
                DreamyCombatActionTuning.CreateDefault("A2", "Attack 2", 1, 9, 6, 1, 1f, 1.15f, 1.24f, 0.99f, 0.28f, 0.22f, 5.2f, 1f, 0f, 0f),
                DreamyCombatActionTuning.CreateDefault("A3", "Attack 3", 2, 15, 8, 2, 0.5f, 2.25f, 1.43f, 1.12f, 0.3f, 0.18f, 5.2f, 0.5f, 1.65f, 0f)
            };
            comboSequence = new[]
            {
                new DreamyCombatComboStep("1", 0),
                new DreamyCombatComboStep("1-1", 0),
                new DreamyCombatComboStep("1-2", 1),
                new DreamyCombatComboStep("1-1-1", 0),
                new DreamyCombatComboStep("1-2-2", 1),
                new DreamyCombatComboStep("1-2-3", 2)
            };
            specialAttack = DreamyCombatActionTuning.CreateDefault("SKL", "Super Smash", 3, 0, 15, 3, 1f, 2.4f, 1.75f, 1.35f, 0.34f, 0.52f, 8.5f, 1f, 0f, 0f);
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            baseDamage = Mathf.Max(0f, baseDamage);
            normalAttackMinimumDuration = Mathf.Max(0.05f, normalAttackMinimumDuration);
            specialAttackMinimumDuration = Mathf.Max(0.05f, specialAttackMinimumDuration);
            normalStaminaCost = Mathf.Max(0f, normalStaminaCost);
            specialStaminaCost = Mathf.Max(0f, specialStaminaCost);
            comboResetDelay = Mathf.Max(0.05f, comboResetDelay);
            comboInputBufferWindow = Mathf.Max(0f, comboInputBufferWindow);
            specialCooldown = Mathf.Max(0f, specialCooldown);

            if (normalAttacks == null || normalAttacks.Length < NormalAttackCount)
            {
                DreamyCombatActionTuning[] defaults =
                {
                    DreamyCombatActionTuning.CreateDefault("A1", "Attack 1", 0, 0, 9, 0, 1f, 1f, 1.15f, 0.92f, 0.28f, 0.45f, 5.2f, 1f, 0f, 0f),
                    DreamyCombatActionTuning.CreateDefault("A2", "Attack 2", 1, 9, 6, 1, 1f, 1.15f, 1.24f, 0.99f, 0.28f, 0.22f, 5.2f, 1f, 0f, 0f),
                    DreamyCombatActionTuning.CreateDefault("A3", "Attack 3", 2, 15, 8, 2, 0.5f, 2.25f, 1.43f, 1.12f, 0.3f, 0.18f, 5.2f, 0.5f, 1.65f, 0f)
                };
                DreamyCombatActionTuning[] rebuilt = new DreamyCombatActionTuning[NormalAttackCount];
                for (int i = 0; i < rebuilt.Length; i++)
                {
                    rebuilt[i] = normalAttacks != null && i < normalAttacks.Length && normalAttacks[i] != null ? normalAttacks[i] : defaults[i];
                }

                normalAttacks = rebuilt;
            }

            for (int i = 0; i < normalAttacks.Length; i++)
            {
                normalAttacks[i] ??= DreamyCombatActionTuning.CreateDefault("A" + (i + 1), "Attack " + (i + 1), i, i == 0 ? 0 : i == 1 ? 9 : 15, i == 0 ? 9 : i == 1 ? 6 : 8, i, 1f, 1f, 1.15f, 0.92f, 0.28f, 0.35f, 5.2f, 1f, 0f, 0f);
                normalAttacks[i].Validate(i);
            }

            if (comboSequence == null || comboSequence.Length == 0)
            {
                comboSequence = new[]
                {
                    new DreamyCombatComboStep("1", 0),
                    new DreamyCombatComboStep("1-1", 0),
                    new DreamyCombatComboStep("1-2", 1),
                    new DreamyCombatComboStep("1-1-1", 0),
                    new DreamyCombatComboStep("1-2-2", 1),
                    new DreamyCombatComboStep("1-2-3", 2)
                };
            }

            for (int i = 0; i < comboSequence.Length; i++)
            {
                comboSequence[i].Validate(NormalAttackCount);
            }

            specialAttack ??= DreamyCombatActionTuning.CreateDefault("SKL", "Super Smash", 3, 0, 15, 3, 1f, 2.4f, 1.75f, 1.35f, 0.34f, 0.52f, 8.5f, 1f, 0f, 0f);
            specialAttack.Validate(3);
        }

        private void OnValidate()
        {
            EnsureDefaults();
        }
    }

    [Serializable]
    public sealed class DreamyCombatActionTuning
    {
        [SerializeField] private string id = "A1";
        [SerializeField] private string displayName = "Attack 1";
        [SerializeField] private int sourceFrameTotal = 23;
        [SerializeField] private int sourceFrameStart;
        [SerializeField] private int frameCount = 9;
        [SerializeField] private int animatorStateIndex;
        [SerializeField] private int attackPartIndex;
        [SerializeField] private float frameSpeedMultiplier = 1f;
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float hitboxLength = 1.15f;
        [SerializeField] private float hitboxWidth = 0.92f;
        [SerializeField] private float originDistance = 0.28f;
        [SerializeField] private float hitMarkerNormalizedTime = 0.45f;
        [SerializeField] private float knockbackForce = 5.2f;
        [SerializeField] private float slowMultiplier = 1f;
        [SerializeField] private float slowDuration;
        [SerializeField] private float stunDuration;
        [SerializeField] private DreamyCombatEventTuning[] events = Array.Empty<DreamyCombatEventTuning>();

        public string Id => id;
        public string DisplayName => displayName;
        public int SourceFrameTotal => sourceFrameTotal;
        public int SourceFrameStart => sourceFrameStart;
        public int FrameCount => frameCount;
        public int AnimatorStateIndex => animatorStateIndex;
        public int AttackPartIndex => attackPartIndex;
        public float FrameSpeedMultiplier => frameSpeedMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float HitboxLength => hitboxLength;
        public float HitboxWidth => hitboxWidth;
        public float OriginDistance => originDistance;
        public float HitMarkerNormalizedTime => hitMarkerNormalizedTime;
        public float KnockbackForce => knockbackForce;
        public float SlowMultiplier => slowMultiplier;
        public float SlowDuration => slowDuration;
        public float StunDuration => stunDuration;
        public DreamyCombatEventTuning[] Events => events ?? Array.Empty<DreamyCombatEventTuning>();

        public static DreamyCombatActionTuning CreateDefault(
            string id,
            string displayName,
            int attackPartIndex,
            int sourceFrameStart,
            int frameCount,
            int animatorStateIndex,
            float frameSpeedMultiplier,
            float damageMultiplier,
            float hitboxLength,
            float hitboxWidth,
            float originDistance,
            float hitMarkerNormalizedTime,
            float knockbackForce,
            float slowMultiplier,
            float slowDuration,
            float stunDuration)
        {
            DreamyCombatActionTuning tuning = new DreamyCombatActionTuning
            {
                id = id,
                displayName = displayName,
                sourceFrameTotal = attackPartIndex == 3 ? 15 : 23,
                sourceFrameStart = sourceFrameStart,
                frameCount = frameCount,
                animatorStateIndex = animatorStateIndex,
                attackPartIndex = attackPartIndex,
                frameSpeedMultiplier = frameSpeedMultiplier,
                damageMultiplier = damageMultiplier,
                hitboxLength = hitboxLength,
                hitboxWidth = hitboxWidth,
                originDistance = originDistance,
                hitMarkerNormalizedTime = hitMarkerNormalizedTime,
                knockbackForce = knockbackForce,
                slowMultiplier = slowMultiplier,
                slowDuration = slowDuration,
                stunDuration = stunDuration,
                events = Array.Empty<DreamyCombatEventTuning>()
            };
            tuning.Validate(attackPartIndex);
            return tuning;
        }

        public void Validate(int fallbackAttackPartIndex)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = "A" + (fallbackAttackPartIndex + 1);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = id;
            }

            sourceFrameTotal = Mathf.Max(1, sourceFrameTotal);
            sourceFrameStart = Mathf.Clamp(sourceFrameStart, 0, sourceFrameTotal - 1);
            frameCount = Mathf.Clamp(frameCount, 1, sourceFrameTotal - sourceFrameStart);
            animatorStateIndex = Mathf.Clamp(animatorStateIndex, 0, 3);
            attackPartIndex = Mathf.Clamp(attackPartIndex, 0, 3);
            frameSpeedMultiplier = Mathf.Clamp(frameSpeedMultiplier, 0.05f, 4f);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
            hitboxLength = Mathf.Max(0.05f, hitboxLength);
            hitboxWidth = Mathf.Max(0.05f, hitboxWidth);
            originDistance = Mathf.Max(0f, originDistance);
            hitMarkerNormalizedTime = Mathf.Clamp01(hitMarkerNormalizedTime);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            slowMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
            slowDuration = Mathf.Max(0f, slowDuration);
            stunDuration = Mathf.Max(0f, stunDuration);
            events ??= Array.Empty<DreamyCombatEventTuning>();
            for (int i = 0; i < events.Length; i++)
            {
                events[i] ??= new DreamyCombatEventTuning();
                events[i].Validate();
            }
        }
    }

    [Serializable]
    public struct DreamyCombatComboStep
    {
        [SerializeField] private string label;
        [SerializeField] private int actionIndex;

        public DreamyCombatComboStep(string label, int actionIndex)
        {
            this.label = label;
            this.actionIndex = actionIndex;
        }

        public string Label => label;
        public int ActionIndex => actionIndex;

        public void Validate(int actionCount)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Step";
            }

            actionIndex = Mathf.Clamp(actionIndex, 0, Mathf.Max(0, actionCount - 1));
        }
    }

    public enum DreamyCombatVfxKind
    {
        None,
        BuiltInSlash,
        Prefab
    }

    [Serializable]
    public sealed class DreamyCombatEventTuning
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private string label = "VFX";
        [SerializeField] private float normalizedTime = 0.5f;
        [SerializeField] private DreamyCombatVfxKind vfxKind = DreamyCombatVfxKind.None;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2 offset = new Vector2(0.52f, 0f);
        [SerializeField] private Color color = new Color(1f, 0.86f, 0.36f, 0.9f);
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float startScale = 0.72f;
        [SerializeField] private float endScale = 1.22f;
        [SerializeField] private bool rotateToDirection = true;

        public bool Enabled => enabled;
        public string Label => label;
        public float NormalizedTime => normalizedTime;
        public DreamyCombatVfxKind VfxKind => vfxKind;
        public GameObject Prefab => prefab;
        public Vector2 Offset => offset;
        public Color Color => color;
        public float Duration => duration;
        public float StartScale => startScale;
        public float EndScale => endScale;
        public bool RotateToDirection => rotateToDirection;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                label = "VFX";
            }

            normalizedTime = Mathf.Clamp01(normalizedTime);
            duration = Mathf.Max(0.03f, duration);
            startScale = Mathf.Max(0.01f, startScale);
            endScale = Mathf.Max(0.01f, endScale);
        }
    }
}
