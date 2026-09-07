# Graph Report - MatchThemAllTemplate  (2026-09-07)

## Corpus Check
- 103 files · ~168,766 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1397 nodes · 1903 edges · 96 communities (90 shown, 6 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0d3303c0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 39|Community 39]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 61|Community 61]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 63|Community 63]]
- [[_COMMUNITY_Community 64|Community 64]]
- [[_COMMUNITY_Community 65|Community 65]]
- [[_COMMUNITY_Community 66|Community 66]]
- [[_COMMUNITY_Community 67|Community 67]]
- [[_COMMUNITY_Community 68|Community 68]]
- [[_COMMUNITY_Community 69|Community 69]]
- [[_COMMUNITY_Community 70|Community 70]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]
- [[_COMMUNITY_Community 80|Community 80]]
- [[_COMMUNITY_Community 81|Community 81]]
- [[_COMMUNITY_Community 82|Community 82]]
- [[_COMMUNITY_Community 83|Community 83]]
- [[_COMMUNITY_Community 84|Community 84]]
- [[_COMMUNITY_Community 85|Community 85]]
- [[_COMMUNITY_Community 86|Community 86]]
- [[_COMMUNITY_Community 87|Community 87]]
- [[_COMMUNITY_Community 88|Community 88]]
- [[_COMMUNITY_Community 89|Community 89]]
- [[_COMMUNITY_Community 90|Community 90]]
- [[_COMMUNITY_Community 91|Community 91]]
- [[_COMMUNITY_Community 92|Community 92]]
- [[_COMMUNITY_Community 93|Community 93]]
- [[_COMMUNITY_Community 94|Community 94]]
- [[_COMMUNITY_Community 95|Community 95]]

## God Nodes (most connected - your core abstractions)
1. `LevelEditorWindow` - 49 edges
2. `ItemManagerWindow` - 47 edges
3. `SaveManager` - 38 edges
4. `ShopEditorWindow` - 37 edges
5. `ItemSpotManager` - 34 edges
6. `LevelMapManager` - 29 edges
7. `TutorialManager` - 26 edges
8. `Item` - 25 edges
9. `ShopSetup` - 22 edges
10. `TimerManager` - 20 edges

## Surprising Connections (you probably didn't know these)
- `ItemManagerWindow` --inherits--> `EditorWindow`  [EXTRACTED]
  Scripts/Editor/ItemManagerWindow.cs →   _Bridges community 1 → community 4_
- `LevelEditorWindow` --inherits--> `EditorWindow`  [EXTRACTED]
  Scripts/Editor/LevelEditorWindow.cs →   _Bridges community 4 → community 0_
- `DailyRewardManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Scripts/Runtime/Core/DailyRewardManager.cs →   _Bridges community 55 → community 44_
- `DebugCheats` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Scripts/Runtime/Core/DebugCheats.cs →   _Bridges community 44 → community 43_
- `GameManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Scripts/Runtime/Core/GameManager.cs →   _Bridges community 44 → community 23_

## Import Cycles
- None detected.

## Communities (96 total, 6 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.07
Nodes (21): Editor, DeletedItemRecord, DeletedItemRecordList, LevelEditorWindow, MatchThemAll.Scripts.Editor, RemovedLevelEntry, bool, Color (+13 more)

### Community 1 - "Community 1"
Cohesion: 0.08
Nodes (20): double, ItemManagerWindow, MatchThemAll.Scripts.Editor, PreviewRenderUtility, bool, Color, EItemName, float (+12 more)

### Community 2 - "Community 2"
Cohesion: 0.07
Nodes (13): hapticsEnabled, lastPlayedDate, loginStreak, musicVolume, PlayerData, MatchThemAll.Scripts.SaveSystem, SaveManager, bool (+5 more)

### Community 3 - "Community 3"
Cohesion: 0.10
Nodes (16): ItemSpotManager, MatchThemAll.Scripts, Action, Ease, EItemName, float, IEnumerable, int (+8 more)

### Community 4 - "Community 4"
Cohesion: 0.09
Nodes (16): MatchThemAll.Scripts.Editor, ShopEditorWindow, EditorWindow, bool, Color, float, GUIStyle, int (+8 more)

### Community 5 - "Community 5"
Cohesion: 0.07
Nodes (22): PowerupEffect, FanEffect, MatchThemAll.Scripts.Power_Ups, FreezeEffect, MatchThemAll.Scripts.Power_Ups, DrawGizmos(), DrawTrajectoryGizmo(), MatchThemAll.Scripts.Power_Ups (+14 more)

