using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using ZLinq;

namespace MatchThemAll.Scripts.Power_Ups
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
        public override bool WaitsForSlotAnimation => true;

        [Tooltip("Seconds each item takes to spiral into the nozzle.")]
        public float spiralTime = 0.9f;
        [Tooltip("Turns of the spiral on the way in.")]
        public float spiralTurns = 2.5f;
        [Tooltip("Starting width of the spiral (world units); it tightens to 0 at the nozzle.")]
        public float spiralRadius = 0.8f;

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

            // Tornado funnel at the suck position (lasts the whole 2.5 s).
            if (ctx.ActivateVfx != null && ctx.VacuumSuckPosition != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.VacuumSuckPosition.position);

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
                Tween.Delay(2.5f).OnComplete(() =>
                {
                    if (ctx.EndVfx != null && ctx.VacuumSuckPosition != null && VfxPool.Instance != null)
                        VfxPool.Instance.Play(ctx.EndVfx, ctx.VacuumSuckPosition.position);
                    ctx.SetBusy(false);
                });
                return;
            }

            var camForward = Camera.main ? Camera.main.transform.forward : Vector3.down;
            foreach (var itemToCollect in _itemsToCollect.AsValueEnumerable().Where(itemToCollect => itemToCollect != null))
            {
                itemToCollect.DisablePhysics();

                var collect = itemToCollect;
                // Tornado: spiral into the suck point — a helix around the travel line (as seen on screen) whose
                // radius tightens as the item speeds up, shrinks and spins.
                Vector3 from = collect.transform.position, to = ctx.VacuumSuckPosition.position;
                Vector3 side = Vector3.Cross(to - from, camForward).normalized;
                Vector3 startScale = collect.transform.localScale;
                Quaternion startRot = collect.transform.rotation;
                float phase = Random.Range(0f, Mathf.PI * 2f);
                Tween.Custom(collect.transform, 0f, 1f, spiralTime, (t, e) =>
                {
                    float swirl = phase + e * spiralTurns * Mathf.PI * 2f;
                    t.position = Vector3.Lerp(from, to, e) + side * (Mathf.Sin(swirl) * spiralRadius * (1f - e));
                    t.localScale = startScale * (1f - e);
                    t.rotation = Quaternion.AngleAxis(e * 900f, camForward) * startRot;
                }, Ease.InQuad).OnComplete(() => ItemReachedVacuum(collect));
            }

            for (int i = _itemsToCollect.Count - 1; i >= 0; i--)
            {
                Item itemToCollect = _itemsToCollect[i];
                if (!itemToCollect) continue;
                ctx.OnItemPickup?.Invoke(itemToCollect);
            }

            // Wait for the full vacuum animation before allowing another powerup.
            Tween.Delay(2.5f).OnComplete(() =>
            {
                if (ctx.EndVfx != null && ctx.VacuumSuckPosition != null && VfxPool.Instance != null)
                    VfxPool.Instance.Play(ctx.EndVfx, ctx.VacuumSuckPosition.position);
                ctx.SetBusy(false);
            });
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
