using UnityEngine;

namespace Dreamy.Extraction
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(ExtractionHealth))]
    [RequireComponent(typeof(ExtractionStamina))]
    [RequireComponent(typeof(ExtractionRunInventory))]
    [RequireComponent(typeof(ExtractionWeaponController))]
    public sealed class ExtractionPlayerController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private float dodgeSpeed = 8.5f;
        [SerializeField] private float dodgeDuration = 0.18f;
        [SerializeField] private float dodgeCooldown = 0.55f;
        [SerializeField] private float dodgeStaminaCost = 18f;
        [SerializeField] private float interactRadius = 1.15f;
        [SerializeField] private LayerMask interactLayers;

        private IExtractionInputSource inputSource;
        private Rigidbody2D body;
        private ExtractionHealth health;
        private ExtractionStamina stamina;
        private ExtractionRunInventory runInventory;
        private ExtractionWeaponController weaponController;
        private Vector2 movement;
        private Vector2 facing = Vector2.down;
        private Vector2 dodgeDirection;
        private float dodgeEndsAt;
        private float nextDodgeTime;
        private float nextCollisionRefresh;

        public Vector2 Facing => facing;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<ExtractionHealth>();
            stamina = GetComponent<ExtractionStamina>();
            runInventory = GetComponent<ExtractionRunInventory>();
            weaponController = GetComponent<ExtractionWeaponController>();
            global::Dreamy.DreamyCharacterCollisionUtility.NormalizeTopDownBody(body);
            BindInput(inputSourceBehaviour as IExtractionInputSource);
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                movement = Vector2.zero;
                return;
            }

            if (inputSource == null && inputSourceBehaviour != null)
            {
                BindInput(inputSourceBehaviour as IExtractionInputSource);
            }

            movement = inputSource != null ? inputSource.ReadMovement() : Vector2.zero;
            if (movement.sqrMagnitude > 0.01f)
            {
                facing = movement.normalized;
            }

            if (inputSource != null && inputSource.ConsumeAttackPressed())
            {
                weaponController.TryAttack(facing);
            }

            if (inputSource != null && inputSource.ConsumeSkillPressed())
            {
                weaponController.TryUseActiveSkill(facing);
            }

            if (inputSource != null && inputSource.ConsumeDodgePressed())
            {
                TryStartDodge();
            }

            if (inputSource != null && inputSource.ConsumeInteractPressed())
            {
                TryInteract();
            }
        }

        private void FixedUpdate()
        {
            Vector2 velocity = IsDodging()
                ? dodgeDirection * dodgeSpeed
                : movement * moveSpeed;
            if (velocity.sqrMagnitude < 0.0001f)
            {
                global::Dreamy.DreamyCharacterCollisionUtility.StopBodyDrift(body);
                RefreshCharacterCollisionIgnores();
                return;
            }

            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
            RefreshCharacterCollisionIgnores();
        }

        public void BindInput(IExtractionInputSource source)
        {
            inputSource = source;
            inputSourceBehaviour = source as MonoBehaviour;
        }

        private void TryStartDodge()
        {
            if (Time.time < nextDodgeTime)
            {
                return;
            }

            if (stamina != null && !stamina.TrySpend(dodgeStaminaCost))
            {
                return;
            }

            dodgeDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : facing;
            dodgeEndsAt = Time.time + dodgeDuration;
            nextDodgeTime = Time.time + dodgeCooldown;
        }

        private bool TryInteract()
        {
            int mask = interactLayers.value == 0 ? Physics2D.AllLayers : interactLayers.value;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, mask);
            ExtractionLootPickup nearestPickup = null;
            ExtractionExtractPoint nearestExtractPoint = null;
            float nearestPickupDistance = float.MaxValue;
            float nearestExtractDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                ExtractionLootPickup pickup = hits[i].GetComponentInParent<ExtractionLootPickup>();
                if (pickup != null)
                {
                    float distance = Vector2.SqrMagnitude((Vector2)pickup.transform.position - body.position);
                    if (distance < nearestPickupDistance)
                    {
                        nearestPickup = pickup;
                        nearestPickupDistance = distance;
                    }
                }

                ExtractionExtractPoint extractPoint = hits[i].GetComponentInParent<ExtractionExtractPoint>();
                if (extractPoint != null)
                {
                    float distance = Vector2.SqrMagnitude((Vector2)extractPoint.transform.position - body.position);
                    if (distance < nearestExtractDistance)
                    {
                        nearestExtractPoint = extractPoint;
                        nearestExtractDistance = distance;
                    }
                }
            }

            if (nearestPickup != null && nearestPickup.TryPickup(runInventory))
            {
                return true;
            }

            return nearestExtractPoint != null && nearestExtractPoint.TryExtract(runInventory);
        }

        private bool IsDodging()
        {
            return Time.time < dodgeEndsAt;
        }

        private void RefreshCharacterCollisionIgnores()
        {
            if (Time.time < nextCollisionRefresh)
            {
                return;
            }

            nextCollisionRefresh = Time.time + 0.45f;
            global::Dreamy.DreamyCharacterCollisionUtility.IgnoreCollisionWithAll<ExtractionEnemyController>(this);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            dodgeSpeed = Mathf.Max(0f, dodgeSpeed);
            dodgeDuration = Mathf.Max(0f, dodgeDuration);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
            dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost);
            interactRadius = Mathf.Max(0.05f, interactRadius);
        }
    }
}
