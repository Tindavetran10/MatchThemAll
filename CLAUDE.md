# MatchThemAll — Claude Code Context

Behavioral guidelines adapted for this Unity project. These rules reduce common LLM coding mistakes
and encode architecture knowledge specific to MatchThemAll.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

---

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.
- For Unity-specific tasks, verify the API exists (Unity docs, or reflection via `Unity_RunCommand`) before writing code — training data may be stale.

---

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use MonoBehaviours or ScriptableObjects.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible Unity lifecycle scenarios.
- If you write 200 lines and it could be 50, rewrite it.
- Prefer extending existing managers (e.g. `PowerupManager`, `GoalManager`) over creating new ones.

Ask yourself: "Would a senior Unity engineer say this is overcomplicated?" If yes, simplify.

---

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing Unity scripts:
- Don't "improve" adjacent code, serialized fields, or Inspector labels.
- Don't refactor things that aren't broken.
- Match existing style (field naming, region layout, event subscription pattern).
- If you notice unrelated dead code, mention it — don't delete it.
- Never rename public fields or serialized properties without flagging it — this breaks Inspector references and prefab overrides.

When your changes create orphans:
- Remove `using` directives, variables, and methods that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

---

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add a power-up" → "Wire up the new type in `PowerupManager`, subscribe to events in `GameEvents.cs`, test in Play mode"
- "Fix the bug" → "Reproduce it first, identify the root cause, then fix — don't guess"
- "Refactor X" → "Confirm all call sites compile and behavior is unchanged"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

**Mandatory Verification Before Finalizing:**
- ALWAYS check the Unity console (via the Unity MCP tool or any console reading tool) for compilation or runtime errors before telling the user the task is complete. If you spot common Unity errors (e.g., missing references, compiler errors, null references), fix them automatically and update this file with any new common mistakes you discover before finalizing your response.

---

## 5. graphify

This project has a knowledge graph at `graphify-out/` with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when `graphify-out/graph.json` exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than `GRAPH_REPORT.md` or raw grep output.
- If `graphify-out/wiki/index.md` exists, use it for broad navigation instead of raw source browsing.
- Read `graphify-out/GRAPH_REPORT.md` only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

---

## 6. God Nodes (most connected — touch with extreme care)

These classes have the most edges in the graph. Changes here have wide ripple effects.

| Rank | Class | Edges | Role |
|------|-------|-------|------|
| 1 | `LevelEditorWindow` | 46 | Hub of all Editor tooling |
| 2 | `ItemManagerWindow` | 45 | Item configuration + 3D preview |
| 3 | `ItemSpotManager` | 34 | Cross-community bridge (Gameplay ↔ Save ↔ UI) |
| 4 | `SaveManager` | 26 | Singleton persistence — no direct refs from gameplay |
| 5 | `TutorialManager` | 26 | Cross-community bridge (Events ↔ Lose Panel) |
| 6 | `PowerupManager` | 22 | Cross-community bridge (Power-Ups ↔ Lose Panel) |
| 7 | `Item` | 21 | Core gameplay object, referenced everywhere |
| 8 | `TimerManager` | 20 | Game-state-aware timing |
| 9 | `InputManager` | 19 | Input routing hub |
| 10 | `GoalManager` | 18 | Goal tracking + GoalCard UI |

> Before touching any God Node, run: `graphify query "What depends on <ClassName>?"`

---

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

