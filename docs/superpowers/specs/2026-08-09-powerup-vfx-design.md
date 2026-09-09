# Powerup VFX Design (Hybrid, Sci-Fi Theme)

**Date:** 2026-08-09
**Status:** Draft
**Scope:** Add mechanic-tied, customizable VFX to all 4 powerups (vacuum, spring, fan, freeze).

---

## 1. Goal

Each powerup gets a distinct sci-fi visual effect that represents *what it does*,
fully customizable per-powerup, with a setup simple enough that a user adding a
new custom powerup can drop in their own VFX without writing spawn logic.

**Non-goals (YAGNI):**
- No new VFX *engine* or generic effect sequencer. We reuse the existing
  `MergeManager` pooled-`ParticleSystem` pattern.
- No reworking of powerup gameplay logic — only visual layer.
- No audio changes (SoundManager already handles powerup SFX).

## 2. Design Choice: Hybrid

- **Prefabs are data-driven** — slots live on `PowerupDataSO`, swapped in the Inspector.
- **Spawn behavior is code-driven** — each `PowerupEffect` subclass decides where/when
  the VFX plays (mirrors how each effect already owns its own tween/physics logic).

This gives custom-powerup authors a clear contract: *fill the prefab slots, the effect
class handles the rest.* Authors of brand-new mechanic types write one spawn call.

## 3. VFX Slots (the customization surface)

Add to `PowerupDataSO`, under a new `[Header("VFX")]`:

```csharp
[Header("VFX")]
[SerializeField] private ParticleSystem activateVfx;   // plays once at activation
[SerializeField] private ParticleSystem endVfx;        // plays once when effect resolves (optional)
```

`activateVfx` is the only required slot. `endVfx` is optional (nullable) — used by
powerups that have a clear "closing" beat (portal collapse, gun power-down). A
future `loopVfx` can be added if a powerup needs a sustained effect; not added now.

Read-only accessors `ActivateVfx` / `EndVfx` mirror the existing `Icon`/`UIPrefab` pattern.

## 4. The Spawner — `VfxPool`

A single MonoBehaviour owns a per-prefab `ParticleSystem` pool, exactly the
`MergeManager` pattern generalized to "any prefab, any position."

**Name & location:** `VfxPool` in `Scripts/Runtime/VFX/` — a new folder, because VFX in
this project isn't powerup-only (`MergeManager` particles, `ComboVFX`, `FloatingTextSpawner`).
A general-purpose, prefab-keyed pool belongs outside the PowerUps namespace so other
systems can use it (MergeManager migration to this pool is *possible* but out of scope).

**Why a dedicated component, not inline in each effect:**
- Effects are `[SerializeReference]` plain C# objects — they cannot hold `MonoBehaviour`
  state, cannot run coroutines, and are shared across the SO (no per-instance pool).
- A pooled spawner is reused by all 4 effects (and future ones), keeping effects thin.
- Mirrors `ItemPoolManager` (keyed pooling) and `MergeManager` (particle pooling) —
  both established in this codebase.

**API (deliberately tiny):**

```csharp
public static VfxPool Instance { get; }

// Spawns prefab at pos (world), sets layer, plays, returns to pool after its lifetime.
// Null-safe: no prefab = no-op (so optional slots cost nothing).
public void Play(ParticleSystem prefab, Vector3 pos);
```

**Pooling:** `Dictionary<ParticleSystem, IObjectPool<ParticleSystem>>`, keyed by the
source prefab so different prefabs don't share instances. `createFunc` instantiates
under the pool's transform; `actionOnGet` sets active + position + layer;
`actionOnRelease` sets inactive. Lifetime = `main.duration + main.startLifetime.constantMax`,
returned via the same `StartCoroutine(ReturnToPool)` guard MergeManager uses. Pre-warm
omitted (powerups fire less often than merges; first-call Instantiate is acceptable).

**Layer handling:** same as MergeManager — `Tutorial` layer when
`InputManager.IsTutorialActive`, else `Default`.

## 5. Per-Powerup Behavior (the code-driven half)

Each `PowerupEffect` subclass calls `PowerupVfxPool.Instance.Play(...)` at the right
moment and origin. Spawn origins come from `PowerupContext` (no new singletons reached
into by effects).

### vacuum → portal
- **activateVfx:** portal-open swirl at `ctx.VacuumSuckPosition` — plays at the start of
  `Activate`, before items begin vortexing in.
- **endVfx:** portal-collapse burst at the same position — plays after the 2.5s busy delay
  completes (in the existing `Tween.Delay(...).OnComplete`).
- **Plays regardless of item count** — even the zero-items early-return branch
  (`VacuumEffect.cs:59-64`) fires both VFX, so there's no jarring silent "powerup did
  nothing" moment mid-gameplay. (See §12 Q3 resolution.)
- The existing item vortex/shrink/spin tweens stay (they're the *item* animation; VFX is
  the portal itself).

### spring → teleport machine
- **activateVfx:** teleporter charge-up flash at the released item's *start position*
  (`itemToRelease.transform.position + Vector3.up * 1f`, the existing `startPos`).
- **endVfx:** none (the item's physics throw is the closing visual).
- Plays right before `EnablePhysics()`.

### fan → sci-fi grenade
- **activateVfx:** grenade-burst shockwave ring at a fixed world origin (the fan object's
  position — needs a context field, see §7).
- Plays once at the start of `Activate`; the per-item force application is unaffected.
- **endVfx:** none.

### freeze → freeze gun
- **activateVfx:** muzzle flash / freeze beam at the freeze gun's muzzle (world origin —
  needs a context field, see §7).
