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
            foreach (var item in ctx.Items.AsValueEnumerable()
                         .Where(item => item && item.gameObject.activeInHierarchy))
            {
                item.ApplyRandomForce(fanMagnitude);
            }
        }
    }
}