### Known Pitfalls
- Item/prop models must stay **one submesh + one material** (`M_Palette`, built by `Tools ▸ AssetGen ▸ Generate Models`). `Item.Select` swaps in `[baseMaterial, outline]`, so a multi-material renderer loses its colours when tapped/hinted.
- Unity MCP `RunCommand` refuses `AssetDatabase.DeleteAsset(s)` ("user interaction"). Delete with `git rm` (file + `.meta`), then `AssetDatabase.Refresh()`.
- **Verify the real state path, never a forced one.** For anything that depends on game state (e.g. the video icon at 0 charges), reproduce the state through gameplay/`SaveManager` (drain charges with `UsePowerupCharge`) and tap through `PowerupClickedEvent` — don't just `SetActive(true)` the object. If something looks hidden or wrong during a check, it is a bug until proven otherwise; never write it off as "expected". Record and restore any save data a test changes, then call `SaveManager.Flush()` — stopping Play mode from a tool does not reliably write the save, so an unflushed restore is silently lost.
- **Merging stacked sprites keeps the top layer's sorting order.** When collapsing several SpriteRenderers into one baked sprite, give it the highest order of the old stack, and check every sibling at the same position. `Video Icon` (order 1) must stay above `Container` (order 0), which is never hidden at 0 charges.
- **No silent spending.** Anything that costs currency must be an explicit player choice (`PowerupOfferPanel` popup), never an automatic fallback inside a click handler.
- **`#if UNITY_EDITOR` code is never compiled by the Editor's `#else` side.** A clean Editor console proves nothing about player builds (`@await` typo and a `CreateInstance` with no `#else` return both slipped in this way). After touching code with editor-only branches, compile player scripts: `PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings { target, group }, "Temp/PlayerScriptCheck")` from `RunCommand`.
- **No `default: throw` in `GameStateChangedEvent` switches.** `GAME` arrives from many states (fresh start, resume, after offer); `case GAME when …` guards leave the rest to `default`, and a throw there fires on every level start (Error Pause, timer never starts). List the do-nothing states explicitly. Also delete IDE "wrap in try/catch" stubs (`catch { throw; // TODO }`) — they handle nothing.
- **Async work must stop when its object is destroyed (scene left mid-spawn).** Read `destroyCancellationToken` into a local while the object is alive, pass it to the await (`Task.Delay(10, token)`), and call `token.ThrowIfCancellationRequested()` *after* each await — a delay that finished just before the unload still runs its continuation on destroyed objects. Catch `OperationCanceledException` silently in the `async void` caller (`LevelManager.SpawnLevel`). Test with a coroutine on the DDOL `DebugCheats` object that calls `SceneLoader.LoadLevel` at set spawn counts; tool-call polling is too slow to hit the window.
- **Unscaled tweens count load hitches.** A tween started right after a scene switch loses its first few hundred ms to the load frames (the old fade-in from black was over before it was drawn). Scene fades go through `SceneLoader.FadeToBlack` / `FadeFromBlack`, which holds black until 3 normal frames in a row (`LoadScene` loads a frame late, so one calm frame proves nothing). Measure fades with a per-frame alpha + frame-time trace (coroutine on `DebugCheats`), not by eye.
- **Adding an `EGameState`:** audit every `GameStateChangedEvent` subscriber. `LevelManager` throws on unknown states and only skips the level re-spawn for the states it lists; `TimerManager` must only resume a timer that was actually running (frozen / tutorial-paused / level not started).
- **`AdManager` bootstraps itself** (`RuntimeInitializeOnLoadMethod`) — no scene contains it. Never assume a manager exists because a script references it; grep scenes/prefabs for its script GUID.
- **Monetization is optional at compile time.** `Scripts/Runtime/Monetization` is its own assembly; the `LevelPlay` and `UnityIAP` sub-assemblies compile only when their package is installed (asmdef `versionDefines`) and register in `MonetizationServices`. Never reference SDK types (`Unity.Services.LevelPlay`, `UnityEngine.Purchasing`) from Assembly-CSharp, or the template stops compiling for buyers without the SDKs. LevelPlay registers only on Android/iOS targets; on Standalone the Editor uses `MockAdService`.
- **Tray checks must not depend on which slide finishes first.** Merge / tray-full checks skip items still `IsMovingToSpot`, and equal-length slides complete in the same frame in creation order. `ItemSpotManager.Arrived` therefore runs `MatchSystem.CheckSettledTray()` when the last mover settles; without it a middle insert into the last free slot froze the game (tray full, no Game Over, taps ignored, timer running). Reproduce tray bugs with a fast random-tap loop that flags "nothing moving + (tray full or 3 of a kind) for 1 s".
- **Store purchases: grant + save before confirm.** `ShopManager.GrantStorePurchase` is the only place store orders are granted; it records the transaction id (re-delivered orders are skipped) and returns `SaveManager.TryFlush()`. Never grant inside the `TryPurchase` callback. To test a purchase in the Editor without clicking the fake store's IMGUI dialog, add the `IAP_FAKE_STORE_DEFAULT` define temporarily (auto-approve), then remove it.
- **Capturing Play-mode checks:** `ScreenCapture.CaptureScreenshot` silently writes nothing unless the Game view is the visible tab, and `EditorApplication.update` timers can stall while Unity is unfocused. If Play mode sits on frame 2 (`Time.time` stuck at 0.02), set `Application.runInBackground = true` from `RunCommand` — in the Editor this **writes `PlayerSettings.runInBackground`** to ProjectSettings.asset, so set `PlayerSettings.runInBackground = false` again after the test. **Never call `AssetDatabase.SaveAssets()` while in Play mode — including in the same `RunCommand` that sets `EditorApplication.isPlaying = false`** (Play mode only ends later that frame). It writes the Play-mode copies of ProjectSettings: `runInBackground: 1`, and QualitySettings stripped to the current platform's levels (the Mobile level vanished twice this way). Exit Play mode in one command, save in the next; `git diff ProjectSettings` after any Play-mode test. Time captures with a coroutine on a scene object (`VfxPool.Instance.StartCoroutine`), render `Camera.main` into a RenderTexture for images, and prefer numeric checks (e.g. barrel-vs-laser angle). `RunCommand` scripts can't reference PrimeTween. Make sure the level isn't OUTOFTIME (60 s levels) before tapping.
- **State panels must be saved inactive.** `WinPanelManager.OnEnable` grants coins and saves level progress, so a `Level Complete` panel left active in MainScene paid out on every level load. After editing a panel by hand, set it inactive again (`UIManager` turns it on).
- **Currency grants publish `CurrencyGrantedEvent`** right after `SaveManager.AddCoins/AddGems`. `CurrencyFlyEffect` on the pill prefabs plays the coin flight and counts up from it. A new grant site that skips the event just snaps the counter. **Never publish an event from a panel's `OnEnable` to its own children.** Unity enables the parent before the children, so they haven't subscribed yet (the win screen's CoinPill missed its coins this way). `WinPanelManager` grants in `OnEnable` and publishes in `LateUpdate`.
- **Daily reward: the saved date is the last *claim* date**, written in `DailyRewardManager.GrantReward`. The Lobby's Daily Rewards button opens the panel with Claim disabled once today's reward is claimed.
- **Power-ups are data-driven.** `PowerupManager` spawns the tray from `PowerupDatabase` at runtime (`trayRoot` = `Powerup Zone`, slots sorted by `order`); MainScene has no power-up spots. Add/edit/reorder in `Match Them All ▸ Power-up Manager`. Built-ins use `Slot_*` prefabs (prop baked in); custom ones use `PowerupSlot` (`trayModel` or `icon`). `unlockLevel` is a 0-based index (players see index + 1; the window shows player numbers). A slot's `Powerup` must reference the badge/label/counter under its *own* slot (Fan once pointed at Vacuum's badge). Activation sounds live on the `PowerupDataSO` (clip/volume/pitch); `SoundEvent` 15–18 are retired. Effects that sync to a slot animation override `WaitsForSlotAnimation`. Never key behaviour on `so.id` strings. Test locks with a high unlock level in Play mode, then restore it. `modelOffset` / `modelRotation` (window: Tray card) are applied once in `Powerup.Configure` — rotation turns the prop / tray model / icon around its own centre on its own axes, offset is in slot space; effects that move the prop (Freeze aim, Fan shake, Spring squash) start from that adjusted pose. While locked, `Powerup.UpdateVisuals` hides the number, video icon and `counterBadge` (the `Amount Container/Container` badge), shows `Lock Badge` + `Level Label`, and darkens the model through a per-renderer `MaterialPropertyBlock` (`_BaseColor`) / sprite colour — never by editing the shared `M_Palette`. Lock badges sit at the shared `Amount Container` position; don't override it per slot (Slot_Vacuum once carried y -3.75 from the old scene spot). Power-up taps are ignored until `LevelManager.IsLevelReady` — the state is already GAME while the level spawns, and `LevelManager.Items` throws before it exists.
- **Don't save a script while Play mode is running** in a test: Unity recompiles in Play mode, every static (`*.Instance`) resets, and the managers' `Update`s throw NREs that are artifacts, not bugs. Exit Play mode, edit, recompile, re-enter.
- **Power-up starting charges are seeded per id.** `SaveManager.InitializePowerups` gives `defaultAmount` to any power-up the save has no entry for (first launch *and* power-ups added later); the entry (even 0) marks it as seeded, so nothing is granted twice. `hasInitializedPowerups` is only kept for save compatibility. Renaming a power-up's id therefore re-grants its starting charges.
- **Effect objects keep private fields across Play sessions.** `PowerupEffect` instances live inside the `PowerupDataSO` asset, and Unity's domain-reload backup restores their private fields — `FanEffect._nextAllowedTime` (a `Time.time` cooldown) once carried over and blocked Fan taps until `Time.time` caught up. Runtime-only state on an effect must be `[NonSerialized]` (Fan, Freeze and Spring now are).
- In Unity MCP `RunCommand` scripts, `Image` resolves to the AI Assistant's `Unity.AI.Image` namespace — alias it: `using UiImage = UnityEngine.UI.Image;`.
- Power-up VFX are generated (`Tools ▸ AssetGen ▸ Generate Powerup FX`) and the `Powerup_*.asset` / `FreezeOverlay` references point into them. Regenerating keeps the prefab GUIDs and internal IDs (checked), but use *Generate Powerup FX*, not *Generate VFX*, unless you also mean to rebuild `FX_MergeBurst`.
- **No leftover objects after a model swap.** When replacing a model, delete the old model's empty children (e.g. `Cylinder.074` under the freeze blaster) unless something references them — and if a point is needed (gun tip), name it for its job (`Muzzle`). Grep for references before deleting.
- **Layering: overlay UI always draws over 3D.** Anything full-screen that must sit *behind* some 3D objects (e.g. the freeze frost behind the ice spheres and tray) needs its own Screen Space - Camera canvas with a `planeDistance` between them (`FreezeOverlay`: 10.6). Animated tray props must move toward the camera (tray-local -Z) to clear the item spots (camera-local z 4) — see `FreezeEffect.shootLift`.
- **Button Color Tint `Normal Color` must stay white.** The tint multiplies the Image only in Play mode, so a coloured Normal Color looks fine in the Editor and dark at runtime (Lobby `SettingsButton` was purple). Colour the sprite/Image, not the ColorBlock.
- Don't use `??` / `?.` on `UnityEngine.Object`s (`GetComponent<T>() ?? AddComponent<T>()` never adds — Unity's fake null isn't C# null). Use `TryGetComponent`.
- **Asset references belong on the prefab, scene references on the instance.** After `SaveAsPrefabAssetAndConnect`, references you set on the scene object are only instance overrides — new copies of the prefab come without them. Set asset references (other prefabs, sprites, VFX) on the prefab asset itself (`FreezeOverlay` once shipped with null ice-sphere/shatter refs this way); only scene objects (e.g. the HUD bar) stay as instance overrides.
- **Repeated scene objects must be prefab instances.** When the same object goes into several places (e.g. one `Slot Background` per power-up spot), save it once as a prefab (`PrefabUtility.SaveAsPrefabAsset`) and place instances (`PrefabUtility.InstantiatePrefab`) — never `new GameObject` copies per place. Copies drift and must be edited one by one. To turn existing copies into instances, use `PrefabUtility.ConvertToPrefabInstance` (keeps the objects, so onClick and other references survive); child names must match (`ObjectMatchMode.ByHierarchy`). Editor builders that generate repeated objects (`UIOverhaulBuilder.Bubbles`) must instantiate the prefab too.
- **The user edits scenes by hand — never run a scene builder blind.** `Tools ▸ Match Them All ▸ UI Overhaul` hardcodes positions and overwrites hand edits. Before running one (even as a test), snapshot the scene's RectTransforms, run it, diff, and restore anything the user changed (then sync the builder's values to theirs).

