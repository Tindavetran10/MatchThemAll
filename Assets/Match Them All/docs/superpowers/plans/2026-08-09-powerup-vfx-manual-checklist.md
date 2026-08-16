# Powerup VFX — Manual Phase Checklist (Task 8)

Everything the code branch (feat/powerup-vfx) needs from the Unity Editor to go live.
Done so far by automation: all C#, `VfxPool` GameObject placed in `MainScene` under
`//MANAGERS` (identity transform), `Prefabs/VFX/Powerups/` folder exists,
`PowerupManager.fanOrigin`/`freezeMuzzle` fields exist (currently null — safe no-ops).

**Do the steps in order. Step 1 before Step 3 is mandatory** (though the code now
null-guards a missing VfxPool, ordering keeps the checklist foolproof).

---

## Step 1 — Place VFX origin Transforms (fan, freeze)

The Vacuum already has its 3D model + `vacuumSuckPosition` wired. Fan/Freeze have no
models yet, so create placeholder Transforms now; reparent to the real models later
(no code change needed):

1. In `MainScene`, create an empty child under `//GAMEPLAY` named `FanOrigin`,
   positioned where the sci-fi grenade's blast center will read well on screen.
2. Create an empty child named `FreezeMuzzle` where the freeze gun's barrel tip will be.
3. Select `//MANAGERS/PowerupManager`:
   - drag `FanOrigin` → **Fan Origin**
   - drag `FreezeMuzzle` → **Freeze Muzzle**
4. Save the scene.

## Step 2 — Author the 5 particle prefabs

Folder: `Assets/Match Them All/Prefabs/VFX/Powerups/` (exists).

For each: create empty GameObject → add ParticleSystem → tune → drag to the folder
(Delete the scene copy after saving as prefab).

**Prefab contracts (from code review — hard rules):**
- **Looping = OFF** on the main module (the pool recycles on a timer; a looping system
  would be recycled mid-emission).
- **Stop Action = None** (never Destroy — the pool owns lifetime).
- **Single self-contained ParticleSystem on the prefab root** (no child sub-systems —
  the layer is set on the root only, mirroring MergeManager).
- One-shot burst emission profile (matches the pool's duration+lifetime return timer).

| Prefab | For | Look (sci-fi) | Suggested tuning |
|---|---|---|---|
| `Portal.prefab` | vacuum **Activate Vfx** | portal-open swirl | Duration ≈ 2.5s (matches busy window), cyan/violet additive, orbital velocity-over-lifetime, burst ~30 |
| `PortalCollapse.prefab` | vacuum **End Vfx** | implosion burst | Duration ≈ 0.6s, magenta/white, burst ~40 |
| `TeleportCharge.prefab` | spring **Activate Vfx** | vertical beam + ground ring | Duration ≈ 0.4s, cyan/white, burst ~25 |
| `ShockwaveRing.prefab` | fan **Activate Vfx** | expanding ring | Duration ≈ 0.5s, size-over-lifetime growing + fading, burst ~1–20 |
| `MuzzleFlash.prefab` | freeze **Activate Vfx** | short cone + sparks | Duration ≈ 0.2s, ice-blue/white, burst ~15 |

## Step 3 — Assign prefabs on the SOs

- `Resources/Powerups/Powerup_vacuum.asset`: Activate = `Portal`, End = `PortalCollapse`
- `Resources/Powerups/Powerup_spring.asset`: Activate = `TeleportCharge`, End = none
- `Resources/Powerups/Powerup_fan.asset`: Activate = `ShockwaveRing`, End = none
- `Resources/Powerups/Powerup_freeze.asset`: Activate = `MuzzleFlash`, End = none

## Step 4 — Play-mode verification

| Powerup | Expect |
|---|---|
| vacuum | portal opens at suck position on activate; collapse ~2.5s later — **also when 0 items match** |
| spring | charge flash at ejected item's position, right before the physics throw |
| fan | shockwave ring at `FanOrigin`; items still shoved |
| freeze | muzzle flash at `FreezeMuzzle`; timer still freezes 10s |

Also: trigger each powerup twice rapidly → pooled reuse, no errors in Console.

## Step 5 — Profiler allocation check

Window → Analysis → Profiler, record, trigger vacuum 5×:
- 1st trigger: one `Instantiate` (pool create). Triggers 2–5: **no** Instantiate.

## Step 6 — Customizability check (the contract)

On `Powerup_vacuum.asset`, swap Activate Vfx → `MuzzleFlash.prefab`, play, confirm the
muzzle flash now plays at the suck position with zero code changes. Swap back.

## Step 7 — Commit + housekeeping

```bash
git add -A
git commit -m "feat: powerup VFX prefabs + scene wiring (manual phase)"
```

Then run `graphify update .` (project convention — VfxPool is a new hub node).

---

## Deferred follow-ups (tracked, not lost)

- Freeze screen-frost tint (UI overlay) — separate small task if freeze feels incomplete.
- MergeManager migration to VfxPool — optional cleanup, not required.
