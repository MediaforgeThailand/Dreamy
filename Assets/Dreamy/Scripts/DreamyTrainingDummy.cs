using System;
using UnityEngine;

namespace Dreamy
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DreamyTrainingDummy : MonoBehaviour, IDreamyCombatTarget
    {
        private const float PixelsPerUnit = 32f;
        private static readonly Color BaseColor = new Color(0.85f, 0.72f, 0.42f, 1f);
        private static readonly Color HitColor = new Color(1f, 0.42f, 0.24f, 1f);

        [SerializeField] private string displayName = "Training Dummy";
        [SerializeField] private Vector2 colliderSize = new Vector2(0.72f, 1.18f);
        [SerializeField] private float hitFlashDuration = 0.12f;

        public static event Action<DreamyTrainingDummy, DreamyTrainingDummyHitRecord> HitRecorded;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D body;
        private BoxCollider2D hitbox;
        private float hitFlashEndsAt;
        private int totalHits;
        private float totalDamage;

        public bool IsTargetAlive => true;
        public Transform TargetTransform => transform;
        public Collider2D TargetCollider => hitbox != null ? hitbox : GetComponent<Collider2D>();
        public string TargetDisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public int TotalHits => totalHits;
        public float TotalDamage => totalDamage;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            ConfigureBody();
            ConfigureHitbox();
            ConfigureSprite();
            EnsureYSort();
        }

        private void Update()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Time.time < hitFlashEndsAt ? HitColor : Color.white;
            }

            DreamyCharacterCollisionUtility.StopBodyDrift(body);
        }

        public void Configure(string dummyName)
        {
            if (!string.IsNullOrWhiteSpace(dummyName))
            {
                displayName = dummyName;
            }

            gameObject.name = displayName;
        }

        public void ResetCounters()
        {
            totalHits = 0;
            totalDamage = 0f;
            hitFlashEndsAt = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        public void ReceiveCombatHit(
            float amount,
            Vector2 hitSourcePosition,
            float knockbackForce,
            float slowMultiplier,
            float slowDuration,
            float stunDuration)
        {
            float resolvedDamage = Mathf.Max(0f, amount);
            totalHits++;
            totalDamage += resolvedDamage;
            hitFlashEndsAt = Time.time + hitFlashDuration;

            HitRecorded?.Invoke(
                this,
                new DreamyTrainingDummyHitRecord(
                    Time.time,
                    resolvedDamage,
                    totalHits,
                    totalDamage,
                    slowMultiplier,
                    slowDuration,
                    stunDuration));
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            DreamyCharacterCollisionUtility.NormalizeTopDownBody(body);
            DreamyCharacterCollisionUtility.StopBodyDrift(body);
        }

        private void ConfigureHitbox()
        {
            if (hitbox == null)
            {
                return;
            }

            hitbox.isTrigger = true;
            hitbox.size = colliderSize;
            hitbox.offset = new Vector2(0f, colliderSize.y * 0.42f);
        }

        private void ConfigureSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateDummySprite();
            }

            spriteRenderer.color = Color.white;
        }

        private void EnsureYSort()
        {
            if (spriteRenderer != null)
            {
                GetComponent<DreamyYSortSprite>()?.Configure(100, 10f);
                if (GetComponent<DreamyYSortSprite>() == null)
                {
                    gameObject.AddComponent<DreamyYSortSprite>().Configure(100, 10f);
                }
            }
        }

        private void OnValidate()
        {
            colliderSize.x = Mathf.Max(0.1f, colliderSize.x);
            colliderSize.y = Mathf.Max(0.1f, colliderSize.y);
            hitFlashDuration = Mathf.Max(0.01f, hitFlashDuration);
        }

        private static Sprite CreateDummySprite()
        {
            Texture2D texture = new Texture2D(24, 36);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color dark = new Color(0.34f, 0.24f, 0.15f, 1f);
            Color face = new Color(0.95f, 0.82f, 0.52f, 1f);
            Color target = new Color(0.86f, 0.18f, 0.16f, 1f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            FillRect(texture, 9, 0, 6, 8, dark);
            FillRect(texture, 5, 8, 14, 20, BaseColor);
            FillRect(texture, 7, 24, 10, 10, face);
            FillRect(texture, 8, 17, 8, 5, target);
            FillRect(texture, 10, 18, 4, 3, BaseColor);
            FillRect(texture, 5, 7, 14, 2, dark);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.12f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    public readonly struct DreamyTrainingDummyHitRecord
    {
        public DreamyTrainingDummyHitRecord(
            float time,
            float damage,
            int hitCount,
            float totalDamage,
            float slowMultiplier,
            float slowDuration,
            float stunDuration)
        {
            Time = time;
            Damage = damage;
            HitCount = hitCount;
            TotalDamage = totalDamage;
            SlowMultiplier = slowMultiplier;
            SlowDuration = slowDuration;
            StunDuration = stunDuration;
        }

        public float Time { get; }
        public float Damage { get; }
        public int HitCount { get; }
        public float TotalDamage { get; }
        public float SlowMultiplier { get; }
        public float SlowDuration { get; }
        public float StunDuration { get; }
    }
}