---

## 8. Community Map (key communities)

| Community | Key Class(es) | What it owns |
|-----------|--------------|--------------|
| Level Editor Tools | `LevelEditorWindow` | Level CRUD, editor UI |
| Item Manager Editor | `ItemManagerWindow` | Item config, 3D preview |
| Item Spot Management | `ItemSpotManager` | Spot grid, placement logic |
| Save System & Player Data | `SaveManager`, `PlayerData` | Persistence, player state |
| Core Game Events | `EventBus<T>`, `GameEvents` | All inter-system events |
| Power-Up Types & Settings | `PowerupManager`, `GameSettingsSO` | Power-up logic + settings SO |
| Merge System | `MergeManager` | Match-3 merge, VFX |
| Input Handling | `InputManager`, `MTAInputSystem_Actions` | Input routing, raycasting |
| Goal Management | `GoalManager`, `GoalCard` | Goal tracking + UI |
| Timer Management | `TimerManager` | Game-state-aware countdown |
| Item Behavior | `Item` | Core item physics + state |
| Item Placement | `ItemPlacer` | Spawn, preview, placement |
| Audio Manager | `SoundManager`, `SoundLibrarySO` | Audio playback |
| Game Manager | `GameManager` | Game state machine |
| Level Data Management | `LevelDataSO` | Level config (ScriptableObject) |
| Pixelate Render Feature | `PixelizeFeature`, `PixelizePass` | Custom URP render pass |
| UI Manager | `UIManager` | Panel switching via GameState |
| Tutorial Steps | `TutorialManager` | Step-based tutorial logic |
| Combo System | `ComboManager` | Combo tracking + multiplier |
| Hint System | `HintManager` | Item highlight hints |
| Shop & Gems | `ShopManager`, `ShopProductCard` | IAP-style shop, gem economy |

