# Graph Report - Match Them All  (2026-07-19)

## Corpus Check
- 99 files · ~161,613 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1265 nodes · 1694 edges · 102 communities (85 shown, 17 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 31 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a3d6b692`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Level Editor Tools|Level Editor Tools]]
- [[_COMMUNITY_Item Manager Editor|Item Manager Editor]]
- [[_COMMUNITY_Item Spot Management|Item Spot Management]]
- [[_COMMUNITY_Save System & Player Data|Save System & Player Data]]
- [[_COMMUNITY_Core Game Events|Core Game Events]]
- [[_COMMUNITY_Power-Up Types & Settings|Power-Up Types & Settings]]
- [[_COMMUNITY_Animations & Power-Ups|Animations & Power-Ups]]
- [[_COMMUNITY_Input System|Input System]]
- [[_COMMUNITY_Audio & Sound Data|Audio & Sound Data]]
- [[_COMMUNITY_Timer Management|Timer Management]]
- [[_COMMUNITY_Item Behavior|Item Behavior]]
- [[_COMMUNITY_Item Placement|Item Placement]]
- [[_COMMUNITY_Input Handling|Input Handling]]
- [[_COMMUNITY_Goal Management|Goal Management]]
- [[_COMMUNITY_Merge System|Merge System]]
- [[_COMMUNITY_Item Spot Layout|Item Spot Layout]]
- [[_COMMUNITY_Editor Reference Tools|Editor Reference Tools]]
- [[_COMMUNITY_Audio Manager|Audio Manager]]
- [[_COMMUNITY_Game Manager|Game Manager]]
- [[_COMMUNITY_Level Data Management|Level Data Management]]
- [[_COMMUNITY_Continue Panel|Continue Panel]]
- [[_COMMUNITY_Project Architecture|Project Architecture]]
- [[_COMMUNITY_Goal Card UI|Goal Card UI]]
- [[_COMMUNITY_Hint System|Hint System]]
- [[_COMMUNITY_Loading Screen|Loading Screen]]
- [[_COMMUNITY_Combo System|Combo System]]
- [[_COMMUNITY_Level Data Config|Level Data Config]]
- [[_COMMUNITY_Floating Text Spawner|Floating Text Spawner]]
- [[_COMMUNITY_Pixelate Render Feature|Pixelate Render Feature]]
- [[_COMMUNITY_Daily Reward Panel|Daily Reward Panel]]
- [[_COMMUNITY_Floating Text Animation|Floating Text Animation]]
- [[_COMMUNITY_Pixelate Render Pass|Pixelate Render Pass]]
- [[_COMMUNITY_Item Object Pool|Item Object Pool]]
- [[_COMMUNITY_UI Animations|UI Animations]]
- [[_COMMUNITY_Tutorial Steps|Tutorial Steps]]
- [[_COMMUNITY_Settings Panel|Settings Panel]]
- [[_COMMUNITY_Win Panel|Win Panel]]
- [[_COMMUNITY_Daily Reward Manager|Daily Reward Manager]]
- [[_COMMUNITY_Event Bus|Event Bus]]
- [[_COMMUNITY_Item Spot Logic|Item Spot Logic]]
- [[_COMMUNITY_Level Select Buttons|Level Select Buttons]]
- [[_COMMUNITY_Player Data Model|Player Data Model]]
- [[_COMMUNITY_UI Manager|UI Manager]]
- [[_COMMUNITY_Level Select Screen|Level Select Screen]]
- [[_COMMUNITY_Coin Display|Coin Display]]
- [[_COMMUNITY_Main Menu|Main Menu]]
- [[_COMMUNITY_Scene Loading|Scene Loading]]
- [[_COMMUNITY_Match System|Match System]]
- [[_COMMUNITY_Pixelate Controller|Pixelate Controller]]
- [[_COMMUNITY_Power-Up Base|Power-Up Base]]
- [[_COMMUNITY_Save System Bootstrap|Save System Bootstrap]]
- [[_COMMUNITY_Combo VFX|Combo VFX]]
- [[_COMMUNITY_Floating Text Testing|Floating Text Testing]]
- [[_COMMUNITY_Ad Manager Mock|Ad Manager Mock]]
- [[_COMMUNITY_Game Sprites|Game Sprites]]
- [[_COMMUNITY_Hierarchy Editor Tool|Hierarchy Editor Tool]]
- [[_COMMUNITY_Transform Extensions|Transform Extensions]]
- [[_COMMUNITY_Lose Panel|Lose Panel]]
- [[_COMMUNITY_Static Event Cleanup|Static Event Cleanup]]
- [[_COMMUNITY_UI Background Elements|UI Background Elements]]
- [[_COMMUNITY_Button States|Button States]]
- [[_COMMUNITY_Item Level Data|Item Level Data]]
- [[_COMMUNITY_Potion Icons|Potion Icons]]
- [[_COMMUNITY_Audio Toggle Icons|Audio Toggle Icons]]
- [[_COMMUNITY_Game Events Definition|Game Events Definition]]
- [[_COMMUNITY_Game State Enum|Game State Enum]]
- [[_COMMUNITY_Item Name Enum|Item Name Enum]]
- [[_COMMUNITY_Heart Gem Icons|Heart Gem Icons]]
- [[_COMMUNITY_Coin & Diamond Icons|Coin & Diamond Icons]]
- [[_COMMUNITY_Checkmark & Cross|Checkmark & Cross]]
- [[_COMMUNITY_Reward & Currency Icons|Reward & Currency Icons]]
- [[_COMMUNITY_Arrow Icons|Arrow Icons]]
- [[_COMMUNITY_Battery Icon|Battery Icon]]
- [[_COMMUNITY_Bomb Icon|Bomb Icon]]
- [[_COMMUNITY_Skull Icon|Skull Icon]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]
- [[_COMMUNITY_Community 80|Community 80]]
- [[_COMMUNITY_Community 81|Community 81]]
- [[_COMMUNITY_Community 82|Community 82]]
- [[_COMMUNITY_Community 83|Community 83]]
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
- [[_COMMUNITY_Community 96|Community 96]]
- [[_COMMUNITY_Community 97|Community 97]]
- [[_COMMUNITY_Community 98|Community 98]]
- [[_COMMUNITY_Community 99|Community 99]]
- [[_COMMUNITY_Community 100|Community 100]]
- [[_COMMUNITY_Community 101|Community 101]]

## God Nodes (most connected - your core abstractions)
1. `LevelEditorWindow` - 47 edges
2. `ItemManagerWindow` - 46 edges
3. `SaveManager` - 36 edges
4. `ItemSpotManager` - 34 edges
5. `LevelMapManager` - 27 edges
6. `TutorialManager` - 26 edges
7. `Item` - 21 edges
8. `TimerManager` - 20 edges
9. `InputManager` - 19 edges
10. `LevelManager` - 18 edges

## Surprising Connections (you probably didn't know these)
- `Vacuum Power-Up Texture` --part_of--> `Match-3 UI Asset Collection`  [INFERRED]
  Material/Textures/Vacuum Tex.png → Sprites/Background.png
- `Heart Gem Icon` --semantically_similar_to--> `Heart Gem Item Icon`  [INFERRED] [semantically similar]
  Sprites/Icons/icon_heartgem.png → Sprites/Icons/icon_item_heartgem.png
- `Blue Potion Item Icon` --semantically_similar_to--> `Potion Icon`  [INFERRED] [semantically similar]
  Sprites/Icons/icon_item_bluepotion.png → Sprites/Icons/icon_potion.png
- `Blue Potion Item Icon` --semantically_similar_to--> `Blue Potion Item Icon (Trash)`  [INFERRED] [semantically similar]
  Sprites/Icons/icon_item_bluepotion.png → Sprites/Icons/Trash/icon_item_bluepotion.png
- `Coin Item Icon` --semantically_similar_to--> `Diamond Item Icon`  [INFERRED] [semantically similar]
  Sprites/Icons/icon_item_coin.png → Sprites/Icons/icon_item_diamond.png

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Match-3 Collectible Item Icons** — sprites_icons_icon_item_battery_battery, sprites_icons_icon_item_bluepotion_blue_potion, sprites_icons_icon_item_bomb_bomb, sprites_icons_icon_item_coin_coin, sprites_icons_icon_item_diamond_diamond, sprites_icons_icon_item_heartgem_heart_gem, sprites_icons_icon_item_skull_skull [INFERRED 0.85]
- **Consumable Potion-Type Icons** — sprites_icons_icon_potion_potion, sprites_icons_icon_item_bluepotion_blue_potion, sprites_icons_trash_icon_item_bluepotion_blue_potion [INFERRED 0.75]
- **Button Visual States** — sprites_ui_button_base_button_base, sprites_ui_button_outline_button_outline, sprites_ui_button_overlay_button_overlay [INFERRED 0.95]
- **Settings Panel Controls** — sprites_ui_music_music, sprites_ui_sfx_sfx, sprites_ui_settings_settings [INFERRED 0.85]
- **Goal Card Set Hyperedge** — sprites_goal_card_goal_card_front, sprites_goal_card_back_goal_card_back, sprites_goal_card_goal_card_set [EXTRACTED 1.00]

## Communities (102 total, 17 thin omitted)

### Community 0 - "Level Editor Tools"
Cohesion: 0.08
Nodes (21): Editor, DeletedItemRecord, DeletedItemRecordList, LevelEditorWindow, Match_Them_All.Scripts.Editor, RemovedLevelEntry, EditorWindow, bool (+13 more)

### Community 1 - "Item Manager Editor"
Cohesion: 0.08
Nodes (20): double, ItemManagerWindow, Match_Them_All.Scripts.Editor, PreviewRenderUtility, bool, Color, EItemName, float (+12 more)

### Community 2 - "Item Spot Management"
Cohesion: 0.10
Nodes (16): ItemSpotManager, MatchThemAll.Scripts, Action, Ease, EItemName, float, IEnumerable, int (+8 more)

### Community 3 - "Save System & Player Data"
Cohesion: 0.07
Nodes (13): hapticsEnabled, lastPlayedDate, loginStreak, musicVolume, PlayerData, MatchThemAll.Scripts.SaveSystem, SaveManager, bool (+5 more)

### Community 4 - "Core Game Events"
Cohesion: 0.11
Nodes (15): Coroutine, CanvasGroup, GameObject, IEnumerator, int, ItemClickedEvent, Level, List (+7 more)

### Community 5 - "Power-Up Types & Settings"
Cohesion: 0.11
Nodes (12): bool, GameSettingsSO, Item, Powerup, PowerupClickedEvent, PowerupContext, PowerupDatabaseSO, PowerupDataSO (+4 more)

### Community 6 - "Animations & Power-Ups"
Cohesion: 0.15
Nodes (9): Powerup, Fan, Match_Them_All.Scripts.Power_Ups, FreezeGun, Match_Them_All.Scripts.Power_Ups, Match_Them_All.Scripts.Power_Ups, Spring, Match_Them_All.Scripts.Power_Ups (+1 more)

### Community 7 - "Input System"
Cohesion: 0.12
Nodes (18): CallbackContext, IDisposable, IGameplayActions, IInputActionCollection2, InputAction, InputActionMap, InputBinding, AddCallbacks() (+10 more)

### Community 8 - "Audio & Sound Data"
Cohesion: 0.20
Nodes (8): int, ItemLevelData, List, Sprite, string, RewardCalculationMode, LevelDataSO, MatchThemAll.Scripts

### Community 9 - "Timer Management"
Cohesion: 0.14
Nodes (10): DebugRunOutOfTime(), MatchThemAll.Scripts, TimerManager, bool, Button, GameStateChangedEvent, IEnumerator, Level (+2 more)

### Community 10 - "Item Behavior"
Cohesion: 0.10
Nodes (11): Collider, Item, MatchThemAll.Scripts, Renderer, Rigidbody, bool, EItemName, ItemSpot (+3 more)

### Community 11 - "Item Placement"
Cohesion: 0.16
Nodes (12): BoxCollider, ItemPlacer, MatchThemAll.Scripts, PreviewSpawn(), PreviewSpawnFromEditor(), PreviewSpawnWithData(), Button, Item (+4 more)

### Community 12 - "Input Handling"
Cohesion: 0.14
Nodes (8): Camera, InputManager, LayerMask, bool, GameStateChangedEvent, Item, Material, MTAInputSystem_Actions

### Community 13 - "Goal Management"
Cohesion: 0.13
Nodes (9): GoalManager, MatchThemAll.Scripts, GoalCard, int, Item, ItemLevelData, Level, List (+1 more)

### Community 14 - "Merge System"
Cohesion: 0.14
Nodes (11): MatchThemAll.Scripts, MergeManager, IObjectPool, ParticleSystem, Ease, float, IEnumerator, int (+3 more)

### Community 15 - "Item Spot Layout"
Cohesion: 0.19
Nodes (9): Axis, ContextMenu, ItemSpotLayout, MatchThemAll.Scripts, LayoutMode, bool, float, List (+1 more)

### Community 16 - "Editor Reference Tools"
Cohesion: 0.19
Nodes (12): ItemReferenceOps, Match_Them_All.Scripts.Editor, entry, IList, index, levelName, Item, ItemLevelData (+4 more)

### Community 17 - "Audio Manager"
Cohesion: 0.16
Nodes (7): AudioMixer, AudioSource, MatchThemAll.Scripts, SoundManager, int, string, SoundDataSO

### Community 18 - "Game Manager"
Cohesion: 0.16
Nodes (4): GameManager, MatchThemAll.Scripts, EGameState, SpotFilledEvent

### Community 19 - "Level Data Management"
Cohesion: 0.11
Nodes (12): IReadOnlyList<string>, Action, bool, Button, GameStateChangedEvent, int, Level, LevelDataSO (+4 more)

### Community 20 - "Continue Panel"
Cohesion: 0.14
Nodes (8): ContinuePanelManager, MatchThemAll.Scripts.UI, Button, float, GameObject, GameSettingsSO, GameStateChangedEvent, TextMeshProUGUI

### Community 21 - "Project Architecture"
Cohesion: 0.12
Nodes (16): Core Infrastructure, EItemName, EventBus Communication, Feature-Based Organization, GameEvents, Gameplay System, Item Component, Level System (+8 more)

### Community 22 - "Goal Card UI"
Cohesion: 0.13
Nodes (8): GoalCard, MatchThemAll.Scripts.UI, Animator, bool, GameObject, Image, Sprite, TextMeshProUGUI

### Community 23 - "Hint System"
Cohesion: 0.16
Nodes (7): HintManager, MatchThemAll.Scripts.Managers, EItemName, float, GameStateChangedEvent, ItemClickedEvent, List

### Community 24 - "Loading Screen"
Cohesion: 0.16
Nodes (10): AsyncOperation, LoadingScreenManager, MatchThemAll.Scripts, float, GameObject, IEnumerator, Image, Slider (+2 more)

### Community 25 - "Combo System"
Cohesion: 0.16
Nodes (7): ComboManager, MatchThemAll.Scripts, Action, float, int, Level, MergeStartedEvent

### Community 26 - "Level Data Config"
Cohesion: 0.16
Nodes (8): ItemPlacer, ItemLevelData, LevelDataSO, List<Item>, Task, TutorialStep>, Level, MatchThemAll.Scripts

### Community 27 - "Floating Text Spawner"
Cohesion: 0.18
Nodes (8): Canvas, ObjectPool, FloatingTextSpawner, MatchThemAll.Scripts.UI, Color, FloatingText, RectTransform, Vector3

### Community 28 - "Pixelate Render Feature"
Cohesion: 0.17
Nodes (9): CustomPassSettings, Match_Them_All.Scripts.Pixelate, PixelizeFeature, PixelizePass, RenderingData, RenderPassEvent, ScriptableRenderer, ScriptableRendererFeature (+1 more)

### Community 29 - "Daily Reward Panel"
Cohesion: 0.18
Nodes (8): DailyRewardPanel, MatchThemAll.Scripts.UI, Action, Button, CanvasGroup, GameObject, TextMeshProUGUI, Transform

### Community 30 - "Floating Text Animation"
Cohesion: 0.23
Nodes (7): FloatingText, MatchThemAll.Scripts.UI, Action, Color, float, RectTransform, TextMeshProUGUI

### Community 31 - "Pixelate Render Pass"
Cohesion: 0.17
Nodes (9): ContextContainer, CustomPassSettings, Match_Them_All.Scripts.Pixelate, PassData, PixelizePass, RenderGraph, ScriptableRenderPass, int (+1 more)

### Community 32 - "Item Object Pool"
Cohesion: 0.26
Nodes (4): Dictionary, Item, ItemPoolManager, MatchThemAll.Scripts

### Community 33 - "UI Animations"
Cohesion: 0.17
Nodes (8): MatchThemAll.Scripts.UI, UIAnimator, CanvasGroup, Ease, float, Image, Transform, Vector3

### Community 34 - "Tutorial Steps"
Cohesion: 0.20
Nodes (9): ECompletionCondition, EHighlightTarget, bool, EItemName, float, List, string, MatchThemAll.Scripts.Tutorial (+1 more)

### Community 35 - "Settings Panel"
Cohesion: 0.18
Nodes (4): MatchThemAll.Scripts.UI, SettingsManager, Toggle, Slider

### Community 36 - "Win Panel"
Cohesion: 0.24
Nodes (4): MatchThemAll.Scripts.UI, WinPanelManager, float, GameObject

### Community 37 - "Daily Reward Manager"
Cohesion: 0.29
Nodes (4): DailyRewardManager, MatchThemAll.Scripts.UI, DailyRewardPanel, Transform

### Community 38 - "Event Bus"
Cohesion: 0.27
Nodes (5): EventBus, MatchThemAll.Scripts, Action, Dictionary, T

### Community 39 - "Item Spot Logic"
Cohesion: 0.20
Nodes (5): ItemSpot, MatchThemAll.Scripts, Animator, Item, Transform

### Community 40 - "Level Select Buttons"
Cohesion: 0.20
Nodes (7): LevelButtonUI, MatchThemAll.Scripts.UI, bool, Button, GameObject, int, TextMeshProUGUI

### Community 41 - "Player Data Model"
Cohesion: 0.15
Nodes (10): LevelProgressEntry, MatchThemAll.Scripts.SaveSystem, PlayerData, PowerupSaveEntry, bool, float, int, IReadOnlyList (+2 more)

### Community 42 - "UI Manager"
Cohesion: 0.28
Nodes (4): MatchThemAll.Scripts, UIManager, GameObject, GameStateChangedEvent

### Community 43 - "Level Select Screen"
Cohesion: 0.29
Nodes (5): LevelButtonUI, LevelMapNode, MatchThemAll.Scripts.UI, Image, LevelDataSO

### Community 44 - "Coin Display"
Cohesion: 0.29
Nodes (3): CoinDisplay, MatchThemAll.Scripts.UI, TextMeshProUGUI

### Community 45 - "Main Menu"
Cohesion: 0.25
Nodes (3): MainMenuManager, MatchThemAll.Scripts.UI, GameObject

### Community 46 - "Scene Loading"
Cohesion: 0.38
Nodes (3): MatchThemAll.Scripts, SceneLoader, string

### Community 47 - "Match System"
Cohesion: 0.29
Nodes (3): MatchSystem, MatchThemAll.Scripts, ItemReachedSpotEvent

### Community 48 - "Pixelate Controller"
Cohesion: 0.29
Nodes (3): Match_Them_All.Scripts.Pixelate, PixelizeController, GameStateChangedEvent

### Community 49 - "Power-Up Base"
Cohesion: 0.13
Nodes (10): Action, Animator, GameObject, Image, PowerupDatabaseSO, PowerupDataSO, string, Match_Them_All.Scripts.Power_Ups (+2 more)

### Community 50 - "Save System Bootstrap"
Cohesion: 0.29
Nodes (3): MatchThemAll.Scripts.SaveSystem, SaveManagerBootstrapper, RuntimeInitializeOnLoadMethod

### Community 51 - "Combo VFX"
Cohesion: 0.29
Nodes (3): ComboVFX, MatchThemAll.Scripts.UI, FloatingText

### Community 52 - "Floating Text Testing"
Cohesion: 0.29
Nodes (4): FloatingTextTester, MatchThemAll.Scripts.Testing, Color, string

### Community 53 - "Ad Manager Mock"
Cohesion: 0.29
Nodes (3): Action, AdManagerMock, MatchThemAll.Scripts.Utilities

### Community 54 - "Game Sprites"
Cohesion: 0.48
Nodes (7): Game Background, Match-3 UI Asset Collection, Goal Card (Back), Goal Card (Front), Goal Card Set, Video/Splash Image, Vacuum Power-Up Texture

### Community 55 - "Hierarchy Editor Tool"
Cohesion: 0.47
Nodes (3): HierarchySectionHeader, GameObject, Rect

### Community 56 - "Transform Extensions"
Cohesion: 0.40
Nodes (3): MatchThemAll.Scripts.Extensions, TransformExtensions, Transform

### Community 57 - "Lose Panel"
Cohesion: 0.33
Nodes (3): MonoBehaviour, LosePanelManager, MatchThemAll.Scripts.UI

### Community 59 - "UI Background Elements"
Cohesion: 0.50
Nodes (4): UI Background, Basic Container Panel, Match-3 UI Sprite Collection, Star Scroll Texture

### Community 60 - "Button States"
Cohesion: 0.67
Nodes (4): Button Base (Normal State), Button Outline (Highlight State), Button Overlay (Pressed State), Round Button

### Community 62 - "Potion Icons"
Cohesion: 0.67
Nodes (3): Blue Potion Item Icon, Potion Icon, Blue Potion Item Icon (Trash)

### Community 63 - "Audio Toggle Icons"
Cohesion: 1.00
Nodes (3): Music Toggle Icon, Settings Icon, SFX Toggle Icon

### Community 75 - "Community 75"
Cohesion: 0.07
Nodes (22): float, PowerupContext, PowerupContext, float, PowerupContext, Vector2, Vector3, Item (+14 more)

### Community 76 - "Community 76"
Cohesion: 0.15
Nodes (12): Architecture Rules, Communication Pattern, Community Map (75 communities — key ones listed), Cross-Community Bridge Nodes (high betweenness — careful!), God Nodes (most connected — touch with extreme care), graphify, Key Events (defined in `GameEvents.cs`), MatchThemAll — Claude Code Context (+4 more)

### Community 77 - "Community 77"
Cohesion: 0.25
Nodes (6): Match_Them_All.Scripts.Editor, PowerupDatabaseSetup, MenuItem, PowerupDataSO, PowerupEffect, string

### Community 78 - "Community 78"
Cohesion: 0.40
Nodes (3): PowerupContext, Match_Them_All.Scripts.Power_Ups, PowerupEffect

### Community 79 - "Community 79"
Cohesion: 0.33
Nodes (5): 🎮 How to add a new item type, 🛠 How to create or edit a level, 📐 Project Architecture (brief), 👋 Start Here — Match Them All Customization Guide, 📂 What's in this folder

### Community 85 - "Community 85"
Cohesion: 0.10
Nodes (13): LevelMapPath, LevelMapManager, MatchThemAll.Scripts.UI, ScrollRect, Button, float, IEnumerator, IReadOnlyList (+5 more)

### Community 86 - "Community 86"
Cohesion: 0.20
Nodes (8): LevelMapBuilder, Match_Them_All.Scripts.Editor, GameObject, LevelMapNode, MenuItem, RectTransform, string, Transform

### Community 87 - "Community 87"
Cohesion: 0.15
Nodes (8): Graphic, LevelMapPath, MatchThemAll.Scripts.UI, float, IEnumerable, List, Vector2, VertexHelper

### Community 88 - "Community 88"
Cohesion: 0.12
Nodes (15): Match_Them_All.Scripts.Editor, ShopSetup, LayoutElement, Button, Color, ECurrency, EKind, EShopCategory (+7 more)

### Community 89 - "Community 89"
Cohesion: 0.16
Nodes (9): MatchThemAll.Scripts.Shop, ShopPanel, Button, EShopCategory, GameObject, List, ShopDatabaseSO, ShopProductCard (+1 more)

### Community 90 - "Community 90"
Cohesion: 0.18
Nodes (8): IIapService, MatchThemAll.Scripts.Shop, ShopManager, Action, IReadOnlyList, ShopDatabaseSO, ShopProductSO, ShopReward

### Community 91 - "Community 91"
Cohesion: 0.21
Nodes (7): MatchThemAll.Scripts.Shop, ShopProductCard, Button, GameObject, Image, ShopProductSO, TextMeshProUGUI

### Community 92 - "Community 92"
Cohesion: 0.20
Nodes (11): MatchThemAll.Scripts.Shop, ShopProductSO, ShopReward, bool, ECurrency, EKind, EShopCategory, int (+3 more)

### Community 93 - "Community 93"
Cohesion: 0.29
Nodes (4): IIapService, MatchThemAll.Scripts.Shop, NullIapService, Action

### Community 94 - "Community 94"
Cohesion: 0.20
Nodes (8): ECurrency, GameObject, int, PowerupEffect, Sprite, string, Match_Them_All.Scripts.Power_Ups, PowerupDataSO

### Community 95 - "Community 95"
Cohesion: 0.25
Nodes (6): MatchThemAll.Scripts.Shop, ShopDatabaseSO, EShopCategory, IEnumerable, List, ShopProductSO

### Community 96 - "Community 96"
Cohesion: 0.29
Nodes (3): GemDisplay, MatchThemAll.Scripts.Shop, TextMeshProUGUI

### Community 97 - "Community 97"
Cohesion: 0.29
Nodes (6): AudioClip, AudioMixerGroup, MatchThemAll.Scripts, SoundDataSO, bool, float

### Community 98 - "Community 98"
Cohesion: 0.29
Nodes (5): List, PowerupDataSO, ScriptableObject, Match_Them_All.Scripts.Power_Ups, PowerupDatabaseSO

### Community 99 - "Community 99"
Cohesion: 0.33
Nodes (3): MatchThemAll.Scripts.Shop, WatchAdForCoinsButton, int

### Community 100 - "Community 100"
Cohesion: 0.33
Nodes (3): MatchThemAll.Scripts.Shop, ShopOpener, ShopPanel

### Community 101 - "Community 101"
Cohesion: 0.40
Nodes (4): bool, int, GameSettingsSO, MatchThemAll.Scripts.Settings

## Ambiguous Edges - Review These
- `Game Background` → `Video/Splash Image`  [AMBIGUOUS]
  Sprites/Video.png · relation: visually_similar_to

## Knowledge Gaps
- **448 isolated node(s):** `MatchThemAll.Scripts`, `MatchThemAll.Scripts`, `Dictionary`, `MatchThemAll.Scripts`, `ItemPlacer` (+443 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **17 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Game Background` and `Video/Splash Image`?**
  _Edge tagged AMBIGUOUS (relation: visually_similar_to) - confidence is low._
- **Why does `Item` connect `Item Behavior` to `Lose Panel`?**
  _High betweenness centrality (0.035) - this node is a cross-community bridge._
- **Why does `ItemSpotManager` connect `Item Spot Management` to `Lose Panel`?**
  _High betweenness centrality (0.026) - this node is a cross-community bridge._
- **Why does `TimerManager` connect `Timer Management` to `Lose Panel`?**
  _High betweenness centrality (0.025) - this node is a cross-community bridge._
- **What connects `MatchThemAll.Scripts`, `MatchThemAll.Scripts`, `Dictionary` to the rest of the system?**
  _449 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Level Editor Tools` be split into smaller, more focused modules?**
  _Cohesion score 0.07581453634085213 - nodes in this community are weakly interconnected._
- **Should `Item Manager Editor` be split into smaller, more focused modules?**
  _Cohesion score 0.07673469387755102 - nodes in this community are weakly interconnected._