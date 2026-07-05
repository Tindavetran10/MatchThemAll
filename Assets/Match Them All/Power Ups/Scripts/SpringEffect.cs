using UnityEngine;
using PrimeTween;
using ZLinq;
using MatchThemAll.Scripts;

namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Releases a random occupied item and throws it with physics.
    /// Owns the spring tuning (moved here from GameSettingsSO so it is per-powerup).
    /// Logic extracted verbatim from PowerupManager.SpringPowerup().
    /// </summary>
    [System.Serializable]
    public class SpringEffect : PowerupEffect
    {
        [Header("Throw Tuning")]
        public Vector2 springHorizontalForceRange = new Vector2(4f, 7f);
        public Vector2 springVerticalForceRange   = new Vector2(6f, 9f);
        public Vector2 springSpinSpeedRange       = new Vector2(5f, 12f);
        public float   springThrowZDirection      = 1f;

        public override bool CanActivate(PowerupContext ctx) =>
            ctx.ItemSpots != null && ctx.ItemSpots.GetRandomOccupiedSpot();

        public override void Activate(PowerupContext ctx)
        {
            ItemSpot spot = ctx.ItemSpots.GetRandomOccupiedSpot();
            if (!spot) return;

            ctx.SetBusy(true);

            Item itemToRelease = spot.Item;
            spot.Clear();
            itemToRelease.UnassignSpot();

            itemToRelease.transform.parent = LevelManager.Instance.ItemParent;
            itemToRelease.transform.localScale = Vector3.one;

            // Pop the item up slightly so it doesn't collide with neighbours while escaping the packed spot area.
            Vector3 startPos = itemToRelease.transform.position + Vector3.up * 1f;
            itemToRelease.transform.position = startPos;

            // Pure physics throw → perfect parabolic arc that respects collisions on the way down.
            itemToRelease.EnablePhysics();
            ctx.OnItemBackToGame?.Invoke(itemToRelease);

            Rigidbody rb = itemToRelease.GetComponent<Rigidbody>();
            if (rb)
            {
                float spreadAngle = UnityEngine.Random.Range(-45f, 45f);
                Vector3 baseDirection = new Vector3(0, 0, Mathf.Sign(springThrowZDirection));
                Vector3 throwDirection = Quaternion.Euler(0, spreadAngle, 0) * baseDirection;

                float throwForceXZ = UnityEngine.Random.Range(springHorizontalForceRange.x, springHorizontalForceRange.y);
                float throwForceY  = UnityEngine.Random.Range(springVerticalForceRange.x, springVerticalForceRange.y);

                rb.linearVelocity = new Vector3(throwDirection.x * throwForceXZ, throwForceY, throwDirection.z * throwForceXZ);

                float spinSpeed = UnityEngine.Random.Range(springSpinSpeedRange.x, springSpinSpeedRange.y);
                rb.angularVelocity = UnityEngine.Random.onUnitSphere * spinSpeed;

                Tween.Delay(0.2f, () => ctx.SetBusy(false));
            }
            else ctx.SetBusy(false);
        }

#if UNITY_EDITOR
        // Trajectory preview gizmos (moved here from PowerupManager.OnDrawGizmosSelected).
        public void DrawGizmos(Vector3 startPos)
        {
            Gizmos.color = Color.green;
            DrawTrajectoryGizmo(startPos, springHorizontalForceRange.x, springVerticalForceRange.x, 0f);

            Gizmos.color = Color.red;
            DrawTrajectoryGizmo(startPos, springHorizontalForceRange.y, springVerticalForceRange.y, 0f);

            Gizmos.color = Color.yellow;
            float midXZ = (springHorizontalForceRange.x + springHorizontalForceRange.y) / 2f;
            float midY  = (springVerticalForceRange.x + springVerticalForceRange.y) / 2f;
            DrawTrajectoryGizmo(startPos, midXZ, midY, 45f);
            DrawTrajectoryGizmo(startPos, midXZ, midY, -45f);
        }

        private void DrawTrajectoryGizmo(Vector3 startPos, float forceXZ, float forceY, float spreadAngle)
        {
            Vector3 baseDirection = new Vector3(0, 0, Mathf.Sign(springThrowZDirection));
            Vector3 throwDirection = Quaternion.Euler(0, spreadAngle, 0) * baseDirection;

            Vector3 velocity = new Vector3(throwDirection.x * forceXZ, forceY, throwDirection.z * forceXZ);
            Vector3 gravity = Physics.gravity;

            Vector3 previousPos = startPos;
            float timeStep = 0.05f;
            for (float t = 0; t < 2f; t += timeStep)
            {
                Vector3 currentPos = startPos + velocity * t + 0.5f * gravity * (t * t);
                Gizmos.DrawLine(previousPos, currentPos);
                previousPos = currentPos;
                if (currentPos.y < 0.5f && t > 0.1f) break;
            }
        }
#endif
    }
}
