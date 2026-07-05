using System;

namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Polymorphic power-up behavior, stored via [SerializeReference] on a <see cref="PowerupDataSO"/>.
    /// One subclass per power-up type. Adding a power-up = new subclass + new SO asset (no enum, no switch).
    /// </summary>
    [Serializable]
    public abstract class PowerupEffect
    {
        public abstract bool CanActivate(PowerupContext ctx);

        public abstract void Activate(PowerupContext ctx);
    }
}