### Community 6 - "Community 6"
Cohesion: 0.12
Nodes (15): MatchThemAll.Scripts.Editor, ShopSetup, LayoutElement, Button, Color, ECurrency, EKind, GameObject (+7 more)

### Community 7 - "Community 7"
Cohesion: 0.09
Nodes (15): LevelMapPath, LevelMapManager, MatchThemAll.Scripts.UI, Button, float, IEnumerator, IReadOnlyList, LevelMapNode (+7 more)

### Community 8 - "Community 8"
Cohesion: 0.11
Nodes (15): Coroutine, CanvasGroup, GameObject, IEnumerator, int, ItemClickedEvent, Level, List (+7 more)

### Community 9 - "Community 9"
Cohesion: 0.07
Nodes (29): AdManagerMock, API Reference, ComboManager, Configuration, Core Systems, Event System, EventBus<T>, Game Events (+21 more)

### Community 10 - "Community 10"
Cohesion: 0.12
Nodes (18): CallbackContext, AddCallbacks(), Disable(), Enable(), Get(), IGameplayActions, @MTAInputSystem_Actions, RemoveCallbacks() (+10 more)

### Community 11 - "Community 11"
Cohesion: 0.11
Nodes (18): DebugActivateFan(), DebugActivateFreeze(), DebugActivateSpring(), DebugActivateVacuum(), ForceActivate(), MatchThemAll.Scripts, PowerupManager, bool (+10 more)

### Community 12 - "Community 12"
Cohesion: 0.09
Nodes (13): Bounds, Collider, Item, MatchThemAll.Scripts, Renderer, Rigidbody, bool, EItemName (+5 more)

### Community 13 - "Community 13"
Cohesion: 0.14
Nodes (10): DebugRunOutOfTime(), MatchThemAll.Scripts, TimerManager, bool, Button, GameStateChangedEvent, IEnumerator, Level (+2 more)

### Community 14 - "Community 14"
Cohesion: 0.16
Nodes (12): BoxCollider, ItemPlacer, MatchThemAll.Scripts, PreviewSpawn(), PreviewSpawnFromEditor(), PreviewSpawnWithData(), Button, Item (+4 more)

### Community 15 - "Community 15"
Cohesion: 0.13
Nodes (9): Camera, InputManager, MatchThemAll.Scripts, LayerMask, MTAInputSystem_Actions, bool, GameStateChangedEvent, Item (+1 more)

### Community 16 - "Community 16"
Cohesion: 0.11
Nodes (12): IReadOnlyList<string>, LevelManager, MatchThemAll.Scripts, List<string>, Action, bool, Button, GameStateChangedEvent (+4 more)

### Community 17 - "Community 17"
Cohesion: 0.13
Nodes (9): GoalManager, MatchThemAll.Scripts, GoalCard, int, Item, ItemLevelData, Level, List (+1 more)

### Community 18 - "Community 18"
Cohesion: 0.14
Nodes (11): MatchThemAll.Scripts, MergeManager, Ease, float, IEnumerator, int, IObjectPool, Item (+3 more)

### Community 19 - "Community 19"
Cohesion: 0.13
Nodes (10): Button, GameObject, List, ShopDatabaseSO, ShopProductCard, string, Transform, MatchThemAll.Scripts.Shop (+2 more)

### Community 20 - "Community 20"
Cohesion: 0.19
Nodes (9): Axis, ContextMenu, ItemSpotLayout, MatchThemAll.Scripts, LayoutMode, bool, float, List (+1 more)

### Community 21 - "Community 21"
Cohesion: 0.19
Nodes (12): ItemReferenceOps, MatchThemAll.Scripts.Editor, entry, IList, index, levelName, Item, ItemLevelData (+4 more)

### Community 22 - "Community 22"
Cohesion: 0.16
Nodes (7): AudioMixer, AudioSource, MatchThemAll.Scripts, SoundManager, int, string, SoundDataSO

### Community 23 - "Community 23"
Cohesion: 0.16
Nodes (4): GameManager, MatchThemAll.Scripts, EGameState, SpotFilledEvent

### Community 24 - "Community 24"
Cohesion: 0.13
Nodes (8): ContinuePanelManager, MatchThemAll.Scripts.UI, Button, float, GameObject, GameSettingsSO, GameStateChangedEvent, TextMeshProUGUI

### Community 25 - "Community 25"
Cohesion: 0.14
Nodes (10): LevelProgressEntry, MatchThemAll.Scripts.SaveSystem, PlayerData, PowerupSaveEntry, bool, float, int, IReadOnlyList (+2 more)

