using UnityEngine;
using PrimeTween;
using ZLinq;
using MatchThemAll.Scripts;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Applies a random force to all active items. Owns fanMagnitude.
    /// Logic extracted verbatim from PowerupManager.FanPowerup().
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

        private float _nextAllowedTime = -999f;

        // Gate here (not in Activate) so debounced clicks are rejected BEFORE PowerupManager spends a charge.
        public override bool CanActivate(PowerupContext ctx) => Time.time >= _nextAllowedTime;

        public override void Activate(PowerupContext ctx)
        {
            _nextAllowedTime = Time.time + cooldown;
            if (ctx.Items == null) return;
            // Shockwave ring at the fan's origin.
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
