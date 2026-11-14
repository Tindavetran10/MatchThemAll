# Architecture Overview

How the systems of **Match Them All** fit together. Exact signatures are in
[API_REFERENCE.md](API_REFERENCE.md).

---

## 1. Systems talk through the EventBus

Managers don't hold references to unrelated managers. One system publishes a `struct` event, and any
number of others react to it:

```csharp
// Subscribe in Awake/OnEnable, unsubscribe in OnDestroy/OnDisable.
EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

// Publish
EventBus.Publish(new ItemClickedEvent(item));
```

All events are in `Scripts/Runtime/Core/GameEvents.cs`.

### What happens on a tap

```
InputManager ──ItemClickedEvent──▶ ItemSpotManager   (tray full? ignore. Otherwise insert next to its kind)
                                        │ slide tween ends
                                        ▼
                            ItemReachedSpotEvent ──▶ MatchSystem
                                                      ├─ 3 alike ─▶ MergeStartedEvent ─▶ MergeManager (animation)
                                                      │                                  GoalManager (goal cards)
                                                      │                                  ComboManager, TutorialManager
                                                      └─ tray full ─▶ SpotFilledEvent ─▶ GameManager → GAMEOVER
```

The tray result never depends on which slide finishes first. When the last moving item settles,
`ItemSpotManager` runs `MatchSystem.CheckSettledTray()` again.

### Game state

`GameManager.SetGameState` publishes `GameStateChangedEvent`. Most managers (UI panels, timer, input, level,
power-ups) switch their behaviour on `EGameState`: `MENU`, `GAME`, `LEVELCOMPLETE`, `GAMEOVER`, `PAUSED`,
`OUTOFTIME`, `POWERUPOFFER`.

---

## 2. Managers and their lifetimes

| System | Lives in | Role |
|---|---|---|
| `SaveManager` | static class, started by `SaveManagerBootstrapper` (`RuntimeInitializeOnLoadMethod`) | JSON save (`save.json`): currency, power-up charges, level stars, purchases, settings |
| `AdManager` | creates itself on startup, `DontDestroyOnLoad` | rewarded and interstitial ads, consent and tracking prompts |
| `GameManager` | MainScene | game-state machine, win/lose/next/retry |
| `LevelManager` | MainScene | loads levels from Addressables (label `LevelData`) and spawns the current one |
| `ItemSpotManager` + `MatchSystem` | MainScene | the tray: placement, merges, full-tray check |
| `MergeManager` | MainScene | merge animation and VFX |
| `GoalManager` | MainScene | goal cards |
| `TimerManager` | MainScene | countdown, freeze, add time |
| `PowerupManager` | MainScene | power-up bar, charges, activation through `PowerupEffect` |
| `TutorialManager`, `HintManager`, `ComboManager` | MainScene | tutorial steps, idle hints, combos |
| `ShopManager` | Lobby | product validation, coin purchases, store purchases |
| `SoundManager` | creates itself from `Resources/Sound/SoundManager.prefab` if no scene has one, `DontDestroyOnLoad` | sound effects and music via `SoundLibrarySO` and the audio mixer |
| `LevelMapManager` | LevelSelect | the saga map |

Never assume a manager exists because a script references it. Check the scene, or use the `Instance` null
check where a manager is optional.

---

## 3. Data is in ScriptableObjects

| Asset | Folder |
|---|---|
| `LevelDataSO` | `_START_HERE/Levels` (Addressable, label `LevelData`) |
| Item prefabs (`Item` component) | `_START_HERE/Items` |
| `PowerupDatabaseSO` + `PowerupDataSO` | `Resources/Powerups` |
| `ShopDatabaseSO` + tabs + products | `Resources/Shop` |
| `SoundLibrarySO` | `Resources/Sound` |
| `GameSettingsSO`, `MonetizationSettingsSO` | `Resources`, `Resources/Monetization` |

Power-up behaviour is polymorphic: `PowerupDataSO.effect` is a `[SerializeReference] PowerupEffect`.
Effects get what they need through `PowerupContext` instead of reaching into singletons.

---

## 4. Monetization is optional at compile time

`Scripts/Runtime/Monetization` is its own assembly with two contracts, `IAdService` and `IIapService`. The
LevelPlay and Unity IAP adapters are separate assemblies that compile only when their package is installed
(asmdef `versionDefines`). They register themselves in `MonetizationServices`. Without the packages, the game
uses `MockAdService` and a null IAP service. Game code never references an SDK type, so the template
compiles with or without the SDKs.

---

## 5. Performance

Hot paths use **ZLinq** (`AsValueEnumerable()`) instead of LINQ to avoid allocations:

```csharp
using ZLinq;

var free = spots.AsValueEnumerable().Take(activeCount).FirstOrDefault(s => s.IsEmpty());
```

Avoid allocations in `Update()` and other per-frame code.

---

## 6. Namespaces and folders

- Runtime code: `MatchThemAll.Scripts` and sub-namespaces (`.Power_Ups`, `.Shop`, `.SaveSystem`, `.UI`,
  `.Utilities`, `.Monetization`, `.Tutorial`), in `Scripts/Runtime/`.
- Editor tools: `MatchThemAll.Scripts.Editor`, in `Scripts/Editor/`. Never reference them from runtime code.