### Community 26 - "Community 26"
Cohesion: 0.13
Nodes (10): MatchThemAll.Scripts.Power_Ups, Powerup, Action, Animator, GameObject, Image, PowerupDatabaseSO, PowerupDataSO (+2 more)

### Community 27 - "Community 27"
Cohesion: 0.17
Nodes (8): Button, GameObject, Image, ShopProductSO, TextMeshProUGUI, MatchThemAll.Scripts.Shop, ShopProductCard, ShopPurchaseSucceededEvent

### Community 28 - "Community 28"
Cohesion: 0.13
Nodes (8): Animator, bool, GameObject, Image, Sprite, TextMeshProUGUI, GoalCard, MatchThemAll.Scripts.UI

### Community 29 - "Community 29"
Cohesion: 0.16
Nodes (7): HintManager, MatchThemAll.Scripts.Managers, EItemName, float, GameStateChangedEvent, ItemClickedEvent, List

### Community 30 - "Community 30"
Cohesion: 0.18
Nodes (8): HashSet, IIapService, Action, IReadOnlyList, ShopProductSO, MatchThemAll.Scripts.Shop, ShopManager, ShopReward

### Community 31 - "Community 31"
Cohesion: 0.16
Nodes (10): AsyncOperation, LoadingScreenManager, MatchThemAll.Scripts, float, GameObject, IEnumerator, Image, Slider (+2 more)

### Community 32 - "Community 32"
Cohesion: 0.20
Nodes (8): LevelMapBuilder, MatchThemAll.Scripts.Editor, GameObject, LevelMapNode, MenuItem, RectTransform, string, Transform

### Community 33 - "Community 33"
Cohesion: 0.16
Nodes (7): ComboManager, MatchThemAll.Scripts, Action, float, int, Level, MergeStartedEvent

### Community 34 - "Community 34"
Cohesion: 0.16
Nodes (8): ItemPlacer, Level, MatchThemAll.Scripts, ItemLevelData, LevelDataSO, List<Item>, Task, TutorialStep>

### Community 35 - "Community 35"
Cohesion: 0.19
Nodes (8): Dictionary, GameObject, IEnumerator, IObjectPool, ParticleSystem, Vector3, MatchThemAll.Scripts, VfxPool

### Community 36 - "Community 36"
Cohesion: 0.18
Nodes (8): Canvas, ObjectPool, Color, FloatingText, RectTransform, Vector3, FloatingTextSpawner, MatchThemAll.Scripts.UI

### Community 37 - "Community 37"
Cohesion: 0.15
Nodes (8): Graphic, float, IEnumerable, List, Vector2, LevelMapPath, MatchThemAll.Scripts.UI, VertexHelper

### Community 38 - "Community 38"
Cohesion: 0.17
Nodes (9): CustomPassSettings, MatchThemAll.Scripts.Pixelate, PixelizeFeature, PixelizePass, RenderingData, RenderPassEvent, ScriptableRenderer, ScriptableRendererFeature (+1 more)

### Community 39 - "Community 39"
Cohesion: 0.15
Nodes (9): Powerup, Fan, MatchThemAll.Scripts.Power_Ups, FreezeGun, MatchThemAll.Scripts.Power_Ups, MatchThemAll.Scripts.Power_Ups, Spring, MatchThemAll.Scripts.Power_Ups (+1 more)

### Community 40 - "Community 40"
Cohesion: 0.18
Nodes (8): Action, Button, CanvasGroup, GameObject, TextMeshProUGUI, Transform, DailyRewardPanel, MatchThemAll.Scripts.UI

### Community 41 - "Community 41"
Cohesion: 0.23
Nodes (7): Action, Color, float, RectTransform, TextMeshProUGUI, FloatingText, MatchThemAll.Scripts.UI

### Community 42 - "Community 42"
Cohesion: 0.17
Nodes (9): ContextContainer, CustomPassSettings, MatchThemAll.Scripts.Pixelate, PassData, PixelizePass, RenderGraph, ScriptableRenderPass, int (+1 more)

### Community 43 - "Community 43"
Cohesion: 0.26
Nodes (5): DebugCheats, MatchThemAll.Scripts, Button, int, RuntimeInitializeOnLoadMethod

### Community 44 - "Community 44"
Cohesion: 0.17
Nodes (7): MatchThemAll.Scripts, OpenSceneLoader, LosePanelManager, MatchThemAll.Scripts.UI, MonoBehaviour, float, IEnumerator

