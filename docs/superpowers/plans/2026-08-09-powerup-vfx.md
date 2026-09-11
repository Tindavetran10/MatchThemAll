# Powerup VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add distinct, pooled, sci-fi VFX to all 4 powerups (vacuum, spring, fan, freeze), customizable per-powerup via Inspector prefab slots.

**Architecture:** Hybrid — VFX prefab slots live on `PowerupDataSO` (`activateVfx` + optional `endVfx`); each `PowerupEffect` subclass owns *when/where* to spawn via a single shared `VfxPool` MonoBehaviour. `VfxPool` generalizes the existing `MergeManager` pooled-`ParticleSystem` pattern to "any prefab, any position," keyed by source prefab.

**Tech Stack:** Unity (URP), C#, `UnityEngine.Pool.ObjectPool<T>`, PrimeTween (already imported), NaughtyAttributes (already imported), ParticleSystem.

**Spec:** [docs/superpowers/specs/2026-08-09-powerup-vfx-design.md](../specs/2026-08-09-powerup-vfx-design.md)

## Global Constraints

- **Runtime namespace:** `MatchThemAll.Scripts` (note the `Scripts` segment — match `MergeManager`, `PowerupManager`).
- **ZLinq** (`AsValueEnumerable()`) in any per-item loop inside managers/effects — maintain the existing no-allocation pattern.
- **No direct singleton reach-inside effects:** effects reach game state only through `PowerupContext`. The one exception is `VfxPool.Instance` (the spawner), which is the VFX equivalent of `ItemPoolManager.Instance` (already used by `VacuumEffect.ItemReachedVacuum`).
- **Additive only:** never rename existing serialized fields/properties. New fields are appended; existing Inspector references must stay intact.
- **Null-safe spawning:** a null prefab slot = no-op (no errors). Optional `endVfx` costs nothing when unset.
- **Layer routing:** spawned particles use `Tutorial` layer when `InputManager.IsTutorialActive`, else `Default` — mirror `MergeManager.FinalizeMerge`.
- **Ponytail (project mode):** minimum code. No generic effect sequencer, no config-for-values-that-never-change. One pooled spawner, four 1-2 line effect edits.
- **No new Editor scripts, no new events.** Spawn calls live inside the existing `PowerupEffect.Activate` methods.
- **Prefab authoring** is done in the Unity Particle System editor; these steps are manual (not unit-testable). Verification is Play-mode visual + Profiler allocation check.

## File Structure

**Created:**
- `Scripts/Runtime/VFX/VfxPool.cs` — pooled ParticleSystem spawner, singleton, prefab-keyed.
- `Prefabs/VFX/Powerups/Portal.prefab` — vacuum activate (portal-open swirl).
- `Prefabs/VFX/Powerups/PortalCollapse.prefab` — vacuum end (portal-collapse burst).
- `Prefabs/VFX/Powerups/TeleportCharge.prefab` — spring activate.
- `Prefabs/VFX/Powerups/ShockwaveRing.prefab` — fan activate.
- `Prefabs/VFX/Powerups/MuzzleFlash.prefab` — freeze activate.

**Modified:**
- `Scripts/Runtime/PowerUps/PowerupDataSO.cs` — add `activateVfx`/`endVfx` fields + accessors.
- `Scripts/Runtime/PowerUps/PowerupContext.cs` — add `FanOrigin`, `FreezeMuzzle` Transform fields.
- `Scripts/Runtime/PowerUps/PowerupManager.cs` — add `fanOrigin`/`freezeMuzzle` serialized fields; wire into `BuildContext`.
- `Scripts/Runtime/PowerUps/VacuumEffect.cs` — play activate + end (both busy-delay branches).
- `Scripts/Runtime/PowerUps/SpringEffect.cs` — play activate at `startPos`.
- `Scripts/Runtime/PowerUps/FanEffect.cs` — play activate at `ctx.FanOrigin`.
- `Scripts/Runtime/PowerUps/FreezeEffect.cs` — play activate at `ctx.FreezeMuzzle`.
- `Scenes/MainScene.unity` — add `VfxPool` GameObject; wire `fanOrigin`/`freezeMuzzle` Transforms on `PowerupManager`.
- 4× `Resources/Powerups/Powerup_*.asset` — assign the new `activateVfx`/`endVfx` prefab references.

