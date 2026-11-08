using System;
using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>Custom power-up mechanic. Pick it in Match Them All ▸ Power-up Manager ▸ Mechanic.</summary>
    [Serializable]
    public class ExtraTimeEffect : PowerupEffect
    {
        [Tooltip("Example setting — shown in the Power-up Manager under Mechanic.")]
        public int amount = 10;

        // Return false to refuse the tap (no charge is used). Example: only while the level timer exists.
        public override bool CanActivate(PowerupContext ctx) => ctx.Timer != null;

        public override void Activate(PowerupContext ctx)
        {
            // What ctx gives you:
            //   ctx.Items      — every item on the board (List<Item>; pooled items may be inactive)
            //   ctx.Goals      — the level's goals (ItemLevelData[])
            //   ctx.ItemSpots  — the tray (ItemSpotManager), e.g. ctx.ItemSpots.GetRandomOccupiedSpot()
            //   ctx.Timer      — the level timer (TimerManager), e.g. ctx.Timer.AddTime(amount)
            //   ctx.Source     — the tapped slot's transform (animate it, spawn VFX at it)
            //   ctx.ActivateVfx / ctx.EndVfx — the VFX set on this power-up
            //   ctx.SetBusy(true/false) — block other power-ups while a long effect runs
            Debug.Log("[ExtraTimeEffect] activated");
        }
    }
}
