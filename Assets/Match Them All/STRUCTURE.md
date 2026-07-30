# Project Structure — Match Them All (Template)

A quick map for template users. The project is organized so **game code** and **editor tooling** are clearly separated, and each kind of asset has one home.

## Code — `Scripts/`

All C# lives under one `Scripts/` root, split into two halves:

```
Scripts/
├── Runtime/        ← GAME code (ships in the build)
│   ├── Core/          game state machine, scene loading, events, input, audio, ads
│   ├── Gameplay/      items, item spots, goals, timer, merge, hints, combos
│   ├── PowerUps/      power-up data, effects, manager
│   ├── Shop/          shop products, tabs, entitlements, IAP seam, UI
│   ├── LevelSystem/   level data (LevelDataSO), level manager, pooling
│   ├── SaveSystem/    SaveManager + PlayerData (persistence)
│   ├── UI/            gameplay/meta UI panels (Manager/ subfolder for screen managers)
│   ├── Tutorial/      step-based tutorial
│   ├── Pixelate/      URP render feature
│   └── Utilities/     extensions, settings, enums
│
└── Editor/        ← TOOLING (editor only, NOT in the build)
    ├── LevelEditorWindow   Match Them All → Template Editor (levels + items + settings)
    ├── ItemManagerWindow   item configuration + 3D preview
    ├── ShopEditorWindow    Match Them All → Shop Manager (products, tabs, rewards)
    ├── ShopSetup           Tools → Shop → Create Default Products / Build Shop Panel
    ├── LevelMapBuilder     Tools → Levels → Build Saga Map
    ├── PowerupDatabaseSetup
    ├── PrefabConsolidator  Tools → Project → Consolidate Prefabs
    └── HierarchySectionHeader, ItemReferenceOps
```

**Rule of thumb:** anything in `Runtime/` runs in the built game; anything in `Editor/` is dev tooling (menu items, inspector windows). A template user adding gameplay should put scripts in `Runtime/<feature>/`; editor extensions go in `Editor/`.

> Note: there are **no `.asmdef` files** — `Runtime/` compiles into `Assembly-CSharp` and `Editor/` into `Assembly-CSharp-Editor` (Unity's convention for the special `Editor` folder name). If you later add asmdefs, keep that Runtime/Editor boundary.

## Namespaces

Namespaces are `MatchThemAll.Scripts.*` (one root — there used to be a second `Match_Them_All.*` root; it was unified). Most files use `MatchThemAll.Scripts` or a sub-namespace matching their feature (`.Shop`, `.SaveSystem`, `.Power_Ups`, etc.). The folder a script lives in is a strong hint of its namespace, though a few folders contain more than one namespace — when in doubt, check the top of the file.

## Prefabs — `Prefabs/`

All prefabs live under one `Prefabs/` root, grouped by feature:

```
Prefabs/
├── Gameplay/     in-world gameplay prefabs (e.g. Item Spot)
├── UI/           panels, cards, labels
│   ├── Level/      saga-map nodes/buttons/labels
│   ├── Power Up/   power-up 3D-model UI overlays
│   └── Shop/       shop product cards, tab buttons
├── PowerUps/     power-up 3D models (e.g. Vacuum)
└── Levels/       level templates
```

## Data & assets

| Folder | Contents |
|---|---|
| `Resources/` | Runtime-loaded assets: `GameSettings`, `Powerups/PowerupDatabase` (+ per-power-up SOs), `Shop/ShopDatabase` (+ products/tabs). **Only assets referenced via `Resources.Load` belong here.** |
| `Settings/` | `GameSettingsSO` and related tuning SOs |
| `_START_HERE/` | Template starter content: sample `Levels/`, sample item `Items/` prefabs. A template user edits/replaces these. |
| `Scenes/` | `MainMenu`, `LevelSelect` (saga map), `LoadingScene`, `MainScene` (gameplay) |
| `Audio/`, `Sprites/`, `Models/`, `Material/`, `Animation/`, `Shader Graph/` | art/audio assets |

## Where things are authored (designer entry points)

- **Levels & items & settings:** `Match Them All → Template Editor`
- **Shop products/tabs/rewards:** `Match Them All → Shop Manager`
- **Power-ups:** create `PowerupDataSO` assets under `Resources/Powerups/` and add to `PowerupDatabase.asset`
- **One-click builders (Tools menu):** Shop, Levels (saga map), Project (prefab consolidation)

## Save data

Player progress is in `Application.persistentDataPath/save.json`, read/written **only** through `SaveManager` (static API). Never mutate `PlayerData` fields directly — go through `SaveManager.Get*/Add*/Spend*`. Progress is keyed by stable ids (level address, power-up id, entitlement key), so adding/removing content won't corrupt a player's save.
