# Architecture Overview

This document describes the architectural patterns, communication flows, and core systems powering the **Match Them All** template.

---

## 1. Decoupled Communication via EventBus
The game avoids direct tight coupling between GameObjects and Managers by using a generic, strongly typed **EventBus**:

```csharp
// Subscribing to an event
EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

// Raising an event
EventBus<ItemClickedEvent>.Raise(new ItemClickedEvent(clickedItem));

// Unsubscribing (always in OnDestroy/OnDisable)
EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
```

### Key Events
- `GameStateChangedEvent`: Broadcasts state transitions (`Lobby`, `Playing`, `Paused`, `Win`, `Lose`).
- `ItemClickedEvent`: Dispatched when the player taps/selects an item in the play area.
- `ItemReachedSpotEvent`: Dispatched when an item finishes moving into a dock/spot.
- `SpotFilledEvent`: Dispatched when all spots or an individual spot is filled.
- `MergeStartedEvent`: Dispatched when 3 matching items start merging.

---

## 2. Core Managers & Lifecycles

| System | Role |
|---|---|
| **GameManager** | Controls high-level game states, win/loss evaluation, and level transitions. |
| **SaveManager** | Bootstrapped automatically via `[RuntimeInitializeOnLoadMethod]`. Handles JSON serialization of `PlayerData` (`save.json`). |
| **ItemSpotManager** | Manages the docking slots at the bottom of the screen and monitors match-3 combinations. |
| **MergeManager** | Animates and resolves match-3 merging sequence, score calculation, and VFX playback. |
| **GoalManager** | Tracks active level objectives (e.g., collect N diamonds) and updates UI cards. |
| **PowerupManager** | Manages power-up activation, cooldowns, and interactions (Fan, Freeze, Spring, Vacuum). |
| **ShopManager** | Handles in-game economy, purchasing logic, and product validation. |
| **LevelMapManager** | Powers the saga level selection map and dynamic level node creation. |

---

## 3. High Performance with ZLinq
In performance-sensitive hot paths and per-frame evaluations, the codebase uses **ZLinq** (`AsValueEnumerable()`) to achieve zero heap allocations during LINQ-style queries:

```csharp
using ZLinq;

// Zero-allocation filtering and aggregation:
var matches = activeItems.AsValueEnumerable()
                         .Where(x => x.ItemNameKey == targetType)
                         .ToArray();
```

---

## 4. Asset Organization & Namespaces
- Runtime code lives under the `MatchThemAll.Scripts` namespace.
- Editor utilities live under `Match_Them_All.Scripts.Editor` or `MatchThemAll.Editor`.
- All user-customizable starter assets reside in `_START_HERE/` for immediate access.
