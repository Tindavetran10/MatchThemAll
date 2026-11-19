# MatchThemAll — Claude Code Context

Hyper-casual match-3D template (Unity 6000.0, URP 17.0.4). Shared rules (how to work, memory, Unity workflow and pitfalls, model routing, skill map) live in `~/.claude/CLAUDE.md`. This file is only what is specific to this game.

Art and mobile skills: `lowpoly-style`, `cartoon-style`, `genre-card-party-idle`; mobile budgets via `unity:optimize-*`, `unity:manage-sprite-atlas`; monetisation via `unity:implement-in-app-purchases`, `unity:levelplay-unity-integration`.

## 6. Hot spots
Most connected classes (query `graphify query "What depends on <Class>?"` before touching them): `LevelEditorWindow`, `ItemManagerWindow`, `ItemSpotManager`, `SaveManager`, `TutorialManager`, `PowerupManager`, `Item`, `TimerManager`, `InputManager`, `GoalManager`. Bridge nodes (change ripples across communities): `ItemSpotManager`, `PowerupManager`, `TutorialManager`.

## 7. Architecture Rules

### Communication Pattern
- **All inter-system events go through `EventBus<T>`** — subscribe in `GameEvents.cs`
- Never add direct MonoBehaviour references between unrelated systems
- `GameStateChangedEvent` is the primary state broadcast — most managers subscribe to it

### Key Events (defined in `GameEvents.cs`)
- `GameStateChangedEvent` — game state transitions (Playing, Paused, Win, Lose…)
- `ItemClickedEvent` — player taps an item
- `ItemReachedSpotEvent` — item lands on a spot
- `SpotFilledEvent` — a spot is fully occupied
- `MergeStartedEvent` — combo/merge chain begins

### No Import Cycles
- Graph confirms **0 import cycles** — keep it that way
- `MatchThemAll.Scripts` namespace: gameplay only
- `MatchThemAll.Scripts.Editor` namespace: editor tools only (never reference from runtime)

### Save System
- `SaveManager` is bootstrapped via `RuntimeInitializeOnLoadMethod` in `SaveManagerBootstrapper`
- Access via `SaveManager.Instance` — do **not** drag it into the Inspector
- `PlayerData` is the serialized model — modify only through `SaveManager`

### Shop & Economy
- Gem/currency reads and writes must go through `SaveManager` — never modify `PlayerData` fields directly
- `ShopManager` owns product validation; `ShopProductCard` is purely presentational

### Performance (ZLinq)
- LINQ is replaced with ZLinq (`AsValueEnumerable()`) in hot paths — maintain this pattern in new code inside managers
- Avoid allocations in `Update()` / per-frame callbacks

### Known Pitfalls (index; full text + repro steps in `docs/claude-pitfalls.md`)
Shared Unity pitfalls (real state path, `#if UNITY_EDITOR`, async cancellation, game-state switches, prefab refs, model swaps, overlay layering, RunCommand aliases/Step) are in the global file; the project-specific detail for them is in the doc.
- Items/props: one submesh + one material `M_Palette` (`Item.Select` swaps materials).
- Tests that change the save: restore it, then `SaveManager.Flush()` (stopping Play mode doesn't write it).
- Merged sprites keep the top layer's sorting order (`Video Icon` above `Container`).
- No silent spending: every currency cost is an explicit choice (`PowerupOfferPanel`).
- Player builds: after editor-only branches, run `PlayerBuildInterface.CompilePlayerScripts` from RunCommand.
- `GameStateChangedEvent`: list do-nothing states explicitly; `TimerManager` resumes only a timer that was running.
- Scene fades: only `SceneLoader.FadeToBlack` / `FadeFromBlack`; measure with a frame trace on `DebugCheats`.
- `AdManager` self-bootstraps (no scene has it); grep GUIDs before assuming a manager exists.
- Monetization is compile-optional: never reference LevelPlay/IAP SDK types from Assembly-CSharp.
- Tray: `ItemSpotManager.Arrived` runs `MatchSystem.CheckSettledTray()` after the last mover settles.
- Store: grant + save only in `ShopManager.GrantStorePurchase`, never in the `TryPurchase` callback.
- State panels (`Level Complete` etc.) must be saved inactive (`WinPanelManager.OnEnable` pays out).
- Currency grants publish `CurrencyGrantedEvent`; never publish from a panel's `OnEnable` to its own children.
- Daily reward date = last *claim* date (`DailyRewardManager.GrantReward`).
- Power-ups are data-driven (`PowerupDatabase`, `Match Them All > Power-up Manager`): never key on `so.id`, darken via `MaterialPropertyBlock` not `M_Palette`, taps wait for `LevelManager.IsLevelReady`, tray redraws on `LevelSpawned`. Read the doc before any power-up change.
- Starting charges are seeded per id: renaming an id re-grants them. Effect runtime state must be `[NonSerialized]`.
- Vacuum rig: rebuild with `Generate Vacuum Hose` + `Wire Vacuum`; clip keys use ClampedAuto tangents. RunCommand: `using UMesh = UnityEngine.Mesh;`.
- Power-up VFX: regenerate with *Generate Powerup FX*, not *Generate VFX*.
- Timer: nothing hangs below `Timer Container`; freeze UI lives inside it (`FreezeBar`, `FreezeBadge`); `FreezeOverlay` planeDistance 10.6.
- RunCommand keeps going after a logged error: check `IsPartOfPrefabInstance` before reparenting, verify after saving.
- Don't save scripts during Play mode (statics reset, NREs are artifacts).
- Unity 6000.0 / URP 17.0.4 only; never commit from a newer editor (`k_AssetVersion` in `Settings/*RPAsset.asset`).
- Button Color Tint `Normal Color` stays white.
- `GameTitle.prefab` owns text/font/material; scenes override only position/size/glow.
- The user edits scenes by hand: never run `UI Overhaul` (or any builder) blind; snapshot, run, diff, restore.
- IMGUI windows: always draw the Save button (disabled when clean), `GUI.FocusControl(null)` when a field hides, no `[TextArea]` in `PropertyField`.
- Template must compile alone: test via clean-project import of `Assets/MatchThemAllTemplate` only; no scratch scripts in it.
- `ProjectSetup.cs` owns buyer setup; layers by number (Match Stuff 6, Power Ups 7, Tutorial 8); find the RP asset by GUID.
- Tutorial steps must set `InputManager.IsTutorialActive` for items AND power-ups; test through real input (see doc), back up `save.json`.
- Sound volumes live per event in `SoundLibrary.asset`; mixer volumes are the player's settings.

## 10. Obsidian Second Brain

The vault root is the project root (where `.obsidian/` lives). In Obsidian:
- `graphify-out/GRAPH_REPORT.md` — clickable `[[community links]]` for navigation
- `graphify-out/graph.html` — interactive visual graph explorer
- `docs/` — architecture decision records, specs, plans

When you make an architectural decision, log it in Obsidian under `docs/decisions/`.

---

## 12. Quick Commands

```powershell
# Query the knowledge graph (use before browsing code)
graphify query "What depends on ItemSpotManager?"
graphify path "MergeManager" "GoalManager"
graphify explain "EventBus"

# Update graph after code changes (AST-only, no API cost)
graphify update .

# Full re-cluster (regenerates GRAPH_REPORT.md)
graphify cluster-only .
```
