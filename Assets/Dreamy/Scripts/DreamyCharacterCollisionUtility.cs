using UnityEngine;

namespace Dreamy
{
    public static class DreamyCharacterCollisionUtility
    {
        public static void NormalizeTopDownBody(Rigidbody2D body)
        {
            if (body == null)
            {
                return;
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = Mathf.Max(body.linearDamping, 18f);
            body.angularDamping = Mathf.Max(body.angularDamping, 18f);
        }

        public static void StopBodyDrift(Rigidbody2D body)
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public static void IgnoreCollisionBetween(Component first, Component second, bool ignore = true)
        {
            if (first == null || second == null || first == second)
            {
                return;
            }

            Collider2D[] firstColliders = first.GetComponentsInChildren<Collider2D>(true);
            Collider2D[] secondColliders = second.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < firstColliders.Length; i++)
            {
                Collider2D firstCollider = firstColliders[i];
                if (firstCollider == null)
                {
                    continue;
                }

                for (int j = 0; j < secondColliders.Length; j++)
                {
                    Collider2D secondCollider = secondColliders[j];
                    if (secondCollider == null || firstCollider == secondCollider)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(firstCollider, secondCollider, ignore);
                }
            }
        }

        public static void IgnoreCollisionWithAll<T>(Component source, bool ignore = true) where T : Component
        {
            if (source == null)
            {
                return;
            }

            T[] targets = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            for (int i = 0; i < targets.Length; i++)
            {
                T target = targets[i];
                if (target == null || target == source)
                {
                    continue;
                }

                IgnoreCollisionBetween(source, target, ignore);
            }
        }
    }
}
