using MatchThemAll.Scripts;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Freezes the level timer. Logic extracted verbatim from PowerupManager.FreezePowerup().
    /// </summary>
    [System.Serializable]
    public class FreezeEffect : PowerupEffect
    {
        public override bool CanActivate(PowerupContext ctx) =>
            ctx.Timer != null && !ctx.Timer.IsFrozen;

        public override void Activate(PowerupContext ctx)
        {
            // Muzzle flash at the freeze gun's muzzle.
            if (ctx.ActivateVfx != null && ctx.FreezeMuzzle != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.FreezeMuzzle.position);

            ctx.Timer.FreezeTimer();
        }
    }
}
