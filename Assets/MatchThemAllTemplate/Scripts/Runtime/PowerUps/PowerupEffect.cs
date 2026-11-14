using System;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Polymorphic power-up behavior, stored via [SerializeReference] on a <see cref="PowerupDataSO"/>.
    /// One subclass per power-up type. Adding a power-up = new subclass + new SO asset (no enum, no switch).
    /// </summary>
    [Serializable]
    public abstract class PowerupEffect
    {
        /// <summary>True if activation waits for the slot's "Activate" animation to fire its Started event (vacuum suck).</summary>
        public virtual bool WaitsForSlotAnimation => false;

        public abstract bool CanActivate(PowerupContext ctx);

        public abstract void Activate(PowerupContext ctx);
    }
}