### Community 45 - "Community 45"
Cohesion: 0.17
Nodes (11): Adding a New Item Type, Adding a New Power-Up, Changing Game Colors/Theme, Creating a New Level, Customization Guide, Gem Models, Getting Help, Replacing Placeholder Assets (+3 more)

### Community 46 - "Community 46"
Cohesion: 0.20
Nodes (8): EditorWindowStyles, MatchThemAll.Scripts.Editor, InitializeOnLoadMethod, Color, Dictionary, GUIStyle, int, Texture2D

### Community 47 - "Community 47"
Cohesion: 0.26
Nodes (4): ItemPoolManager, MatchThemAll.Scripts, Dictionary, Item

### Community 48 - "Community 48"
Cohesion: 0.20
Nodes (10): bool, ECurrency, EKind, int, List, Sprite, string, MatchThemAll.Scripts.Shop (+2 more)

### Community 49 - "Community 49"
Cohesion: 0.17
Nodes (8): CanvasGroup, Ease, float, Image, Transform, Vector3, MatchThemAll.Scripts.UI, UIAnimator

### Community 50 - "Community 50"
Cohesion: 0.29
Nodes (4): IIapService, MatchThemAll.Scripts.Shop, NullIapService, Action

### Community 51 - "Community 51"
Cohesion: 0.18
Nodes (8): LevelDataSO, MatchThemAll.Scripts, RewardCalculationMode, int, ItemLevelData, List, Sprite, string

### Community 52 - "Community 52"
Cohesion: 0.18
Nodes (4): MatchThemAll.Scripts.UI, SettingsManager, Slider, Toggle

### Community 53 - "Community 53"
Cohesion: 0.24
Nodes (4): MatchThemAll.Scripts.UI, WinPanelManager, float, GameObject

### Community 54 - "Community 54"
Cohesion: 0.18
Nodes (9): MatchThemAll.Scripts.Power_Ups, PowerupDataSO, ECurrency, GameObject, int, ParticleSystem, PowerupEffect, Sprite (+1 more)

### Community 55 - "Community 55"
Cohesion: 0.29
Nodes (4): DailyRewardManager, MatchThemAll.Scripts.UI, DailyRewardPanel, Transform

### Community 56 - "Community 56"
Cohesion: 0.27
Nodes (5): EventBus, MatchThemAll.Scripts, Action, Dictionary, T

### Community 57 - "Community 57"
Cohesion: 0.20
Nodes (9): ECompletionCondition, EHighlightTarget, bool, EItemName, float, List, string, MatchThemAll.Scripts.Tutorial (+1 more)

### Community 58 - "Community 58"
Cohesion: 0.20
Nodes (5): ItemSpot, MatchThemAll.Scripts, Animator, Item, Transform

### Community 59 - "Community 59"
Cohesion: 0.22
Nodes (6): IEnumerable, List, ShopProductSO, MatchThemAll.Scripts.Shop, ShopDatabaseSO, ShopTabSO

### Community 60 - "Community 60"
Cohesion: 0.20
Nodes (7): bool, Button, GameObject, int, TextMeshProUGUI, LevelButtonUI, MatchThemAll.Scripts.UI

### Community 61 - "Community 61"
Cohesion: 0.25
Nodes (6): MatchThemAll.Scripts.Editor, PowerupDatabaseSetup, MenuItem, PowerupDataSO, PowerupEffect, string

### Community 62 - "Community 62"
Cohesion: 0.31
Nodes (4): MatchThemAll.Scripts.Editor, PrefabConsolidator, MenuItem, string

### Community 63 - "Community 63"
Cohesion: 0.28
Nodes (4): MatchThemAll.Scripts, UIManager, GameObject, GameStateChangedEvent

### Community 64 - "Community 64"
Cohesion: 0.22
Nodes (8): License, Match Them All - Complete Game Template, Next Steps, Package Structure, Quick Start, Requirements, Support, What's Included

### Community 65 - "Community 65"
Cohesion: 0.25
Nodes (3): MainMenuManager, MatchThemAll.Scripts.UI, GameObject

### Community 66 - "Community 66"
Cohesion: 0.29
Nodes (3): TextMeshProUGUI, GemDisplay, MatchThemAll.Scripts.Shop

### Community 67 - "Community 67"
Cohesion: 0.29
Nodes (3): int, MatchThemAll.Scripts.Shop, WatchAdForCoinsButton

### Community 68 - "Community 68"
Cohesion: 0.29
Nodes (3): TextMeshProUGUI, CoinDisplay, MatchThemAll.Scripts.UI

