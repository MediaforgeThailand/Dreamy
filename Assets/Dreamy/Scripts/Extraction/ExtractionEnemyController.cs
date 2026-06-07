using UnityEngine;

namespace Dreamy.Extraction
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(ExtractionHealth))]
    public sealed class ExtractionEnemyController : MonoBehaviour
    {
        [SerializeField] private ExtractionEnemyData enemyData;
        [SerializeField] private Transform target;
        [SerializeField] private ExtractionLootSpawner lootSpawner;

        private Rigidbody2D body;
        private ExtractionHealth health;
        private ExtractionRoomFlowController roomFlow;
        private float nextAttackTime;
        private float nextCollisionRefresh;

        private float MaxHealth => enemyData != null ? enemyData.MaxHealth : 30f;
        private float ChaseSpeed => enemyData != null ? enemyData.ChaseSpeed : 2f;
        private float DetectionRange => enemyData != null ? enemyData.DetectionRange : 5f;
        private float AttackRange => enemyData != null ? enemyData.AttackRange : 0.75f;
        private float ContactDamage => enemyData != null ? enemyData.ContactDamage : 8f;
        private float AttackCooldown => enemyData != null ? enemyData.AttackCooldown : 1.2f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<ExtractionHealth>();
            lootSpawner = lootSpawner != null ? lootSpawner : GetComponent<ExtractionLootSpawner>();
            roomFlow = roomFlow != null ? roomFlow : UnityEngine.Object.FindAnyObjectByType<ExtractionRoomFlowController>();
            global::Dreamy.DreamyCharacterCollisionUtility.NormalizeTopDownBody(body);
            health.SetMaxHealth(MaxHealth, true);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }

            roomFlow?.RegisterEnemy(this);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                ExtractionPlayerController player = UnityEngine.Object.FindAnyObjectByType<ExtractionPlayerController>();
                target = player != null ? player.transform : null;
            }

            RefreshCharacterCollisionIgnores();

            if (target == null || !health.IsAlive)
            {
                global::Dreamy.DreamyCharacterCollisionUtility.StopBodyDrift(body);
                return;
            }

            Vector2 delta = target.position - transform.position;
            float distance = delta.magnitude;
            if (distance > DetectionRange)
            {
                global::Dreamy.DreamyCharacterCollisionUtility.StopBodyDrift(body);
                return;
            }

            if (distance > AttackRange)
            {
                Vector2 step = delta.normalized * (ChaseSpeed * Time.fixedDeltaTime);
                body.MovePosition(body.position + step);
            }
            else
            {
                global::Dreamy.DreamyCharacterCollisionUtility.StopBodyDrift(body);
                TryAttackTarget();
            }
        }

        private void TryAttackTarget()
        {
            if (Time.time < nextAttackTime || target == null)
            {
                return;
            }

            ExtractionHealth targetHealth = target.GetComponentInParent<ExtractionHealth>();
            if (targetHealth != null)
            {
                targetHealth.ApplyDamage(new ExtractionDamage(ContactDamage, gameObject, transform.position));
            }

            nextAttackTime = Time.time + AttackCooldown;
        }

        private void HandleDeath()
        {
            if (lootSpawner != null)
            {
                lootSpawner.SpawnLoot(enemyData != null ? enemyData.LootTable : null, transform.position);
            }

            roomFlow?.NotifyEnemyDefeated(this);
            Destroy(gameObject);
        }

        public void Configure(ExtractionEnemyData data, Transform chaseTarget, ExtractionRoomFlowController flow)
        {
            enemyData = data;
            target = chaseTarget;
            roomFlow = flow;
            RefreshCharacterCollisionIgnores(true);
            if (health != null)
            {
                health.SetMaxHealth(MaxHealth, true);
            }

            if (isActiveAndEnabled)
            {
                roomFlow?.RegisterEnemy(this);
            }
        }

        private void RefreshCharacterCollisionIgnores(bool force = false)
        {
            if (!force && Time.time < nextCollisionRefresh)
            {
                return;
            }

            nextCollisionRefresh = Time.time + 0.45f;
            if (target != null)
            {
                global::Dreamy.DreamyCharacterCollisionUtility.IgnoreCollisionBetween(this, target);
            }

            global::Dreamy.DreamyCharacterCollisionUtility.IgnoreCollisionWithAll<ExtractionEnemyController>(this);
        }
    }
}