> Full list: see `graphify-out/GRAPH_REPORT.md` → Community Hubs section

---

## 9. Cross-Community Bridge Nodes (high betweenness — careful!)

These nodes connect otherwise separate communities. Breaking them breaks multiple systems:
- `ItemSpotManager` — bridges Item Spot Management ↔ Lose Panel (betweenness: 0.050)
- `PowerupManager` — bridges Power-Ups ↔ Lose Panel (betweenness: 0.037)
- `TutorialManager` — bridges Core Game Events ↔ Lose Panel (betweenness: 0.037)

---

## 10. Obsidian Second Brain

The vault root is the project root (where `.obsidian/` lives). In Obsidian:
- `graphify-out/GRAPH_REPORT.md` — clickable `[[community links]]` for navigation
- `graphify-out/graph.html` — interactive visual graph explorer
- `docs/` — architecture decision records, specs, plans

When you make an architectural decision, log it in Obsidian under `docs/decisions/`.

---

## 11. Unity Plugin Skills

Only Unity's official tooling is installed: the `unity@unity-agent-plugin` plugin (skills such as `unity:ui-ugui`, `unity:urp-postprocessing`) and the official `unity-mcp` server (relay from `com.unity.ai.assistant`).

**Use Unity MCP tools BEFORE writing custom code** — many common tasks are already covered:

| Task | Tool (`mcp__unity-mcp__…`) |
|------|---------------|
| GameObjects | `Unity_ManageGameObject` |
| Scenes | `Unity_ManageScene` |
| Assets / prefabs | `Unity_ManageAsset`, `Unity_FindProjectAssets` |
| Scripts | `Unity_CreateScript`, `Unity_ScriptApplyEdits`, `Unity_ValidateScript` |
| Packages | `Unity_PackageManager_GetData`, `Unity_PackageManager_ExecuteAction` |
| Profiler | `Unity_Profiler_*` |
| Screenshots | `Unity_Camera_Capture`, `Unity_SceneView_Capture2DScene` |
| Console logs | `Unity_ReadConsole`, `Unity_GetConsoleLogs` |
| Editor state / play mode | `Unity_ManageEditor` |
| Anything else (incl. API reflection) | `Unity_RunCommand` |

**Quick workflow:**
1. Check editor state: `Unity_ManageEditor`
2. Find existing assets/objects via the tools above
3. Only write custom code when no existing tool covers it

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

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, clarifying questions come before implementation rather than after mistakes, and no broken Inspector references or prefab overrides from silent renames.