---

### Task 1: `VfxPool` — the pooled spawner

**Files:**
- Create: `Scripts/Runtime/VFX/VfxPool.cs`

**Interfaces:**
- Produces: `public static VfxPool.Instance` (singleton getter); `public void Play(ParticleSystem prefab, Vector3 pos)` — null-safe, plays once at world position, auto-returns to pool after particle lifetime. Consumed by Tasks 4–7.

**Why this is Task 1:** every effect edit depends on `VfxPool.Instance.Play(...)` existing and compiling.

- [ ] **Step 1: Create the folder + file**

In the Unity Project window, create folder `Assets/Match Them All/Scripts/Runtime/VFX/`. Then create `VfxPool.cs` there with this content:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// General-purpose pooled ParticleSystem spawner. One pool per source prefab,
    /// keyed by the prefab so different prefabs never share instances.
    /// Generalizes the MergeManager particle-pool pattern to "any prefab, any position".
    ///
    /// ponytail: no pre-warm (powerups fire less often than merges; first-call
    /// Instantiate is acceptable). Add pre-warm if a first-use spike shows in the Profiler.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        public static VfxPool Instance { get; private set; }

        private readonly Dictionary<ParticleSystem, IObjectPool<ParticleSystem>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            // Stop pending return-to-pool coroutines so none fire against destroyed objects on scene unload.
            StopAllCoroutines();
        }

        /// <summary>
        /// Spawns prefab at pos (world space), sets layer, plays, returns to pool after lifetime.
        /// Null-safe: no prefab = no-op.
        /// </summary>
        public void Play(ParticleSystem prefab, Vector3 pos)
        {
            if (prefab == null) return;

            var pool = GetOrCreatePool(prefab);
            ParticleSystem ps = pool.Get();
            ps.transform.position = pos;
            ps.gameObject.layer = LayerMask.NameToLayer(InputManager.IsTutorialActive ? "Tutorial" : "Default");

            var main = ps.main;
            ps.Play();

            float lifetime = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(pool, ps, lifetime));
        }

        private IObjectPool<ParticleSystem> GetOrCreatePool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool)) return pool;

            pool = new ObjectPool<ParticleSystem>(
                createFunc:      () => Instantiate(prefab, transform),
                actionOnGet:     ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize:         16
            );
            _pools[prefab] = pool;
            return pool;
        }

        private IEnumerator ReturnToPool(IObjectPool<ParticleSystem> pool, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            // Scene may have reloaded and destroyed this object while waiting.
            if (ps) pool.Release(ps);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles**

In Unity: let it compile (watch the spinner / check Console).
Expected: no errors. If `InputManager.IsTutorialActive` is not found, confirm it exists via `Unity.FindInFile` on `InputManager.cs` for `IsTutorialActive` — MergeManager already references it identically, so it should resolve.

- [ ] **Step 3: Place VfxPool in the scene**

In `MainScene`, create an empty GameObject named `VfxPool`, add the `VfxPool` component (or drag the script onto it). Save the scene.

- [ ] **Step 4: Verify singleton wires**

Enter Play mode. In the Inspector for the `VfxPool` object, confirm it's alive. Exit Play mode.
Expected: no `Instance` duplicate warnings in Console.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/VFX/VfxPool.cs" "Assets/Match Them All/Scenes/MainScene.unity"
git commit -m "feat: add VfxPool pooled ParticleSystem spawner"
```

---

### Task 2: VFX slots on `PowerupDataSO`

**Files:**
- Modify: `Scripts/Runtime/PowerUps/PowerupDataSO.cs`

**Interfaces:**
- Produces: `public ParticleSystem ActivateVfx { get; }` and `public ParticleSystem EndVfx { get; }` (read-only accessors on `PowerupDataSO`). Consumed by Task 3, which copies these onto `PowerupContext` so effects can read them without a back-reference to the SO.

**Design note — how an effect gets its prefab:** `PowerupEffect` subclasses are stored per-SO via `[SerializeReference]`, so each effect instance belongs to exactly one SO. But the effect does not hold a back-reference to its SO, and `PowerupContext` does not carry the SO. The cleanest fix that respects "no direct singleton reach-in" minimally: **pass the SO's VFX prefabs into `PowerupContext`**. That change is in Task 3 alongside the other context additions, so the prefabs flow: `PowerupManager.BuildContext(so)` → `ctx.ActivateVfx` / `ctx.EndVfx` → effect reads them. This keeps effects context-only and adds zero singletons.

- [ ] **Step 1: Add the fields + accessors**

In `PowerupDataSO.cs`, after the existing `[Header("Runtime")]` block (after line 30, the `effect` field), add a new header + fields:

```csharp
        [Header("VFX")]
        [Tooltip("Plays once when the powerup activates. Required for VFX; null = no effect.")]
        [SerializeField] private ParticleSystem activateVfx;
        [Tooltip("Plays once when the effect resolves (e.g. portal collapse). Optional.")]
        [SerializeField] private ParticleSystem endVfx;
```

And in the public accessors region (after `public PowerupEffect Effect => effect;`), add:

```csharp
        public ParticleSystem ActivateVfx => activateVfx;
        public ParticleSystem EndVfx => endVfx;
```

- [ ] **Step 2: Verify it compiles**

Unity compiles. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/PowerupDataSO.cs"
git commit -m "feat: add activateVfx/endVfx slots to PowerupDataSO"
```

---

### Task 3: `PowerupContext` + `PowerupManager` wiring

**Files:**
- Modify: `Scripts/Runtime/PowerUps/PowerupContext.cs`
- Modify: `Scripts/Runtime/PowerUps/PowerupManager.cs` (add serialized fields + `BuildContext` wiring)

**Interfaces:**
- Produces on `PowerupContext`: `ParticleSystem ActivateVfx`, `ParticleSystem EndVfx`, `Transform FanOrigin`, `Transform FreezeMuzzle`.
- Produces on `PowerupManager`: two new `[SerializeField] private Transform` fields `fanOrigin`, `freezeMuzzle`, wired in `BuildContext`.

- [ ] **Step 1: Extend `PowerupContext`**

In `PowerupContext.cs`, add four fields inside the struct (after `VacuumSuckPosition`):

```csharp
        public ParticleSystem ActivateVfx;     // from the activating SO
        public ParticleSystem EndVfx;          // from the activating SO (optional)
        public Transform FanOrigin;            // fan object's transform (nullable)
        public Transform FreezeMuzzle;          // freeze gun muzzle transform (nullable)
```

Add `using UnityEngine;` is already present (line 4). No new usings needed (`ParticleSystem`/`Transform` are in `UnityEngine`).

- [ ] **Step 2: Add serialized origin fields to `PowerupManager`**

In `PowerupManager.cs`, after the `[Header("Vacuum Elements")]` block (after line 26, `vacuumSuckPosition`), add:

```csharp
        [Header("Fan Elements")]
        [SerializeField] private Transform fanOrigin;          // where the fan VFX originates

        [Header("Freeze Elements")]
        [SerializeField] private Transform freezeMuzzle;       // the freeze gun muzzle
```

- [ ] **Step 3: Wire them in `BuildContext`**

In `PowerupManager.BuildContext` (around line 138–149), add four lines to the returned struct initializer, before the closing `};`:

```csharp
                ActivateVfx = so.ActivateVfx,
                EndVfx = so.EndVfx,
                FanOrigin = fanOrigin,
                FreezeMuzzle = freezeMuzzle,
```

- [ ] **Step 4: Verify it compiles**

Unity compiles. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/PowerupContext.cs" "Assets/Match Them All/Scripts/Runtime/PowerUps/PowerupManager.cs"
git commit -m "feat: thread VFX prefabs + fan/freeze origins through PowerupContext"
```

---

### Task 4: Vacuum VFX (portal open + collapse)

**Files:**
- Modify: `Scripts/Runtime/PowerUps/VacuumEffect.cs`

**Interfaces:**
- Consumes: `ctx.ActivateVfx`, `ctx.EndVfx`, `ctx.VacuumSuckPosition` (existing), `VfxPool.Instance.Play(prefab, pos)`.
- Note on vacuum timing: vacuum's `Activate` is invoked either directly (`PowerupManager.cs:112`, no animator) or via the `OnVacuumStarted` animation handshake (`PowerupManager.cs:130`). Either way `Activate` runs once — the VFX calls inside it are correct for both paths.

- [ ] **Step 1: Add the VFX calls**

In `VacuumEffect.cs`, at the top of `Activate` (right after the opening brace, before `var items = ctx.Items;`), play the portal-open effect at the suck position:

```csharp
            // Portal opens at the suck position.
            if (ctx.ActivateVfx != null && ctx.VacuumSuckPosition != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.VacuumSuckPosition.position);
```

Then add the portal-collapse end VFX in **both** busy-delay callbacks. In the zero-items branch (the `if (vacuumItemToCollect == 0)` block, around line 59–64), change:

```csharp
            if (vacuumItemToCollect == 0)
            {
                // Delay clearing busy until the visual animation finishes (~2.5s).
                Tween.Delay(2.5f).OnComplete(() => ctx.SetBusy(false));
                return;
            }
```
to:
```csharp
            if (vacuumItemToCollect == 0)
            {
                // Delay clearing busy until the visual animation finishes (~2.5s).
                Tween.Delay(2.5f).OnComplete(() =>
                {
                    if (ctx.EndVfx != null && ctx.VacuumSuckPosition != null)
                        VfxPool.Instance.Play(ctx.EndVfx, ctx.VacuumSuckPosition.position);
                    ctx.SetBusy(false);
                });
                return;
            }
```

And in the final busy-delay at the end of `Activate` (around line 91), change:

```csharp
            // Wait for the full vacuum animation before allowing another powerup.
            Tween.Delay(2.5f).OnComplete(() => ctx.SetBusy(false));
```
to:
```csharp
            // Wait for the full vacuum animation before allowing another powerup.
            Tween.Delay(2.5f).OnComplete(() =>
            {
                if (ctx.EndVfx != null && ctx.VacuumSuckPosition != null)
                    VfxPool.Instance.Play(ctx.EndVfx, ctx.VacuumSuckPosition.position);
                ctx.SetBusy(false);
            });
```

- [ ] **Step 2: Verify it compiles**

Unity compiles. Expected: no errors. (`VfxPool` is in `MatchThemAll.Scripts`; `VacuumEffect` already has `using MatchThemAll.Scripts;` on line 5.)

- [ ] **Step 3: Author the two prefabs (Unity Editor)**

Create two ParticleSystem prefabs under `Assets/Match Them All/Prefabs/VFX/Powerups/` (create the folder):

- **`Portal.prefab`** — sci-fi portal-open swirl: a torus/circular emitter, additive-blended material, cyan/violet palette, orbital velocity (Velocity-over-Lifetime) for the swirl. Main module: Duration ≈ 2.5s, Start Lifetime ≈ 2.0s, Stop Action = Destroy is NOT set (pool returns it). Emission: burst of ~30, rate over distance 0. Simulation Space = World.
- **`PortalCollapse.prefab`** — implosion burst: short-lived inward radial particles + a flash, magenta/white, Duration ≈ 0.6s, Start Lifetime ≈ 0.5s, one burst of ~40.

Keep both as a `ParticleSystem` component on an empty prefab root (no MonoBehaviour). Set the root's initial scale/rotation to identity; `VfxPool.Play` sets position at runtime.

- [ ] **Step 4: Assign prefabs on the vacuum SO**

Select `Assets/Match Them All/Resources/Powerups/Powerup_vacuum.asset`. In Inspector:
- drag `Portal.prefab` → `Activate Vfx`
- drag `PortalCollapse.prefab` → `End Vfx`

- [ ] **Step 5: Play-mode verify**

Enter Play mode, trigger the vacuum powerup (tap it when it has charges / afford the buy). Verify:
1. Portal swirl appears at the suck position immediately on activation.
2. After ~2.5s, the collapse burst plays at the same spot.
3. Trigger vacuum again immediately after — second portal plays (pool reuse, no errors).
4. Force the zero-items case (activate vacuum when no unassigned item matches the top goal) — portal still opens AND collapses (no silent moment).

Exit Play mode.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/VacuumEffect.cs" "Assets/Match Them All/Prefabs/VFX/" "Assets/Match Them All/Resources/Powerups/Powerup_vacuum.asset"
git commit -m "feat: vacuum portal open/collapse VFX"
```

---

### Task 5: Spring VFX (teleport charge)

**Files:**
- Modify: `Scripts/Runtime/PowerUps/SpringEffect.cs`

**Interfaces:**
- Consumes: `ctx.ActivateVfx`, `VfxPool.Instance.Play(prefab, pos)`, the existing `startPos` local.

- [ ] **Step 1: Add the VFX call**

In `SpringEffect.Activate`, right after `startPos` is computed (after line 41, `itemToRelease.transform.position = startPos;`), and before `// Pure physics throw`, add:

```csharp
            // Teleporter charge-up flash at the ejection point.
            if (ctx.ActivateVfx != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, startPos);
```

- [ ] **Step 2: Verify it compiles**

Unity compiles. Expected: no errors. (`SpringEffect` already has `using MatchThemAll.Scripts;` on line 4.)

- [ ] **Step 3: Author the prefab**

Create `Assets/Match Them All/Prefabs/VFX/Powerups/TeleportCharge.prefab`:
- Vertical beam emitter + a ground ring, cyan/white additive, Duration ≈ 0.4s, Start Lifetime ≈ 0.3s, one burst ~25. Simulation Space = World.

- [ ] **Step 4: Assign on the spring SO**

Select `Resources/Powerups/Powerup_spring.asset`. Drag `TeleportCharge.prefab` → `Activate Vfx`. Leave `End Vfx` unset.

- [ ] **Step 5: Play-mode verify**

Play mode: trigger spring (needs an occupied spot — `CanActivate` returns false otherwise). Verify:
1. Charge flash plays at the released item's start position, right before the physics throw.
2. Item still throws normally (VFX did not affect gameplay timing).
3. Repeat — second flash, no errors (pool reuse).

Exit Play mode.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/SpringEffect.cs" "Assets/Match Them All/Prefabs/VFX/Powerups/TeleportCharge.prefab" "Assets/Match Them All/Resources/Powerups/Powerup_spring.asset"
git commit -m "feat: spring teleport charge VFX"
```

---

### Task 6: Fan VFX (shockwave ring)

**Files:**
- Modify: `Scripts/Runtime/PowerUps/FanEffect.cs`

**Interfaces:**
- Consumes: `ctx.ActivateVfx`, `ctx.FanOrigin`, `VfxPool.Instance.Play(prefab, pos)`.

**Note:** `FanOrigin` is a scene Transform on the fan object. It may be null until you wire it (Task 8). The call is null-guarded on both the prefab and the origin, so the effect compiles and runs safely now; VFX just won't show until the origin is assigned.

- [ ] **Step 1: Add the VFX call**

In `FanEffect.Activate`, at the very top of the method (right after `if (ctx.Items == null) return;`), add:

```csharp
            // Shockwave ring at the fan's origin.
            if (ctx.ActivateVfx != null && ctx.FanOrigin != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.FanOrigin.position);
```

- [ ] **Step 2: Verify it compiles**

Unity compiles. Expected: no errors. (`FanEffect` already has `using MatchThemAll.Scripts;` on line 3.)

- [ ] **Step 3: Author the prefab**

Create `Assets/Match Them All/Prefabs/VFX/Powerups/ShockwaveRing.prefab`:
- Expanding ring (use a Mesh emitter with a ring/torus mesh, or a Particle System with Size-over-Lifetime growing + fading), cyan/violet additive, Duration ≈ 0.5s, Start Lifetime ≈ 0.5s, one burst ~1 (a single growing ring) or ~20 on a cone for a blast look. Simulation Space = World.

- [ ] **Step 4: Assign on the fan SO**

Select `Resources/Powerups/Powerup_fan.asset`. Drag `ShockwaveRing.prefab` → `Activate Vfx`. Leave `End Vfx` unset.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/FanEffect.cs" "Assets/Match Them All/Prefabs/VFX/Powerups/ShockwaveRing.prefab" "Assets/Match Them All/Resources/Powerups/Powerup_fan.asset"
git commit -m "feat: fan shockwave ring VFX"
```

(Full Play-mode verification happens after Task 8 wires `fanOrigin`; the code is safe to land now.)

---

### Task 7: Freeze VFX (muzzle flash)

**Files:**
- Modify: `Scripts/Runtime/PowerUps/FreezeEffect.cs`

**Interfaces:**
- Consumes: `ctx.ActivateVfx`, `ctx.FreezeMuzzle`, `VfxPool.Instance.Play(prefab, pos)`. Same null-safe pattern as fan.

- [ ] **Step 1: Rewrite `Activate` to emit VFX**

Replace the body of `FreezeEffect.Activate` (currently a one-liner):

```csharp
        public override void Activate(PowerupContext ctx)
        {
            // Muzzle flash at the freeze gun's muzzle.
            if (ctx.ActivateVfx != null && ctx.FreezeMuzzle != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, ctx.FreezeMuzzle.position);

            ctx.Timer.FreezeTimer();
        }
```

Keep `CanActivate` unchanged. The `using MatchThemAll.Scripts;` on line 1 already covers `VfxPool`.

- [ ] **Step 2: Verify it compiles**

Unity compiles. Expected: no errors.

- [ ] **Step 3: Author the prefab**

Create `Assets/Match Them All/Prefabs/VFX/Powerups/MuzzleFlash.prefab`:
- Short cone + sparks, ice-blue/white additive, Duration ≈ 0.2s, Start Lifetime ≈ 0.2s, one burst ~15. Simulation Space = World.

- [ ] **Step 4: Assign on the freeze SO**

Select `Resources/Powerups/Powerup_freeze.asset`. Drag `MuzzleFlash.prefab` → `Activate Vfx`. Leave `End Vfx` unset.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Match Them All/Scripts/Runtime/PowerUps/FreezeEffect.cs" "Assets/Match Them All/Prefabs/VFX/Powerups/MuzzleFlash.prefab" "Assets/Match Them All/Resources/Powerups/Powerup_freeze.asset"
git commit -m "feat: freeze gun muzzle flash VFX"
```

(Full Play-mode verification after Task 8 wires `freezeMuzzle`.)

---

### Task 8: Scene wiring — `fanOrigin`, `freezeMuzzle`, and full verification

**Files:**
- Modify: `Scenes/MainScene.unity`

**Why last:** the fan/freeze origin Transforms must exist on the 3D fan/freeze-gun objects in the scene. If those sci-fi models aren't built yet, create empty child Transforms named `FanOrigin` / `FreezeMuzzle` at the intended spawn points — they can be reparented to the real models later without touching code.

- [ ] **Step 1: Add origin Transforms to the scene**

- On (or as a child of) the fan powerup object: add an empty child GameObject named `FanOrigin`, positioned where the shockwave should emit (center of the fan/grenade).
- On (or as a child of) the freeze-gun object: add an empty child named `FreezeMuzzle`, positioned at the gun's barrel tip.

If the real sci-fi fan/gun models don't exist yet, place these Transforms near the existing placeholder powerup objects — move them once the models are in.

- [ ] **Step 2: Wire them on `PowerupManager`**

Select the `PowerupManager` GameObject in `MainScene`. In Inspector:
- drag `FanOrigin` → the `Fan Origin` field.
- drag `FreezeMuzzle` → the `Freeze Muzzle` field.

Save the scene.

- [ ] **Step 3: Full Play-mode verification (all 4 powerups)**

Enter Play mode. For each powerup, verify VFX plays at the correct origin and gameplay is unaffected:

| Powerup | VFX | Origin | Verify |
|---------|-----|--------|--------|
| vacuum | portal open + collapse | suck position | opens on activate, collapses ~2.5s later, even on zero items |
| spring | teleport charge | ejected item's startPos | flashes before throw, throw still works |
| fan | shockwave ring | fan object | ring expands once on activate, items still shoved |
| freeze | muzzle flash | gun muzzle | flash on activate, timer still freezes (check timer UI) |

Also verify: triggering each powerup rapidly twice → pooled reuse, no `Instantiate` spam, no errors in Console.

Exit Play mode.

- [ ] **Step 4: Profiler allocation check (pool verification)**

Open Window → Analysis → Profiler. Enter Play mode. Trigger the vacuum 5 times in a row while recording. In the CPU Usage / Memory module, confirm:
- The first trigger shows one `Instantiate` (pool create).
- Triggers 2–5 show **no** `Instantiate` for the particle (pool reuse — this is the success criterion for pooling).
- No GC alloc spikes per activation after warmup.

If you see per-activation `Instantiate`, the prefab-keyed pool isn't matching — check that the same prefab asset (not a duplicate) is assigned across calls.

Exit Play mode.

- [ ] **Step 5: Customizability check (the contract)**

In the Inspector for `Powerup_vacuum.asset`, swap `Activate Vfx` to `MuzzleFlash.prefab` (wrong effect, on purpose). Enter Play mode, trigger vacuum. Confirm the muzzle-flash effect now plays at the suck position with **no code change**. Exit Play mode, swap it back to `Portal.prefab`. This proves the data-driven customizability works.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Match Them All/Scenes/MainScene.unity"
git commit -m "feat: wire fan/freeze VFX origins, verify all powerup VFX"
```

---

## Verification Summary (Success Criteria from spec §11)

- [ ] Each of 4 powerups plays distinct sci-fi VFX at the correct origin (Task 8 Step 3).
- [ ] Pooled — no per-activation Instantiate after warmup (Task 8 Step 4).
- [ ] Swapping `activateVfx` in Inspector changes the effect with no code change (Task 8 Step 5).
- [ ] Null `activateVfx` = no error, no effect (each effect call is null-guarded).
- [ ] No existing Inspector references broken (all fields additive; verified by Play-mode boot with no missing-reference errors).
- [ ] Tutorial layer routing matches MergeManager (same `InputManager.IsTutorialActive` check in `VfxPool.Play`).

## Deferred (out of scope, tracked)

- **Freeze screen-frost tint** — UI/camera overlay, follow-up task once muzzle flash is confirmed sufficient. (Spec §12 Q1.)
- **MergeManager migration to `VfxPool`** — possible now that `VfxPool` exists, but not required; leave MergeManager's dedicated pool as-is.
