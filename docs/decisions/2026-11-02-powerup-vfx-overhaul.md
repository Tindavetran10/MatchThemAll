# Power-up VFX overhaul (Freeze, Bomb/Fan, Spring, Vacuum)

**Date:** 2026-11-02

## Context
The four power-ups worked but read weakly. Each now has its own sequence; all art and sound is generated
(`_AssetGen`: VfxGen, ModelGen, SpriteGen, SfxSynth), so nothing third-party was added.

## Decisions
- **`PowerupContext.Source`** — the tapped power-up's tray prop (set in `PowerupManager.BuildContext`). Effects animate
  their own prop (gun lifts/aims, canister jiggles, pad squashes) without new scene references.
- **Freeze countdown lives in `TimerManager`** (`freezeDuration`, ticks only in `GAME`, `FreezeRemaining01`,
  `TimerFrozenEvent` / `TimerUnfrozenEvent`). Replaces `Invoke(UnfreezeTimer, 10f)`, which kept counting while paused
  or in popups. `FreezeFx` (frost, ice spheres, bar under the clock) is driven only by those events.
- **Frost on its own Screen Space - Camera canvas** (`FreezeOverlay`, plane distance 10.6) so the ice spheres, item
  spots and tray draw over it while it still covers the item pile; the HUD overlay stays on top.
- **Tray props move toward the camera when animated** (`FreezeEffect.shootLift`), otherwise the item spots (closer to
  the camera) cover them.
- **Spring is tween-driven, not a physics throw**: hop onto the pad → pad squash → arc with trail → landing puff/thud →
  physics back on. The old throw tuning fields and the trajectory gizmo were removed with it.
- **Vacuum items spiral** (helix tightening into the nozzle) inside a tornado particle funnel.
- Sounds that belong to an animation beat are delayed inside the clip (`SfxSynth.At`), because `PowerupManager` plays
  the clip on the tap.

## Tuning knobs
`FreezeEffect` (laserTarget, boardHeight, shootLift), `FreezeFx.spherePoints`, `TimerManager.freezeDuration`,
`FanEffect.fuseTime`, `SpringEffect` (hopTime, flightTime, arcHeight, landing area, heights),
`VacuumEffect` (spiralTime, spiralTurns, spiralRadius). Look of the particles: `VfxGen.BuildPowerupFx` → *Generate Powerup FX*.
