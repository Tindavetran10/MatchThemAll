# MatchThemAll — Claude Code Context

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

---

## God Nodes (most connected — touch with extreme care)

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

## Architecture Rules

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
- `Match_Them_All.Scripts.Editor` namespace: editor tools only (never reference from runtime)

### Save System
- `SaveManager` is bootstrapped via `RuntimeInitializeOnLoadMethod` in `SaveManagerBootstrapper`
- Access via `SaveManager.Instance` — do **not** drag it into the Inspector
- `PlayerData` is the serialized model — modify only through `SaveManager`

---

## Community Map (75 communities — key ones listed)

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
| Audio Manager | `SoundManager`, `SoundDataSO` | Audio playback |
| Game Manager | `GameManager` | Game state machine |
| Level Data Management | `LevelDataSO` | Level config (ScriptableObject) |
| Pixelate Render Feature | `PixelizeFeature`, `PixelizePass` | Custom URP render pass |
| UI Manager | `UIManager` | Panel switching via GameState |
| Tutorial Steps | `TutorialManager` | Step-based tutorial logic |
| Combo System | `ComboManager` | Combo tracking + multiplier |
| Hint System | `HintManager` | Item highlight hints |

> Full list: see `graphify-out/GRAPH_REPORT.md` → Community Hubs section

---

## Cross-Community Bridge Nodes (high betweenness — careful!)

These nodes connect otherwise separate communities. Breaking them breaks multiple systems:
- `ItemSpotManager` — bridges Item Spot Management ↔ Lose Panel (betweenness: 0.050)
- `PowerupManager` — bridges Power-Ups ↔ Lose Panel (betweenness: 0.037)
- `TutorialManager` — bridges Core Game Events ↔ Lose Panel (betweenness: 0.037)

---

## Obsidian Second Brain

The vault root is this folder. In Obsidian:
- `graphify-out/GRAPH_REPORT.md` — clickable `[[community links]]` for navigation
- `graphify-out/graph.html` — interactive visual graph explorer
- `docs/` — architecture decision records, specs, plans

When you make an architectural decision, log it in Obsidian under `docs/decisions/`.

---

## Quick Commands

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
