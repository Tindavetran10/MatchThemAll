# 👋 Start Here — Match Them All Customization Guide

This folder is the **single entry point** for customizing the game without touching engine code.

---

## 📂 What's in this folder

| Folder | Contents | Purpose |
|---|---|---|
| `Levels/` | `LevelData01.asset` … | All level configurations (items, duration, goals, tutorials) |
| `Items/` | `Apple.prefab` … | All item 3D prefabs that can appear in levels |

---

## 🛠 How to create or edit a level

1. Open the **Template Editor** via the Unity menu bar:
   `Match Them All → Template Editor`
2. Click **+ New** to create a level, or select an existing one from the list.
3. Configure the level settings (duration, items, goals, tutorial steps).
4. Click **👁 Preview Layout** to instantly see the item layout in the Scene View.
5. Click **▶ Play Level** to test the level in Play Mode immediately.
6. Click **💾 Save** when done.

---

## 🎮 How to add a new item type

1. Create a new 3D prefab in `_START_HERE/Items/` with an `Item` component attached.
2. Add the new item's name to the `EItemName` enum in `Scripts/Enums/`.
3. The item will automatically appear in the Template Editor's item dropdown.

---

## 📐 Project Architecture (brief)

For developers who want to go deeper, the project is organized by **feature**, not by file type:

**Feature Folders** (scripts + assets together):
- **`Level System/Scripts/`** — `LevelManager`, `LevelDataSO`, `Level`, `ItemLevelData`, `ItemPoolManager`
- **`Level System/Prefabs/`** — `LevelTemplate.prefab` (the physical scene layout)
- **`UI/Scripts/`** — All 17 UI panel controllers (menus, win/lose, settings, daily reward…)
- **`UI/Prefabs/`** — All UI prefabs (panels, buttons, floating text)
- **`Power Ups/Scripts/`** — `PowerupManager` + individual power-up scripts (Fan, FreezeGun, Spring, Vacuum)
- **`Power Ups/Prefabs/`** — Power-up prefabs

**Scripts-Only Folders** (no prefabs needed):
- **`Scripts/Core/`** — Infrastructure: `GameManager`, `InputManager`, `SoundManager`, `SceneLoader`, `EventBus`, `GameEvents`, `DailyRewardManager`, `SoundDataSO`
- **`Scripts/Gameplay/`** — Core game loop: `Item`, `ItemPlacer`, `ItemSpot`, `ItemSpotLayout`, `GoalManager`, `HintManager`, `ItemSpotManager`, `MatchSystem`, `MergeManager`, `ComboManager`, `TimerManager`
- **`Scripts/Tutorial/`** — `TutorialManager`, `TutorialStep`
- **`Scripts/Save System/`** — `SaveManager`, `PlayerData`, `SaveManagerBootstrapper`
- **`Scripts/Enums/`** — `EItemName`, `EGameState`
- **`Scripts/Editor/`** — `LevelEditorWindow`, `HierarchySectionHeader`
- **`Prefabs/Gameplay/`** — Shared gameplay prefabs (`Item Spot.prefab`)

> **Tip:** The game communicates entirely through an `EventBus`. If you add a new system,
> subscribe to existing events in `Scripts/Core/GameEvents.cs` rather than adding direct references.
