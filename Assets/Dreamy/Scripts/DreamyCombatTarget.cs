using UnityEngine;

namespace Dreamy
{
    public interface IDreamyCombatTarget
    {
        bool IsTargetAlive { get; }
        Transform TargetTransform { get; }
        Collider2D TargetCollider { get; }
        string TargetDisplayName { get; }

        void ReceiveCombatHit(
            float amount,
            Vector2 hitSourcePosition,
            float knockbackForce,
            float slowMultiplier,
            float slowDuration,
            float stunDuration);
    }
}
