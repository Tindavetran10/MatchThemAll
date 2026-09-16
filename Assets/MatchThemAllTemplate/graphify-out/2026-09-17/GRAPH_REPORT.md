# Graph Report - MatchThemAllTemplate  (2026-09-16)

## Corpus Check
- 104 files · ~168,807 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 398 file(s) not represented in the graph (top: .meta 303, .asset 38, .prefab 30)

## Summary
- 2418 nodes · 4267 edges · 147 communities (126 shown, 21 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 68 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `509c47b6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- LevelEditorWindow
- ItemManagerWindow
- SaveManager
- ItemSpotManager
- ShopEditorWindow
- VacuumEffect
- ShopSetup
- LevelMapManager
- TutorialManager
- API Reference
- @MTAInputSystem_Actions
- PowerupManager
- Item
- TimerManager
- ItemPlacer
- InputManager
- LevelManager
- GoalManager
- MergeManager
- ShopPanel
- ItemSpotLayout
- .RemoveFromLevels
- SoundManager
- GameManager
- ContinuePanelManager
- PlayerData
- Powerup
- ShopProductCard
- GoalCard
- HintManager
- Action
- LoadingScreenManager
- LevelMapBuilder
- ComboManager
- Level
- VfxPool
- FloatingText
- LevelMapPath
- PixelizeFeature
- Scripts/Runtime/PowerUps/Fan.cs
- DailyRewardPanel
- Item
- ItemSpotManager
- DebugCheats
- OpenSceneLoader
- Customization Guide
- EditorWindowStyles
- ItemPoolManager
- ShopProductSO
- UIAnimator
- LevelEditorWindow
- LevelDataSO
- SettingsManager
- WinPanelManager
- PowerupDataSO
- DailyRewardManager
- EventBus
- TutorialStep
- ItemSpot
- ShopDatabaseSO
- LevelButtonUI
- PowerupDatabaseSetup
- PrefabConsolidator
- UIManager
- Match Them All - Complete Game Template
- MainMenuManager
- GemDisplay
- WatchAdForCoinsButton
- CoinDisplay
- SoundDataSO
- SceneLoader
- MonoBehaviour
- LevelMapNode
- PixelizeController
- ScriptableObject
- SaveManagerBootstrapper
- ComboVFX
- FloatingTextTester
- .OwnsEntitlement
- [1.0.0] - 2026-09-07
- .DrawHighlight
- Transform
- PowerupEffect
- DebugGrantCurrencyButton
- ShopTabSO
- PowerupManager
- 👋 Start Here — Match Them All Customization Guide
- ItemManagerWindow
- EventBus
- Scripts/Runtime/Shop/EntitlementIds.cs
- Scripts/Runtime/LevelSystem/ItemLevelData.cs
- Scripts/Runtime/Core/GameEvents.cs
- Scripts/Runtime/Utilities/Enums/EGameState.cs
- Scripts/Runtime/Utilities/Enums/EItemName.cs
- Scripts/Runtime/PowerUps/ECurrency.cs
- Scripts/Runtime/PowerUps/PowerupContext.cs
- .BuildPanel
- TutorialManager
- TutorialStep
- GameStateChangedEvent
- LevelMapManager
- SaveManager
- MatchThemAll.Scripts
- GoalManager
- ShopPanel
- .Load
- ShopProductSO
- GameManager
- .Build
- .Flush
- ItemPlacer
- PowerupContext
- SoundManager
- MatchThemAll.Scripts.Shop
- .GenerateItemPrefab
- .DrawLevelDetail
- EItemName
- List
- ShopSetup
- PlayerData
- MergeStartedEvent
- MatchThemAll.Scripts.UI
- .CreateDefaultProducts
- Level
- LevelButtonUI
- Scripts/Runtime/PowerUps/PowerupManager.cs
- SpringEffect
- .OnDisable
- EGameState
- .DrawDockPreview
- FanEffect
- .Publish
- .AddCoins
- Texture2D
- FreezeEffect
- SpringEffect
- .Activate
- VacuumEffect
- EKind
- WinPanelManager
- .Start
- .CreateDatabase
- .ShowWindow
- .ResetSessionStatics
- Scripts/Runtime/PowerUps/FreezeGun.cs
- Scripts/Runtime/PowerUps/Spring.cs
- Scripts/Runtime/PowerUps/Vacuum.cs

## God Nodes (most connected - your core abstractions)
1. `Item` - 76 edges
2. `LevelEditorWindow` - 54 edges
3. `LevelEditorWindow` - 49 edges
4. `ItemManagerWindow` - 47 edges
5. `ItemManagerWindow` - 42 edges
6. `SaveManager` - 39 edges
7. `SaveManager` - 38 edges
8. `MatchThemAll.Scripts` - 37 edges
9. `ShopEditorWindow` - 37 edges
10. `ItemSpotManager` - 35 edges

## Surprising Connections (you probably didn't know these)
- `Fan` --inherits--> `Powerup`  [EXTRACTED]
  Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Fan.cs → Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Powerup.cs
- `FreezeGun` --inherits--> `Powerup`  [EXTRACTED]
  Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/FreezeGun.cs → Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Powerup.cs
- `Spring` --inherits--> `Powerup`  [EXTRACTED]
  Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Spring.cs → Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Powerup.cs
- `ItemManagerWindow` --references--> `EItemName`  [EXTRACTED]
  Assets/MatchThemAllTemplate/Scripts/Editor/ItemManagerWindow.cs → Assets/MatchThemAllTemplate/Scripts/Runtime/Utilities/Enums/EItemName.cs
- `ShopEditorWindow` --references--> `ShopDatabaseSO`  [EXTRACTED]
  Assets/MatchThemAllTemplate/Scripts/Editor/ShopEditorWindow.cs → Assets/MatchThemAllTemplate/Scripts/Runtime/Shop/ShopDatabaseSO.cs

## Import Cycles
- None detected.

## Communities (147 total, 21 thin omitted)

### Community 0 - "LevelEditorWindow"
Cohesion: 0.10
Nodes (8): Editor, LevelEditorWindow, float, GameSettingsSO, GUIStyle, MenuItem, Vector2, Texture

### Community 1 - "ItemManagerWindow"
Cohesion: 0.12
Nodes (8): double, ItemManagerWindow, MatchThemAll.Scripts.Editor, bool, EItemName, float, int, string

### Community 2 - "SaveManager"
Cohesion: 0.09
Nodes (7): PlayerData, MatchThemAll.Scripts.SaveSystem, SaveManager, bool, ECurrency, PowerupDatabaseSO, string

### Community 3 - "ItemSpotManager"
Cohesion: 0.10
Nodes (14): ItemSpotManager, MatchThemAll.Scripts, Ease, EItemName, float, IEnumerable, int, Item (+6 more)

### Community 4 - "ShopEditorWindow"
Cohesion: 0.06
Nodes (17): MatchThemAll.Scripts.Editor, ShopEditorWindow, EditorWindow, bool, Color, float, GUIStyle, int (+9 more)

### Community 5 - "VacuumEffect"
Cohesion: 0.27
Nodes (5): MatchThemAll.Scripts.Power_Ups, VacuumEffect, Item, ItemLevelData, PowerupContext

### Community 6 - "ShopSetup"
Cohesion: 0.19
Nodes (5): MatchThemAll.Scripts.Editor, ShopSetup, Color, ShopProductCard, string

### Community 7 - "LevelMapManager"
Cohesion: 0.11
Nodes (12): LevelMapPath, LevelMapManager, MatchThemAll.Scripts.UI, Button, float, IEnumerator, IReadOnlyList, LevelMapNode (+4 more)

### Community 8 - "TutorialManager"
Cohesion: 0.18
Nodes (6): IEnumerator, int, List, TutorialStep, MatchThemAll.Scripts.Managers, TutorialManager

### Community 9 - "API Reference"
Cohesion: 0.07
Nodes (29): AdManagerMock, API Reference, ComboManager, Configuration, Core Systems, Event System, EventBus<T>, Game Events (+21 more)

### Community 10 - "@MTAInputSystem_Actions"
Cohesion: 0.05
Nodes (36): CallbackContext, AddCallbacks(), Disable(), Enable(), Get(), IGameplayActions, @MTAInputSystem_Actions, RemoveCallbacks() (+28 more)

### Community 11 - "PowerupManager"
Cohesion: 0.13
Nodes (11): PowerupManager, bool, GameSettingsSO, Item, Powerup, PowerupClickedEvent, PowerupContext, PowerupDatabaseSO (+3 more)

### Community 12 - "Item"
Cohesion: 0.08
Nodes (13): Bounds, Collider, Item, MatchThemAll.Scripts, Renderer, Rigidbody, bool, EItemName (+5 more)

### Community 13 - "TimerManager"
Cohesion: 0.09
Nodes (13): DebugRunOutOfTime(), MatchThemAll.Scripts, TimerManager, bool, Button, GameStateChangedEvent, IEnumerator, Level (+5 more)

### Community 14 - "ItemPlacer"
Cohesion: 0.16
Nodes (11): BoxCollider, ItemPlacer, MatchThemAll.Scripts, PreviewSpawn(), PreviewSpawnFromEditor(), PreviewSpawnWithData(), Button, Item (+3 more)

### Community 15 - "InputManager"
Cohesion: 0.08
Nodes (16): Camera, InputManager, MatchThemAll.Scripts, LayerMask, MeshRenderer, MTAInputSystem_Actions, bool, Collider (+8 more)

### Community 16 - "LevelManager"
Cohesion: 0.06
Nodes (25): IReadOnlyList<string>, LevelManager, MatchThemAll.Scripts, List<string>, Action, bool, Button, GameStateChangedEvent (+17 more)

### Community 17 - "GoalManager"
Cohesion: 0.14
Nodes (7): GoalManager, MatchThemAll.Scripts, GoalCard, int, Item, ItemLevelData, Level

### Community 18 - "MergeManager"
Cohesion: 0.11
Nodes (12): MatchThemAll.Scripts, MergeManager, Ease, float, IEnumerator, int, IObjectPool, Item (+4 more)

### Community 19 - "ShopPanel"
Cohesion: 0.16
Nodes (7): Button, ShopDatabaseSO, ShopProductCard, string, MatchThemAll.Scripts.Shop, ShopPanel, UIAnimator

### Community 20 - "ItemSpotLayout"
Cohesion: 0.09
Nodes (19): Axis, ContextMenu, ItemSpotLayout, MatchThemAll.Scripts, LayoutMode, bool, float, List (+11 more)

### Community 21 - ".RemoveFromLevels"
Cohesion: 0.15
Nodes (16): ItemReferenceOps, MatchThemAll.Scripts.Editor, entry, IList, index, levelName, GameObject, index (+8 more)

### Community 22 - "SoundManager"
Cohesion: 0.19
Nodes (5): MatchThemAll.Scripts, SoundManager, int, string, SoundDataSO

### Community 23 - "GameManager"
Cohesion: 0.16
Nodes (4): GameManager, MatchThemAll.Scripts, EGameState, SpotFilledEvent

### Community 24 - "ContinuePanelManager"
Cohesion: 0.10
Nodes (9): ContinuePanelManager, MatchThemAll.Scripts.UI, Button, float, GameObject, GameSettingsSO, GameStateChangedEvent, TextMeshProUGUI (+1 more)

### Community 25 - "PlayerData"
Cohesion: 0.13
Nodes (10): LevelProgressEntry, MatchThemAll.Scripts.SaveSystem, PlayerData, PowerupSaveEntry, bool, float, int, IReadOnlyList (+2 more)

### Community 26 - "Powerup"
Cohesion: 0.11
Nodes (14): MatchThemAll.Scripts.Power_Ups, Powerup, Action, Animator, Collider, GameObject, Image, PowerupDatabaseSO (+6 more)

### Community 27 - "ShopProductCard"
Cohesion: 0.15
Nodes (9): Button, GameObject, Image, ShopProductSO, TextMeshProUGUI, ShopProductCard, MatchThemAll.Scripts.Shop, ShopProductCard (+1 more)

### Community 28 - "GoalCard"
Cohesion: 0.10
Nodes (9): Animator, bool, GameObject, Image, Sprite, TextMeshProUGUI, GoalCard, GoalCard (+1 more)

### Community 29 - "HintManager"
Cohesion: 0.13
Nodes (9): HintManager, MatchThemAll.Scripts.Managers, EItemName, float, GameStateChangedEvent, Item, ItemClickedEvent, List (+1 more)

### Community 30 - "Action"
Cohesion: 0.08
Nodes (16): HashSet, IIapService, MatchThemAll.Scripts.Shop, NullIapService, IIapService, Action, IIapService, IsInitialized (+8 more)

### Community 31 - "LoadingScreenManager"
Cohesion: 0.15
Nodes (12): AsyncOperation, LoadingScreenManager, MatchThemAll.Scripts, Canvas, float, GameObject, IEnumerator, Image (+4 more)

### Community 32 - "LevelMapBuilder"
Cohesion: 0.21
Nodes (7): LevelMapBuilder, MatchThemAll.Scripts.Editor, GameObject, LevelMapNode, MenuItem, string, Transform

### Community 33 - "ComboManager"
Cohesion: 0.18
Nodes (6): ComboManager, MatchThemAll.Scripts, float, int, Level, MergeStartedEvent

### Community 34 - "Level"
Cohesion: 0.18
Nodes (7): ItemPlacer, Level, MatchThemAll.Scripts, ItemLevelData, LevelDataSO, List<Item>, TutorialStep>

### Community 35 - "VfxPool"
Cohesion: 0.15
Nodes (11): Dictionary, GameObject, IEnumerator, IObjectPool, ParticleSystem, Transform, Vector3, VfxPool (+3 more)

### Community 36 - "FloatingText"
Cohesion: 0.07
Nodes (19): Canvas, ObjectPool, ComboVFX, Action, Color, float, RectTransform, TextMeshProUGUI (+11 more)

### Community 37 - "LevelMapPath"
Cohesion: 0.13
Nodes (9): Graphic, float, IEnumerable, List, Vector2, LevelMapPath, LevelMapPath, MatchThemAll.Scripts.UI (+1 more)

### Community 38 - "PixelizeFeature"
Cohesion: 0.06
Nodes (27): ContextContainer, MatchThemAll.Scripts.Pixelate, CustomPassSettings, PassData, CustomPassSettings, MatchThemAll.Scripts.Pixelate, PixelizeFeature, MatchThemAll.Scripts.Pixelate (+19 more)

### Community 40 - "DailyRewardPanel"
Cohesion: 0.15
Nodes (9): Action, Button, CanvasGroup, GameObject, TextMeshProUGUI, Transform, DailyRewardPanel, DailyRewardPanel (+1 more)

### Community 41 - "Item"
Cohesion: 0.08
Nodes (21): ItemPickedUpEvent, LevelSpawnedEvent, PowerupItemBackToGameEvent, PowerupItemPickedUpEvent, Collider, Renderer, Rigidbody, Item (+13 more)

### Community 42 - "ItemSpotManager"
Cohesion: 0.11
Nodes (6): ItemClickedEvent, ItemSpot, Item, Action, ItemSpotManager, Instance

### Community 43 - "DebugCheats"
Cohesion: 0.18
Nodes (6): DebugCheats, MatchThemAll.Scripts, Button, int, RuntimeInitializeOnLoadMethod, DebugCheats

### Community 44 - "OpenSceneLoader"
Cohesion: 0.22
Nodes (5): MatchThemAll.Scripts, OpenSceneLoader, float, IEnumerator, OpenSceneLoader

### Community 45 - "Customization Guide"
Cohesion: 0.17
Nodes (11): Adding a New Item Type, Adding a New Power-Up, Changing Game Colors/Theme, Creating a New Level, Customization Guide, Gem Models, Getting Help, Replacing Placeholder Assets (+3 more)

### Community 46 - "EditorWindowStyles"
Cohesion: 0.19
Nodes (9): EditorWindowStyles, MatchThemAll.Scripts.Editor, InitializeOnLoadMethod, Color, Dictionary, GUIStyle, int, Texture2D (+1 more)

### Community 47 - "ItemPoolManager"
Cohesion: 0.29
Nodes (3): ItemPoolManager, MatchThemAll.Scripts, Item

### Community 48 - "ShopProductSO"
Cohesion: 0.20
Nodes (10): bool, ECurrency, EKind, int, List, Sprite, string, MatchThemAll.Scripts.Shop (+2 more)

### Community 49 - "UIAnimator"
Cohesion: 0.14
Nodes (9): CanvasGroup, Ease, float, Image, Transform, Vector3, UIAnimator, MatchThemAll.Scripts.UI (+1 more)

### Community 50 - "LevelEditorWindow"
Cohesion: 0.12
Nodes (10): DeletedItemRecordList, Color, index, level, LevelDataSO, levelName, Texture2D, LevelEditorWindow (+2 more)

### Community 51 - "LevelDataSO"
Cohesion: 0.10
Nodes (16): LevelDataSO, MatchThemAll.Scripts, RewardCalculationMode, int, ItemLevelData, List, Sprite, string (+8 more)

### Community 52 - "SettingsManager"
Cohesion: 0.11
Nodes (8): hapticsEnabled, MatchThemAll.Scripts.UI, SettingsManager, musicVolume, Slider, SettingsManager, sfxVolume, Toggle

### Community 53 - "WinPanelManager"
Cohesion: 0.24
Nodes (4): MatchThemAll.Scripts.UI, WinPanelManager, float, GameObject

### Community 54 - "PowerupDataSO"
Cohesion: 0.12
Nodes (16): MatchThemAll.Scripts.Power_Ups, PowerupDataSO, ECurrency, GameObject, int, ParticleSystem, PowerupEffect, Sprite (+8 more)

### Community 55 - "DailyRewardManager"
Cohesion: 0.29
Nodes (4): DailyRewardManager, MatchThemAll.Scripts.UI, DailyRewardPanel, Transform

### Community 56 - "EventBus"
Cohesion: 0.27
Nodes (5): EventBus, MatchThemAll.Scripts, Action, Dictionary, T

### Community 57 - "TutorialStep"
Cohesion: 0.22
Nodes (8): ECompletionCondition, EHighlightTarget, bool, float, List, string, MatchThemAll.Scripts.Tutorial, TutorialStep

### Community 58 - "ItemSpot"
Cohesion: 0.20
Nodes (5): ItemSpot, MatchThemAll.Scripts, Animator, Item, Transform

### Community 59 - "ShopDatabaseSO"
Cohesion: 0.12
Nodes (11): IEnumerable, IReadOnlyList, List, ShopProductSO, ShopDatabaseSO, OrderedTabs, ShopTabSO, DisplayName (+3 more)

### Community 60 - "LevelButtonUI"
Cohesion: 0.25
Nodes (5): bool, Button, int, LevelButtonUI, MatchThemAll.Scripts.UI

### Community 61 - "PowerupDatabaseSetup"
Cohesion: 0.25
Nodes (6): MatchThemAll.Scripts.Editor, PowerupDatabaseSetup, MenuItem, PowerupDataSO, PowerupEffect, string

### Community 62 - "PrefabConsolidator"
Cohesion: 0.16
Nodes (7): destFolder, MatchThemAll.Scripts.Editor, PrefabConsolidator, MenuItem, string, PrefabConsolidator, src

### Community 63 - "UIManager"
Cohesion: 0.28
Nodes (4): MatchThemAll.Scripts, UIManager, GameObject, GameStateChangedEvent

### Community 64 - "Match Them All - Complete Game Template"
Cohesion: 0.22
Nodes (8): License, Match Them All - Complete Game Template, Next Steps, Package Structure, Quick Start, Requirements, Support, What's Included

### Community 65 - "MainMenuManager"
Cohesion: 0.15
Nodes (4): MainMenuManager, MatchThemAll.Scripts.UI, GameObject, MainMenuManager

### Community 66 - "GemDisplay"
Cohesion: 0.16
Nodes (4): TextMeshProUGUI, GemDisplay, GemDisplay, MatchThemAll.Scripts.Shop

### Community 67 - "WatchAdForCoinsButton"
Cohesion: 0.29
Nodes (3): int, MatchThemAll.Scripts.Shop, WatchAdForCoinsButton

### Community 68 - "CoinDisplay"
Cohesion: 0.18
Nodes (4): TextMeshProUGUI, CoinDisplay, CoinDisplay, MatchThemAll.Scripts.UI

### Community 69 - "SoundDataSO"
Cohesion: 0.22
Nodes (7): AudioClip, AudioMixerGroup, MatchThemAll.Scripts, SoundDataSO, bool, float, SoundDataSO

### Community 70 - "SceneLoader"
Cohesion: 0.38
Nodes (3): MatchThemAll.Scripts, SceneLoader, string

### Community 71 - "MonoBehaviour"
Cohesion: 0.11
Nodes (9): MatchSystem, MatchThemAll.Scripts, ItemReachedSpotEvent, LosePanelManager, MatchThemAll.Scripts.UI, MonoBehaviour, MatchThemAll.Scripts.Shop, ShopOpener (+1 more)

### Community 72 - "LevelMapNode"
Cohesion: 0.29
Nodes (5): LevelButtonUI, Image, LevelDataSO, LevelMapNode, MatchThemAll.Scripts.UI

### Community 73 - "PixelizeController"
Cohesion: 0.29
Nodes (3): MatchThemAll.Scripts.Pixelate, PixelizeController, GameStateChangedEvent

### Community 74 - "ScriptableObject"
Cohesion: 0.17
Nodes (9): MatchThemAll.Scripts.Power_Ups, PowerupDatabaseSO, ScriptableObject, List, PowerupDataSO, bool, int, GameSettingsSO (+1 more)

### Community 75 - "SaveManagerBootstrapper"
Cohesion: 0.17
Nodes (3): MatchThemAll.Scripts.SaveSystem, SaveManagerBootstrapper, RuntimeInitializeOnLoadMethod

### Community 76 - "ComboVFX"
Cohesion: 0.29
Nodes (3): FloatingText, ComboVFX, MatchThemAll.Scripts.UI

### Community 77 - "FloatingTextTester"
Cohesion: 0.18
Nodes (6): MatchThemAll.Scripts.Testing, Color, string, FloatingTextTester, FloatingTextTester, MatchThemAll.Scripts.Testing

### Community 78 - ".OwnsEntitlement"
Cohesion: 0.11
Nodes (6): WatchAdForCoinsButton, Action, AdManagerMock, Instance, AdManagerMock, MatchThemAll.Scripts.Utilities

### Community 79 - "[1.0.0] - 2026-09-07"
Cohesion: 0.33
Nodes (5): [1.0.0] - 2026-09-07, Added, Changelog, Features, Technical

### Community 80 - ".DrawHighlight"
Cohesion: 0.31
Nodes (4): HierarchySectionHeader, GameObject, Rect, HierarchySectionHeader

### Community 81 - "Transform"
Cohesion: 0.22
Nodes (5): MatchThemAll.Scripts.Extensions, MatchThemAll.Scripts.Extensions, TransformExtensions, Transform, TransformExtensions

### Community 82 - "PowerupEffect"
Cohesion: 0.40
Nodes (3): MatchThemAll.Scripts.Power_Ups, PowerupEffect, PowerupContext

### Community 83 - "DebugGrantCurrencyButton"
Cohesion: 0.33
Nodes (3): int, DebugGrantCurrencyButton, MatchThemAll.Scripts.Shop

### Community 84 - "ShopTabSO"
Cohesion: 0.33
Nodes (5): int, Sprite, string, MatchThemAll.Scripts.Shop, ShopTabSO

### Community 85 - "PowerupManager"
Cohesion: 0.11
Nodes (7): PowerupClickedEvent, IEnumerable, PowerupDatabaseSO, Ordered, PowerupManager, Instance, Vacuum

### Community 86 - "👋 Start Here — Match Them All Customization Guide"
Cohesion: 0.33
Nodes (5): 🎮 How to add a new item type, 🛠 How to create or edit a level, 📐 Project Architecture (brief), 👋 Start Here — Match Them All Customization Guide, 📂 What's in this folder

### Community 87 - "ItemManagerWindow"
Cohesion: 0.14
Nodes (7): PreviewRenderUtility, GUIStyle, List, Rect, Renderer, Vector2, ItemManagerWindow

### Community 88 - "EventBus"
Cohesion: 0.14
Nodes (8): Delegate, List, EventBus, RuntimeInitializeOnLoadMethod, StaticEventCleaner, Type, MatchThemAll.Scripts.Utilities, StaticEventCleaner

### Community 89 - "Scripts/Runtime/Shop/EntitlementIds.cs"
Cohesion: 0.50
Nodes (3): string, EntitlementIds, MatchThemAll.Scripts.Shop

### Community 96 - ".BuildPanel"
Cohesion: 0.18
Nodes (12): ContentSizeFitter, HorizontalLayoutGroup, LayoutElement, Button, Canvas, GameObject, Image, RectMask2D (+4 more)

### Community 97 - "TutorialManager"
Cohesion: 0.15
Nodes (11): Coroutine, CanvasGroup, GameObject, GoalCard, Item, Level, Powerup, TextMeshProUGUI (+3 more)

### Community 98 - "TutorialStep"
Cohesion: 0.10
Nodes (16): MatchThemAll.Scripts.Managers, MatchThemAll.Scripts.Tutorial, EItemName, GameObject, ECompletionCondition, Manual, OnMerge, OnPowerupUsed (+8 more)

### Community 99 - "GameStateChangedEvent"
Cohesion: 0.15
Nodes (4): GameStateChangedEvent, PixelizeController, UIAnimator, UIManager

### Community 100 - "LevelMapManager"
Cohesion: 0.19
Nodes (5): Image, LevelDataSO, RectTransform, ScrollRect, LevelMapManager

### Community 101 - "SaveManager"
Cohesion: 0.18
Nodes (4): IReadOnlyList, SaveManager, Data, LiveOrderedIds

### Community 102 - "MatchThemAll.Scripts"
Cohesion: 0.15
Nodes (6): MatchThemAll.Scripts.Settings, MatchThemAll.Scripts, MatchThemAll.Scripts.Power_Ups, Fan, FreezeGun, Spring

### Community 103 - "GoalManager"
Cohesion: 0.15
Nodes (5): List, Transform, GoalManager, Goals, ItemLevelData

### Community 104 - "ShopPanel"
Cohesion: 0.15
Nodes (7): ShopOpener, GameObject, List, Transform, ShopPanel, IsOpen, TMP_Text

### Community 105 - ".Load"
Cohesion: 0.13
Nodes (6): Canvas, Image, SceneLoader, RequestedLevelIndex, TargetScene, LosePanelManager

### Community 106 - "ShopProductSO"
Cohesion: 0.15
Nodes (12): ShopPurchaseSucceededEvent, ECurrency, Coins, Gems, IReadOnlyList, ShopProductSO, DisplayName, FirstPurchaseBonus (+4 more)

### Community 107 - "GameManager"
Cohesion: 0.18
Nodes (4): SpotFilledEvent, GameManager, PreviousState, State

### Community 108 - ".Build"
Cohesion: 0.19
Nodes (11): CanvasScaler, GraphicRaycaster, Canvas, EventSystem, Image, InputSystemUIInputModule, RectMask2D, RectTransform (+3 more)

### Community 109 - ".Flush"
Cohesion: 0.19
Nodes (4): lastPlayedDate, loginStreak, DailyRewardManager, SaveManagerBootstrapper

### Community 110 - "ItemPlacer"
Cohesion: 0.21
Nodes (4): BoxCollider, List, Task, ItemPlacer

### Community 111 - "PowerupContext"
Cohesion: 0.17
Nodes (8): FanEffect, FreezeEffect, Action, List, ParticleSystem, Transform, PowerupContext, PowerupEffect

### Community 112 - "SoundManager"
Cohesion: 0.20
Nodes (4): AudioMixer, AudioSource, SoundManager, Instance

### Community 113 - "MatchThemAll.Scripts.Shop"
Cohesion: 0.14
Nodes (4): MatchThemAll.Scripts.Shop, MatchThemAll.Scripts.Editor, DebugGrantCurrencyButton, EntitlementIds

### Community 114 - ".GenerateItemPrefab"
Cohesion: 0.19
Nodes (7): MeshCollider, MeshFilter, BoxCollider, Collider, GameObject, Rigidbody, Sprite

### Community 115 - ".DrawLevelDetail"
Cohesion: 0.35
Nodes (3): GameObject, Item, ItemPlacer

### Community 116 - "EItemName"
Cohesion: 0.15
Nodes (10): List, EItemName, Battery, BluePotion, Bomb, Clock, Coin, Diamond (+2 more)

### Community 117 - "List"
Cohesion: 0.18
Nodes (12): DeletedItemRecord, DeletedItemRecord, DeletedItemRecordList, MatchThemAll.Scripts.Editor, RemovedLevelEntry, RemovedLevelEntry, bool, int (+4 more)

### Community 118 - "ShopSetup"
Cohesion: 0.26
Nodes (6): EKind, EventSystem, InputSystemUIInputModule, ShopSetup, TmpFont, TMP_FontAsset

### Community 119 - "PlayerData"
Cohesion: 0.24
Nodes (3): LevelProgressEntry, PlayerData, PowerupSaveEntry

### Community 120 - "MergeStartedEvent"
Cohesion: 0.19
Nodes (6): List, MergeStartedEvent, Action, ComboManager, CurrentCombo, Instance

### Community 121 - "MatchThemAll.Scripts.UI"
Cohesion: 0.25
Nodes (3): MatchThemAll.Scripts.SaveSystem, MatchThemAll.Scripts.Utilities, MatchThemAll.Scripts.UI

### Community 122 - ".CreateDefaultProducts"
Cohesion: 0.36
Nodes (4): ECurrency, EKind, MenuItem, ShopProductSO

### Community 123 - "Level"
Cohesion: 0.20
Nodes (7): List, Task, Transform, Level, Duration, ItemParent, SpotCount

### Community 124 - "LevelButtonUI"
Cohesion: 0.24
Nodes (5): GameObject, TextMeshProUGUI, LevelButtonUI, LevelMapNode, Button

### Community 125 - "Scripts/Runtime/PowerUps/PowerupManager.cs"
Cohesion: 0.42
Nodes (7): DebugActivateFan(), DebugActivateFreeze(), DebugActivateSpring(), DebugActivateVacuum(), ForceActivate(), MatchThemAll.Scripts, Button

### Community 126 - "SpringEffect"
Cohesion: 0.33
Nodes (5): DrawGizmos(), DrawTrajectoryGizmo(), MatchThemAll.Scripts.Power_Ups, Vector3, SpringEffect

### Community 127 - ".OnDisable"
Cohesion: 0.28
Nodes (3): ItemClickedEvent, MergeStartedEvent, PowerupClickedEvent

### Community 128 - "EGameState"
Cohesion: 0.25
Nodes (7): EGameState, GAME, GAMEOVER, LEVELCOMPLETE, MENU, OUTOFTIME, PAUSED

### Community 130 - "FanEffect"
Cohesion: 0.33
Nodes (4): FanEffect, MatchThemAll.Scripts.Power_Ups, float, PowerupContext

### Community 134 - "FreezeEffect"
Cohesion: 0.40
Nodes (3): FreezeEffect, MatchThemAll.Scripts.Power_Ups, PowerupContext

### Community 135 - "SpringEffect"
Cohesion: 0.40
Nodes (4): SpringEffect, float, PowerupContext, Vector2

### Community 138 - "EKind"
Cohesion: 0.40
Nodes (5): EKind, Coins, Entitlement, Gems, PowerupCharge

## Knowledge Gaps
- **424 isolated node(s):** `TmpFont`, `LevelSpawnedEvent`, `State`, `PreviousState`, `Instance` (+419 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 690 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Item` connect `Item` to `.Publish`, `GoalManager`, `MonoBehaviour`, `.Activate`, `ItemSpotManager`, `VacuumEffect`, `Item`, `ItemPlacer`, `InputManager`, `LevelManager`, `PowerupContext`, `.GenerateItemPrefab`, `MergeManager`, `EItemName`, `PowerupManager`, `ItemManagerWindow`, `MergeStartedEvent`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Why does `LevelEditorWindow` connect `LevelEditorWindow` to `LevelEditorWindow`, `TutorialStep`, `ShopEditorWindow`, `EditorWindowStyles`, `.DrawLevelDetail`, `List`, `.RemoveFromLevels`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **Why does `GameSettingsSO` connect `LevelEditorWindow` to `MatchThemAll.Scripts`, `ScriptableObject`, `PowerupContext`, `PowerupManager`, `ContinuePanelManager`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **What connects `TmpFont`, `LevelSpawnedEvent`, `State` to the rest of the system?**
  _424 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `LevelEditorWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.10253699788583509 - nodes in this community are weakly interconnected._
- **Should `ItemManagerWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.11827956989247312 - nodes in this community are weakly interconnected._
- **Should `SaveManager` be split into smaller, more focused modules?**
  _Cohesion score 0.09102564102564102 - nodes in this community are weakly interconnected._