- Plus a short screen-space frost tint is **deferred to a follow-up task** — it needs a
  UI/camera-overlay hook separate from the world-particle pool. The world muzzle effect
  is built first; if the freeze still feels incomplete once everything is wired, the frost
  tint is added as its own small task. (See §12 Q1 resolution.)

## 6. Hook Point — `PowerupVfxEvent` vs. in-effect calls

**Decision: spawn from inside each `PowerupEffect.Activate`**, not from a new event.

Considered: a `PowerupActivatedEvent` on `EventBus` fired at `PowerupManager.cs:118`,
with a subscriber spawning VFX. **Rejected** because the per-powerup *timing* (vacuum's
2.5s-later end burst, spring's mid-throw) cannot be expressed from a single fire-point
without re-implementing each effect's timeline in the subscriber — that duplicates the
effect logic and defeats the "thin effect" goal.

In-effect calls are co-located with the behavior they visualize, need no new event, and
keep `PowerupManager` untouched. (The existing `PowerupClickedEvent` already exists for
any *button-feedback* VFX if wanted later — not part of this spec.)

## 7. PowerupContext additions

Two effects (fan, freeze) need a world origin that isn't currently in context:

```csharp
public Transform FanOrigin;       // the fan object's transform
public Transform FreezeMuzzle;    // the freeze gun's muzzle transform
```

Wired in `PowerupManager.BuildContext` from new serialized fields on `PowerupManager`:

```csharp
[Header("Fan Elements")]   private Transform fanOrigin;
[Header("Freeze Elements")] private Transform freezeMuzzle;
```

Vacuum already has `VacuumSuckPosition`; spring derives origin from the item itself
(no new field). These are nullable — an effect that finds its origin null simply
skips VFX (graceful, matches "optional slot" philosophy).

## 8. File-by-File Change Summary

| File | Change |
|------|--------|
| `PowerUps/PowerupDataSO.cs` | Add `[Header("VFX")]` + `activateVfx`/`endVfx` fields + read-only accessors. |
| `PowerUps/PowerupContext.cs` | Add `FanOrigin`, `FreezeMuzzle` Transform fields. |
| `PowerUps/PowerupManager.cs` | Add `fanOrigin`/`freezeMuzzle` serialized fields; wire in `BuildContext`. |
| **NEW** `Scripts/Runtime/VFX/VfxPool.cs` | General-purpose pooled spawner MonoBehaviour (§4). Scene-placed singleton. |
| `PowerUps/VacuumEffect.cs` | Play activate at suck pos; play end in both the 2.5s `OnComplete` callbacks (item-collect and zero-item branches). |
| `PowerUps/SpringEffect.cs` | Play activate at `startPos` before `EnablePhysics`. |
| `PowerUps/FanEffect.cs` | Play activate at `ctx.FanOrigin`. |
| `PowerUps/FreezeEffect.cs` | Play activate at `ctx.FreezeMuzzle`. |
| **NEW** 4 ParticleSystem prefabs under `Prefabs/VFX/Powerups/` | portal, teleport-charge, shockwave-ring, muzzle-flash. |

No changes to `GameEvents.cs`, `InputManager`, `MergeManager`, or the 4 SO assets'
existing fields (the new VFX slots are additive — Inspector references stay intact).

## 9. Prefab Authoring (the 4 effects)

Built in Unity's Particle System editor, sci-fi palette (cyan/magenta/violet,
project-wide). Each is a self-contained `ParticleSystem` on a prefab (no scripts):

- **Portal:** torus emitter, additive shader, swirl via Velocity-over-Lifetime orbital,
  ~2.5s lifetime to match vacuum's busy window.
- **Teleport charge:** vertical beam + ground ring, 0.4s burst.
- **Shockwave ring:** expanding ring mesh emitter, 0.5s, fades out.
- **Muzzle flash:** short cone + sparks, 0.2s.

Detailed tuning happens in-editor during implementation, not in this spec.

## 10. Bootstrap

`VfxPool` is a `MonoBehaviour` placed once in `MainScene` (like `MergeManager`).
A `Singleton`-style `Instance` with the standard `Awake` guard. No
`RuntimeInitializeOnLoadMethod` needed (it's scene-local, like the other gameplay managers).

## 11. Success Criteria

1. Activating each of the 4 powerups plays its distinct sci-fi VFX at the correct origin.
2. VFX is pooled — no `Instantiate`/`Destroy` per activation after warmup (verify via
   Profiler or a debug log count).
3. Swapping a powerup's `activateVfx` prefab in the Inspector changes the effect with no
   code change (the customizability contract).
4. Setting `activateVfx` to null produces no error and no effect (graceful).
5. No Inspector references broken on existing prefabs/SOs (additive fields only).
6. Tutorial layer routing matches MergeManager behavior.

## 12. Reviewer Questions — Resolved

- **Q1 (freeze screen-frost tint):** Deferred to a follow-up task. The world muzzle flash
  is built first; the frost tint is added later as its own small task if the freeze still
  feels incomplete. Not lost — tracked here.
- **Q2 (spawner location):** New folder `Scripts/Runtime/VFX/`, class renamed
  `PowerupVfxPool` → **`VfxPool`** (general-purpose, not powerup-specific).
- **Q3 (vacuum zero-items):** Not skipped. Both activate and end VFX fire even when zero
  items are collected — prevents a silent "powerup did nothing" moment mid-gameplay.