### Community 69 - "Community 69"
Cohesion: 0.29
Nodes (6): AudioClip, AudioMixerGroup, MatchThemAll.Scripts, SoundDataSO, bool, float

### Community 70 - "Community 70"
Cohesion: 0.38
Nodes (3): MatchThemAll.Scripts, SceneLoader, string

### Community 71 - "Community 71"
Cohesion: 0.29
Nodes (3): MatchSystem, MatchThemAll.Scripts, ItemReachedSpotEvent

### Community 72 - "Community 72"
Cohesion: 0.29
Nodes (5): LevelButtonUI, Image, LevelDataSO, LevelMapNode, MatchThemAll.Scripts.UI

### Community 73 - "Community 73"
Cohesion: 0.29
Nodes (3): MatchThemAll.Scripts.Pixelate, PixelizeController, GameStateChangedEvent

### Community 74 - "Community 74"
Cohesion: 0.29
Nodes (5): MatchThemAll.Scripts.Power_Ups, PowerupDatabaseSO, ScriptableObject, List, PowerupDataSO

### Community 75 - "Community 75"
Cohesion: 0.29
Nodes (3): MatchThemAll.Scripts.SaveSystem, SaveManagerBootstrapper, RuntimeInitializeOnLoadMethod

### Community 76 - "Community 76"
Cohesion: 0.29
Nodes (3): FloatingText, ComboVFX, MatchThemAll.Scripts.UI

### Community 77 - "Community 77"
Cohesion: 0.29
Nodes (4): Color, string, FloatingTextTester, MatchThemAll.Scripts.Testing

### Community 78 - "Community 78"
Cohesion: 0.29
Nodes (3): Action, AdManagerMock, MatchThemAll.Scripts.Utilities

### Community 79 - "Community 79"
Cohesion: 0.33
Nodes (5): [1.0.0] - 2026-09-07, Added, Changelog, Features, Technical

### Community 80 - "Community 80"
Cohesion: 0.47
Nodes (3): HierarchySectionHeader, GameObject, Rect

### Community 81 - "Community 81"
Cohesion: 0.40
Nodes (3): MatchThemAll.Scripts.Extensions, TransformExtensions, Transform

### Community 82 - "Community 82"
Cohesion: 0.40
Nodes (3): MatchThemAll.Scripts.Power_Ups, PowerupEffect, PowerupContext

### Community 83 - "Community 83"
Cohesion: 0.33
Nodes (3): int, DebugGrantCurrencyButton, MatchThemAll.Scripts.Shop

### Community 84 - "Community 84"
Cohesion: 0.33
Nodes (5): int, Sprite, string, MatchThemAll.Scripts.Shop, ShopTabSO

### Community 85 - "Community 85"
Cohesion: 0.33
Nodes (3): MatchThemAll.Scripts.Shop, ShopOpener, ShopPanel

### Community 86 - "Community 86"
Cohesion: 0.33
Nodes (5): 🎮 How to add a new item type, 🛠 How to create or edit a level, 📐 Project Architecture (brief), 👋 Start Here — Match Them All Customization Guide, 📂 What's in this folder

### Community 87 - "Community 87"
Cohesion: 0.40
Nodes (4): bool, int, GameSettingsSO, MatchThemAll.Scripts.Settings

### Community 88 - "Community 88"
Cohesion: 0.40
Nodes (3): RuntimeInitializeOnLoadMethod, MatchThemAll.Scripts.Utilities, StaticEventCleaner

### Community 89 - "Community 89"
Cohesion: 0.50
Nodes (3): string, EntitlementIds, MatchThemAll.Scripts.Shop

## Knowledge Gaps
- **493 isolated node(s):** `MatchThemAll.Scripts.Editor`, `int`, `Dictionary`, `InitializeOnLoadMethod`, `Texture2D` (+488 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Item` connect `Community 12` to `Community 44`?**
  _High betweenness centrality (0.041) - this node is a cross-community bridge._
- **Why does `ItemSpotManager` connect `Community 3` to `Community 44`?**
  _High betweenness centrality (0.035) - this node is a cross-community bridge._
- **Why does `HintManager` connect `Community 29` to `Community 44`?**
  _High betweenness centrality (0.026) - this node is a cross-community bridge._
- **What connects `MatchThemAll.Scripts.Editor`, `int`, `Dictionary` to the rest of the system?**
  _493 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.07441016333938294 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.07529411764705882 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.07180851063829788 - nodes in this community are weakly interconnected._