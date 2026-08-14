using UnityEngine;
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

        public override bool CanActivate(PowerupContext ctx) => true;

        public override void Activate(PowerupContext ctx)
        {
            if (ctx.Items == null) return;
            // Shockwave ring at the fan's origin.
            if (ctx.ActivateVfx != null && ctx.FanOrigin != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.FanOrigin.position);
            foreach (var item in ctx.Items.AsValueEnumerable()
                         .Where(item => item && item.gameObject.activeInHierarchy))
            {
                item.ApplyRandomForce(fanMagnitude);
            }
        }
    }
}
