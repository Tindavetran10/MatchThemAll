using UnityEngine;
using PrimeTween;
using ZLinq;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// The "bomb": the canister jiggles and swells for a moment, then explodes over the board
    /// (fireball + smoke + embers VFX, camera shake) and blasts every active item with a random force.
    /// Owns fanMagnitude.
    /// </summary>
    [System.Serializable]
    public class FanEffect : PowerupEffect
    {
        public float fanMagnitude = 30f;
        [Tooltip("Camera shake strength for the blast (PrimeTween strengthFactor).")]
        public float shakeStrength = 0.35f;
        [Tooltip("Camera shake duration in seconds.")]
        public float shakeDuration = 0.4f;
        [Tooltip("Minimum seconds between activations. Spam clicks are rejected for free (no charge spent).")]
        public float cooldown = 0.4f;
        [Tooltip("Seconds the canister jiggles before it blows (the explosion sound is timed to this).")]
        public float fuseTime = 0.2f;

        // NonSerialized: otherwise the Editor's domain-reload backup carries a stale Time.time into the next Play session.
        [System.NonSerialized] private float _nextAllowedTime = -999f;

        // Gate here (not in Activate) so debounced clicks are rejected BEFORE PowerupManager spends a charge.
        public override bool CanActivate(PowerupContext ctx) => Time.time >= _nextAllowedTime;

        public override void Activate(PowerupContext ctx)
        {
            _nextAllowedTime = Time.time + cooldown;
            if (ctx.Items == null) return;
            // Fuse: the canister swells and rattles, then it blows.
            if (ctx.Source)
            {
                Tween.PunchScale(ctx.Source, ctx.Source.localScale * .25f, fuseTime + .1f, frequency: 18);
                Tween.ShakeLocalPosition(ctx.Source, new Vector3(.06f, .06f, 0f), fuseTime, frequency: 30);
            }
            Tween.Delay(fuseTime, () => Blast(ctx));
        }

        private void Blast(PowerupContext ctx)
        {
            if (!GameManager.Instance || !GameManager.Instance.IsGame()) return;
            // Explosion over the board, at the blast origin.
            if (ctx.ActivateVfx != null && ctx.FanOrigin != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.FanOrigin.position);
            // Camera punch to sell the blast. Restores the camera transform when done.
            if (Camera.main != null)
                Tween.ShakeCamera(Camera.main, shakeStrength, shakeDuration);
            foreach (var item in ctx.Items.AsValueEnumerable()
                         .Where(item => item && item.gameObject.activeInHierarchy))
            {
                item.ApplyRandomForce(fanMagnitude);
            }
        }
    }
}
