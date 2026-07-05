using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using ZLinq;
using MatchThemAll.Scripts;

namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Collects up to 3 unassigned items matching the greatest-remaining goal,
    /// vortexes them to the suck position, shrinks+spins them, then releases to pool.
    /// Logic extracted verbatim from PowerupManager.VacuumPowerup().
    ///
    /// ponytail: the original 2.5s busy gate is preserved here so vacuum feel is identical.
    /// The animation handshake (Play → Started → collect) is owned by PowerupUI/PowerupManager
    /// in Stage 2; this effect runs the collection + timing.
    /// </summary>
    [System.Serializable]
    public class VacuumEffect : PowerupEffect
    {
        // Pre-allocated, reused per call to avoid GC allocations (mirrors PowerupManager).
        private readonly List<Item> _itemsToCollect = new(3);

        public override bool CanActivate(PowerupContext ctx) => true;

        public override void Activate(PowerupContext ctx)
        {
            var items = ctx.Items;
            ItemLevelData[] goals = ctx.Goals;

            int greatestGoalIndex = GetGreatestGoalIndex(goals);
            if (greatestGoalIndex == -1)
            {
                ctx.SetBusy(false);
                return;
            }

            ItemLevelData goal = goals[greatestGoalIndex];

            _itemsToCollect.Clear();

            if (items != null)
            {
                foreach (var item in items.AsValueEnumerable()
                             .Where(item => item && item.gameObject.activeInHierarchy)
                             .Where(item =>
                                 item.ItemNameKey == goal.itemPrefab.ItemNameKey &&
                                 !item.Spot &&
                                 !item.IsMovingToSpot))
                {
                    _itemsToCollect.Add(item);
                    if (_itemsToCollect.Count >= 3)
                        break;
                }
            }

            int vacuumItemToCollect = _itemsToCollect.Count;

            if (vacuumItemToCollect == 0)
            {
                // Delay clearing busy until the visual animation finishes (~2.5s).
                Tween.Delay(2.5f).OnComplete(() => ctx.SetBusy(false));
                return;
            }

            foreach (var itemToCollect in _itemsToCollect.AsValueEnumerable().Where(itemToCollect => itemToCollect != null))
            {
                itemToCollect.DisablePhysics();

                var collect = itemToCollect;
                // 1. Vortex move to vacuum suck position
                Tween.Position(itemToCollect.transform, ctx.VacuumSuckPosition.position, 0.5f, Ease.InCubic)
                    .OnComplete(() => ItemReachedVacuum(collect));

                // 2. Shrink down to 0
                Tween.Scale(itemToCollect.transform, Vector3.zero, 0.5f, Ease.InCubic);

                // 3. Dynamic spin for aesthetic vortex feel
                Tween.LocalEulerAngles(itemToCollect.transform, itemToCollect.transform.localEulerAngles,
                    itemToCollect.transform.localEulerAngles + new Vector3(0, 720f, 0), 0.5f, Ease.InCubic);
            }

            for (int i = _itemsToCollect.Count - 1; i >= 0; i--)
            {
                Item itemToCollect = _itemsToCollect[i];
                if (!itemToCollect) continue;
                ctx.OnItemPickup?.Invoke(itemToCollect);
            }

            // Wait for the full vacuum animation before allowing another powerup.
            Tween.Delay(2.5f).OnComplete(() => ctx.SetBusy(false));
        }

        private static void ItemReachedVacuum(Item item) =>
            ItemPoolManager.Instance.ReleaseItem(item);

        // Returns clean goal array index to avoid Nullable struct boxing overhead.
        private static int GetGreatestGoalIndex(ItemLevelData[] goals)
        {
            if (goals == null || goals.Length == 0)
                return -1;

            int max = 0;
            int goalIndex = -1;

            for (int i = 0; i < goals.Length; i++)
            {
                if (goals[i].amount > max)
                {
                    max = goals[i].amount;
                    goalIndex = i;
                }
            }

            return goalIndex;
        }
    }
}
