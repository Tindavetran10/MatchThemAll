# Graph Report - MatchThemAllTemplate  (2026-09-17)

## Corpus Check
- 237 files · ~203,771 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 599 file(s) not represented in the graph (top: .meta 481, .asset 42, .prefab 36)

## Summary
- 3344 nodes · 5860 edges · 220 communities (195 shown, 25 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 118 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `eacbd8fb`
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
- MonoBehaviour
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
- MatchSystem
- NaughtyAttributes.Editor
- PixelizeController
- ScriptableObject
- PlayerData
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
- ECompletionCondition
- GameStateChangedEvent
- LevelMapManager
- SaveManager
- MatchThemAll.Scripts.Power_Ups
- GoalManager
- ShopPanel
- .Load
- ShopProductSO
- GameManager
- .Build
- DailyRewardManager
- ItemPlacer
- PowerupContext
- SoundManager
- MatchThemAll.Scripts.UI
- .GenerateItemPrefab
- .LoadAll
- EItemName
- List
- .EnsureEventSystem
- NaughtyAttributes
- MergeStartedEvent
- .GetTargetObjectWithProperty
- .GetPropertyHeight
- Level
- LevelButtonUI
- Scripts/Runtime/PowerUps/PowerupManager.cs
- SpringEffect
- .OnDisable
- EColor
- .DrawDockPreview
- FanEffect
- ItemReachedSpotEvent
- ShopManager
- Texture2D
- FreezeEffect
- .GetAttribute
- .Activate
- ReorderableListPropertyDrawer
- EKind
- .GetHelpBoxHeight
- .Start
- DisableIfEnumFlag
- .ShowWindow
- .ResetSessionStatics
- Scripts/Runtime/PowerUps/FreezeGun.cs
- Scripts/Runtime/PowerUps/Spring.cs
- Scripts/Runtime/PowerUps/Vacuum.cs
- EnableIfEnumFlag
- HideIfEnumFlag
- ShowIfEnumFlag
- ShowNativePropertyTest
- InputManager
- NaughtyInspector
- LevelManager
- MetaAttribute
- NaughtyAttributes.Test
- ValidatorAttribute
- NaughtyEditorGUI
- EnableIfAttributeBase
- .OnGUI_Internal
- .OnGUI_Internal
- package.json
- MinMaxValueTest
- .DrawSerializedProperties
- EInfoBoxType
- ScenePropertyDrawer
- TestEnum
- SpecialCaseDrawerAttribute
- DropdownList
- .PropertyField_Implementation
- ShowIfAttributeBase
- .IsEnabled
- LayerPropertyDrawer
- SortingLayerPropertyDrawer
- MatchThemAll.Scripts
- DropdownNest1
- ResizableTextAreaPropertyDrawer
- AnimatorParamNest1
- InfoBoxDecoratorDrawer
- ButtonAttribute
- ButtonTest
- OnValueChangedTest
- ValidateInputTest.cs
- CurveRangeTest
- Adding a New Power-Up
- GemDisplay
- FilePathAttribute
- .GetPropertyHeight_Internal
- .GetPropertyHeight_Internal
- .GetPropertyHeight_Internal
- CoinDisplay
- HorizontalLineDecoratorDrawer
- Code Style & Contribution Guidelines
- Common Mistakes & Troubleshooting
- Level Creation Tutorial
- App Store & Google Play Publishing Checklist
- Shop & Economy Configuration
- EHighlightTarget
- ShopOpener
- .OnGUI_Internal
- InputAxisTest.cs
- LayerTest.cs
- MinMaxSliderTest.cs
- _NaughtyScriptableObject
- ReorderableListTest
- ShowAssetPreviewNest1
- SortingLayerTest.cs
- TagTest.cs
- GameSettingsSO
- FolderPathAttribute
- ExpandableTest.cs
- LabelTest.cs
- .OnLevelSpawned
- FolderPathTest.cs
- HorizontalLineTest.cs
- InfoBoxTest.cs
- ProgressBarTest.cs
- ReadOnlyTest.cs
- ResizableTextAreaTest.cs
- SavedBool

## God Nodes (most connected - your core abstractions)
1. `Item` - 76 edges
2. `NaughtyAttributes` - 56 edges
3. `LevelEditorWindow` - 54 edges
4. `LevelEditorWindow` - 49 edges
5. `ItemManagerWindow` - 47 edges
6. `ItemManagerWindow` - 42 edges
7. `NaughtyAttributes.Test` - 41 edges
8. `SaveManager` - 39 edges
9. `SaveManager` - 38 edges
10. `MatchThemAll.Scripts` - 37 edges

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

## Communities (220 total, 25 thin omitted)

### Community 0 - "LevelEditorWindow"
Cohesion: 0.13
Nodes (3): LevelEditorWindow, float, GameSettingsSO

### Community 1 - "ItemManagerWindow"
Cohesion: 0.12
Nodes (8): double, ItemManagerWindow, MatchThemAll.Scripts.Editor, bool, EItemName, float, int, string

### Community 2 - "SaveManager"
Cohesion: 0.09
Nodes (7): PlayerData, MatchThemAll.Scripts.SaveSystem, SaveManager, bool, ECurrency, PowerupDatabaseSO, string

### Community 3 - "ItemSpotManager"
Cohesion: 0.11
Nodes (11): ItemSpotManager, MatchThemAll.Scripts, EItemName, float, IEnumerable, int, Item, ItemClickedEvent (+3 more)

### Community 4 - "ShopEditorWindow"
Cohesion: 0.06
Nodes (17): MatchThemAll.Scripts.Editor, ShopEditorWindow, EditorWindow, bool, Color, float, GUIStyle, int (+9 more)

### Community 5 - "VacuumEffect"
Cohesion: 0.27
Nodes (5): MatchThemAll.Scripts.Power_Ups, VacuumEffect, Item, ItemLevelData, PowerupContext

### Community 6 - "ShopSetup"
Cohesion: 0.15
Nodes (8): MatchThemAll.Scripts.Editor, ShopSetup, ECurrency, EKind, MenuItem, ShopProductCard, ShopProductSO, string

### Community 7 - "LevelMapManager"
Cohesion: 0.11
Nodes (12): LevelMapPath, LevelMapManager, MatchThemAll.Scripts.UI, Button, float, IEnumerator, IReadOnlyList, LevelMapNode (+4 more)

### Community 8 - "TutorialManager"
Cohesion: 0.16
Nodes (8): Coroutine, CanvasGroup, int, List, TextMeshProUGUI, TutorialStep, MatchThemAll.Scripts.Managers, TutorialManager

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
Cohesion: 0.10
Nodes (13): DebugRunOutOfTime(), MatchThemAll.Scripts, TimerManager, bool, Button, GameStateChangedEvent, IEnumerator, Level (+5 more)

### Community 14 - "ItemPlacer"
Cohesion: 0.17
Nodes (10): BoxCollider, ItemPlacer, MatchThemAll.Scripts, PreviewSpawn(), PreviewSpawnFromEditor(), PreviewSpawnWithData(), Button, Item (+2 more)

### Community 15 - "InputManager"
Cohesion: 0.15
Nodes (9): Camera, LayerMask, MeshRenderer, Collider, Vector2, InputManager, Instance, IsPointerActive (+1 more)

### Community 16 - "LevelManager"
Cohesion: 0.10
Nodes (14): Button, IReadOnlyList, List, Transform, LevelManager, CurrentLevelData, CurrentLevelIndex, IsLevelReady (+6 more)

### Community 17 - "GoalManager"
Cohesion: 0.14
Nodes (7): GoalManager, MatchThemAll.Scripts, GoalCard, int, Item, ItemLevelData, Level

### Community 18 - "MergeManager"
Cohesion: 0.12
Nodes (12): MatchThemAll.Scripts, MergeManager, Ease, float, IEnumerator, int, IObjectPool, Item (+4 more)

### Community 19 - "ShopPanel"
Cohesion: 0.16
Nodes (7): Button, ShopDatabaseSO, ShopProductCard, string, MatchThemAll.Scripts.Shop, ShopPanel, UIAnimator

### Community 20 - "ItemSpotLayout"
Cohesion: 0.10
Nodes (19): Axis, ContextMenu, ItemSpotLayout, MatchThemAll.Scripts, LayoutMode, bool, float, List (+11 more)

### Community 21 - ".RemoveFromLevels"
Cohesion: 0.14
Nodes (17): ItemReferenceOps, MatchThemAll.Scripts.Editor, entry, IList, index, levelName, GameObject, index (+9 more)

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
Cohesion: 0.13
Nodes (10): IIapService, MatchThemAll.Scripts.Shop, NullIapService, Action, IIapService, IsInitialized, NullIapService, IsInitialized (+2 more)

### Community 31 - "LoadingScreenManager"
Cohesion: 0.15
Nodes (12): AsyncOperation, LoadingScreenManager, MatchThemAll.Scripts, Canvas, float, GameObject, IEnumerator, Image (+4 more)

### Community 32 - "LevelMapBuilder"
Cohesion: 0.24
Nodes (6): LevelMapBuilder, MatchThemAll.Scripts.Editor, GameObject, LevelMapNode, MenuItem, string

### Community 33 - "ComboManager"
Cohesion: 0.16
Nodes (7): ComboManager, MatchThemAll.Scripts, Action, float, int, Level, MergeStartedEvent

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
Cohesion: 0.12
Nodes (8): ItemSpot, Item, Action, Ease, Transform, Vector3, ItemSpotManager, Instance

### Community 43 - "DebugCheats"
Cohesion: 0.15
Nodes (6): DebugCheats, MatchThemAll.Scripts, Button, int, RuntimeInitializeOnLoadMethod, DebugCheats

### Community 44 - "MonoBehaviour"
Cohesion: 0.10
Nodes (14): MatchThemAll.Scripts, OpenSceneLoader, LosePanelManager, MatchThemAll.Scripts.UI, MonoBehaviour, float, IEnumerator, OpenSceneLoader (+6 more)

### Community 45 - "Customization Guide"
Cohesion: 0.06
Nodes (27): 1. Decoupled Communication via EventBus, 2. Core Managers & Lifecycles, 3. High Performance with ZLinq, 4. Asset Organization & Namespaces, Architecture Overview, Key Events, Adding a New Item Type, Adding a New Power-Up (+19 more)

### Community 46 - "EditorWindowStyles"
Cohesion: 0.20
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
Cohesion: 0.10
Nodes (13): DeletedItemRecordList, Color, Editor, GUIStyle, index, ItemPlacer, level, levelName (+5 more)

### Community 51 - "LevelDataSO"
Cohesion: 0.10
Nodes (16): LevelDataSO, MatchThemAll.Scripts, RewardCalculationMode, int, ItemLevelData, List, Sprite, string (+8 more)

### Community 52 - "SettingsManager"
Cohesion: 0.09
Nodes (9): hapticsEnabled, MatchThemAll.Scripts.UI, SettingsManager, musicVolume, MainMenuManager, Slider, SettingsManager, sfxVolume (+1 more)

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
Cohesion: 0.18
Nodes (11): ECompletionCondition, EHighlightTarget, bool, EItemName, float, GameObject, List, string (+3 more)

### Community 58 - "ItemSpot"
Cohesion: 0.20
Nodes (5): ItemSpot, MatchThemAll.Scripts, Animator, Item, Transform

### Community 59 - "ShopDatabaseSO"
Cohesion: 0.25
Nodes (5): List, ShopProductSO, MatchThemAll.Scripts.Shop, ShopDatabaseSO, ShopTabSO

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
Cohesion: 0.25
Nodes (3): MainMenuManager, MatchThemAll.Scripts.UI, GameObject

### Community 67 - "WatchAdForCoinsButton"
Cohesion: 0.29
Nodes (3): int, MatchThemAll.Scripts.Shop, WatchAdForCoinsButton

### Community 69 - "SoundDataSO"
Cohesion: 0.22
Nodes (7): AudioClip, AudioMixerGroup, MatchThemAll.Scripts, SoundDataSO, bool, float, SoundDataSO

### Community 70 - "SceneLoader"
Cohesion: 0.38
Nodes (3): MatchThemAll.Scripts, SceneLoader, string

### Community 71 - "MatchSystem"
Cohesion: 0.29
Nodes (3): MatchSystem, MatchThemAll.Scripts, ItemReachedSpotEvent

### Community 72 - "NaughtyAttributes.Editor"
Cohesion: 0.05
Nodes (22): NaughtyAttributes.Editor, MaxValueAttribute, MinValueAttribute, RequiredAttribute, RequiredTypeAttribute, SerializedProperty, MaxValuePropertyValidator, SerializedProperty (+14 more)

### Community 73 - "PixelizeController"
Cohesion: 0.29
Nodes (3): MatchThemAll.Scripts.Pixelate, PixelizeController, GameStateChangedEvent

### Community 74 - "ScriptableObject"
Cohesion: 0.17
Nodes (9): MatchThemAll.Scripts.Power_Ups, PowerupDatabaseSO, ScriptableObject, List, PowerupDataSO, List, _TestScriptableObjectA, Vector2Int (+1 more)

### Community 75 - "PlayerData"
Cohesion: 0.13
Nodes (6): MatchThemAll.Scripts.SaveSystem, SaveManagerBootstrapper, LevelProgressEntry, PlayerData, PowerupSaveEntry, RuntimeInitializeOnLoadMethod

### Community 76 - "ComboVFX"
Cohesion: 0.29
Nodes (3): FloatingText, ComboVFX, MatchThemAll.Scripts.UI

### Community 77 - "FloatingTextTester"
Cohesion: 0.18
Nodes (6): MatchThemAll.Scripts.Testing, Color, string, FloatingTextTester, FloatingTextTester, MatchThemAll.Scripts.Testing

### Community 78 - ".OwnsEntitlement"
Cohesion: 0.12
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
Cohesion: 0.22
Nodes (7): int, Sprite, string, ShopTabSO, DisplayName, MatchThemAll.Scripts.Shop, ShopTabSO

### Community 85 - "PowerupManager"
Cohesion: 0.13
Nodes (6): PowerupClickedEvent, IEnumerable, PowerupDatabaseSO, Ordered, PowerupManager, Instance

### Community 86 - "👋 Start Here — Match Them All Customization Guide"
Cohesion: 0.33
Nodes (5): 🎮 How to add a new item type, 🛠 How to create or edit a level, 📐 Project Architecture (brief), 👋 Start Here — Match Them All Customization Guide, 📂 What's in this folder

### Community 87 - "ItemManagerWindow"
Cohesion: 0.14
Nodes (7): PreviewRenderUtility, GUIStyle, List, Rect, Renderer, Vector2, ItemManagerWindow

### Community 88 - "EventBus"
Cohesion: 0.14
Nodes (8): Delegate, List, Type, EventBus, RuntimeInitializeOnLoadMethod, StaticEventCleaner, MatchThemAll.Scripts.Utilities, StaticEventCleaner

### Community 89 - "Scripts/Runtime/Shop/EntitlementIds.cs"
Cohesion: 0.50
Nodes (3): string, EntitlementIds, MatchThemAll.Scripts.Shop

### Community 96 - ".BuildPanel"
Cohesion: 0.16
Nodes (13): ContentSizeFitter, HorizontalLayoutGroup, LayoutElement, Button, Canvas, Color, GameObject, Image (+5 more)

### Community 97 - "TutorialManager"
Cohesion: 0.22
Nodes (7): GameObject, GoalCard, IEnumerator, Item, Powerup, TutorialManager, IsTutorialRunning

### Community 98 - "ECompletionCondition"
Cohesion: 0.18
Nodes (7): MatchThemAll.Scripts.Managers, MatchThemAll.Scripts.Tutorial, ECompletionCondition, Manual, OnMerge, OnPowerupUsed, OnTimer

### Community 99 - "GameStateChangedEvent"
Cohesion: 0.16
Nodes (4): GameStateChangedEvent, PixelizeController, UIAnimator, UIManager

### Community 100 - "LevelMapManager"
Cohesion: 0.19
Nodes (5): Image, LevelDataSO, RectTransform, ScrollRect, LevelMapManager

### Community 101 - "SaveManager"
Cohesion: 0.09
Nodes (5): IReadOnlyList, SaveManager, Data, LiveOrderedIds, SaveManagerBootstrapper

### Community 102 - "MatchThemAll.Scripts.Power_Ups"
Cohesion: 0.13
Nodes (6): MatchThemAll.Scripts.Settings, MatchThemAll.Scripts.Power_Ups, Fan, FreezeGun, Spring, Vacuum

### Community 103 - "GoalManager"
Cohesion: 0.16
Nodes (5): List, Transform, GoalManager, Goals, ItemLevelData

### Community 104 - "ShopPanel"
Cohesion: 0.15
Nodes (7): ShopOpener, GameObject, List, Transform, ShopPanel, IsOpen, TMP_Text

### Community 105 - ".Load"
Cohesion: 0.11
Nodes (7): Canvas, Image, SceneLoader, RequestedLevelIndex, TargetScene, LosePanelManager, WinPanelManager

### Community 106 - "ShopProductSO"
Cohesion: 0.09
Nodes (21): EKind, ShopSetup, TmpFont, ShopPurchaseSucceededEvent, ECurrency, Coins, Gems, IEnumerable (+13 more)

### Community 107 - "GameManager"
Cohesion: 0.11
Nodes (11): SpotFilledEvent, GameManager, PreviousState, State, EGameState, GAME, GAMEOVER, LEVELCOMPLETE (+3 more)

### Community 108 - ".Build"
Cohesion: 0.16
Nodes (12): CanvasScaler, GraphicRaycaster, Canvas, EventSystem, Image, InputSystemUIInputModule, RectMask2D, RectTransform (+4 more)

### Community 109 - "DailyRewardManager"
Cohesion: 0.33
Nodes (3): lastPlayedDate, loginStreak, DailyRewardManager

### Community 110 - "ItemPlacer"
Cohesion: 0.19
Nodes (5): BoxCollider, List, Task, Vector3, ItemPlacer

### Community 111 - "PowerupContext"
Cohesion: 0.11
Nodes (11): PowerupDatabaseSetup, FanEffect, FreezeEffect, Action, List, ParticleSystem, Transform, PowerupContext (+3 more)

### Community 112 - "SoundManager"
Cohesion: 0.20
Nodes (4): AudioMixer, AudioSource, SoundManager, Instance

### Community 113 - "MatchThemAll.Scripts.UI"
Cohesion: 0.09
Nodes (7): MatchThemAll.Scripts.Shop, MatchThemAll.Scripts.SaveSystem, MatchThemAll.Scripts.Utilities, MatchThemAll.Scripts.Editor, MatchThemAll.Scripts.UI, DebugGrantCurrencyButton, EntitlementIds

### Community 114 - ".GenerateItemPrefab"
Cohesion: 0.19
Nodes (7): MeshCollider, MeshFilter, BoxCollider, Collider, GameObject, Rigidbody, Sprite

### Community 115 - ".LoadAll"
Cohesion: 0.27
Nodes (4): GameObject, Item, LevelDataSO, Texture

### Community 116 - "EItemName"
Cohesion: 0.17
Nodes (10): List, EItemName, Battery, BluePotion, Bomb, Clock, Coin, Diamond (+2 more)

### Community 117 - "List"
Cohesion: 0.16
Nodes (12): DeletedItemRecord, DeletedItemRecord, DeletedItemRecordList, MatchThemAll.Scripts.Editor, RemovedLevelEntry, RemovedLevelEntry, bool, int (+4 more)

### Community 119 - "NaughtyAttributes"
Cohesion: 0.07
Nodes (23): AnimatorControllerParameterType, NaughtyAttributes, PropertyAttribute, AllowNestingAttribute, AnimatorParamAttribute, AnimatorName, AnimatorParamType, DrawerAttribute (+15 more)

### Community 120 - "MergeStartedEvent"
Cohesion: 0.15
Nodes (6): List, ItemClickedEvent, MergeStartedEvent, ComboManager, CurrentCombo, Instance

### Community 121 - ".GetTargetObjectWithProperty"
Cohesion: 0.12
Nodes (17): Func, FieldInfo, GUIContent, Rect, SerializedProperty, Type, DropdownPropertyDrawer, SerializedProperty (+9 more)

### Community 122 - ".GetPropertyHeight"
Cohesion: 0.10
Nodes (22): PropertyDrawer, ShowAssetPreviewAttribute, GUIContent, Rect, SerializedProperty, InputAxisPropertyDrawer, GUIContent, Rect (+14 more)

### Community 123 - "Level"
Cohesion: 0.20
Nodes (7): List, Task, Transform, Level, Duration, ItemParent, SpotCount

### Community 124 - "LevelButtonUI"
Cohesion: 0.14
Nodes (10): LevelButtonUI, GameObject, TextMeshProUGUI, LevelButtonUI, Image, LevelDataSO, LevelMapNode, Button (+2 more)

### Community 125 - "Scripts/Runtime/PowerUps/PowerupManager.cs"
Cohesion: 0.42
Nodes (7): DebugActivateFan(), DebugActivateFreeze(), DebugActivateSpring(), DebugActivateVacuum(), ForceActivate(), MatchThemAll.Scripts, Button

### Community 126 - "SpringEffect"
Cohesion: 0.17
Nodes (9): DrawGizmos(), DrawTrajectoryGizmo(), MatchThemAll.Scripts.Power_Ups, SpringEffect, float, PowerupContext, Vector2, Vector3 (+1 more)

### Community 127 - ".OnDisable"
Cohesion: 0.28
Nodes (3): ItemClickedEvent, MergeStartedEvent, PowerupClickedEvent

### Community 128 - "EColor"
Cohesion: 0.07
Nodes (27): Vector2, CurveRangeAttribute, Color, Max, Min, HorizontalLineAttribute, Color, Height (+19 more)

### Community 130 - "FanEffect"
Cohesion: 0.33
Nodes (4): FanEffect, MatchThemAll.Scripts.Power_Ups, float, PowerupContext

### Community 132 - "ShopManager"
Cohesion: 0.18
Nodes (7): HashSet, IIapService, Action, ShopProductSO, MatchThemAll.Scripts.Shop, ShopManager, ShopReward

### Community 134 - "FreezeEffect"
Cohesion: 0.40
Nodes (3): FreezeEffect, MatchThemAll.Scripts.Power_Ups, PowerupContext

### Community 135 - ".GetAttribute"
Cohesion: 0.13
Nodes (15): FlagsAttribute, LabelAttribute, OnValueChangedAttribute, ReadOnlyAttribute, GUIContent, Rect, SerializedProperty, ExpandablePropertyDrawer (+7 more)

### Community 137 - "ReorderableListPropertyDrawer"
Cohesion: 0.11
Nodes (16): ReorderableList, Dictionary, GUIContent, GUIStyle, Object, Rect, SerializedProperty, ReorderableListPropertyDrawer (+8 more)

### Community 138 - "EKind"
Cohesion: 0.40
Nodes (5): EKind, Coins, Entitlement, Gems, PowerupCharge

### Community 139 - ".GetHelpBoxHeight"
Cohesion: 0.15
Nodes (11): FilePathAttribute, FolderPathAttribute, GUIContent, Rect, SerializedProperty, FilePathPropertyDrawer, GUIContent, Rect (+3 more)

### Community 141 - "DisableIfEnumFlag"
Cohesion: 0.13
Nodes (17): Vector2, DisableIfEnum, Case0, Case1, Case2, DisableIfEnumFlag, Flag0, Flag1 (+9 more)

### Community 147 - "EnableIfEnumFlag"
Cohesion: 0.13
Nodes (17): Vector2, EnableIfEnum, Case0, Case1, Case2, EnableIfEnumFlag, Flag0, Flag1 (+9 more)

### Community 148 - "HideIfEnumFlag"
Cohesion: 0.13
Nodes (17): Vector2, HideIfEnum, Case0, Case1, Case2, HideIfEnumFlag, Flag0, Flag1 (+9 more)

### Community 149 - "ShowIfEnumFlag"
Cohesion: 0.13
Nodes (17): Vector2, ShowIfEnum, Case0, Case1, Case2, ShowIfEnumFlag, Flag0, Flag1 (+9 more)

### Community 150 - "ShowNativePropertyTest"
Cohesion: 0.12
Nodes (16): Vector2, BoxGroupTest, Vector2, FoldoutTest, RequiredNest1, RequiredNest2, RequiredTest, ShowNativePropertyTest (+8 more)

### Community 151 - "InputManager"
Cohesion: 0.15
Nodes (7): InputManager, MatchThemAll.Scripts, MTAInputSystem_Actions, bool, GameStateChangedEvent, Item, Material

### Community 152 - "NaughtyInspector"
Cohesion: 0.16
Nodes (10): Editor, Dictionary, FieldInfo, GUIStyle, INaughtyAttribute, List, MethodInfo, PropertyInfo (+2 more)

### Community 153 - "LevelManager"
Cohesion: 0.12
Nodes (11): IReadOnlyList<string>, LevelManager, MatchThemAll.Scripts, List<string>, Action, bool, GameStateChangedEvent, int (+3 more)

### Community 154 - "MetaAttribute"
Cohesion: 0.12
Nodes (11): BoxGroupAttribute, Name, FoldoutAttribute, Name, IGroupAttribute, LabelAttribute, Label, MetaAttribute (+3 more)

### Community 155 - "NaughtyAttributes.Test"
Cohesion: 0.15
Nodes (12): NaughtyAttributes.Test, FilePathNest1, FilePathNest2, FilePathTest, GameObject, IRequiredTypeTestInterface, IRequiredTypeTestInterface2, RequiredTypeTest (+4 more)

### Community 156 - "ValidatorAttribute"
Cohesion: 0.12
Nodes (12): MaxValueAttribute, MaxValue, MaxValueName, MinValueAttribute, MinValue, MinValueName, RequiredAttribute, Message (+4 more)

### Community 157 - "NaughtyEditorGUI"
Cohesion: 0.25
Nodes (8): FieldInfo, GUIStyle, MessageType, MethodInfo, Object, PropertyInfo, SerializedObject, NaughtyEditorGUI

### Community 158 - "EnableIfAttributeBase"
Cohesion: 0.13
Nodes (11): DisableIfAttribute, EnableIfAttribute, Enum, EnableIfAttributeBase, ConditionOperator, Conditions, EnumValue, Inverted (+3 more)

### Community 159 - ".OnGUI_Internal"
Cohesion: 0.36
Nodes (8): AnimatorController, AnimatorControllerParameter, AnimatorParamAttribute, GUIContent, List, Rect, SerializedProperty, AnimatorParamPropertyDrawer

### Community 160 - ".OnGUI_Internal"
Cohesion: 0.37
Nodes (6): ProgressBarAttribute, Color, GUIContent, Rect, SerializedProperty, ProgressBarPropertyDrawer

### Community 161 - "package.json"
Cohesion: 0.15
Nodes (12): author, name, url, category, dependencies, description, displayName, keywords (+4 more)

### Community 162 - "MinMaxValueTest"
Cohesion: 0.23
Nodes (10): Vector2, Vector2Int, Vector3, MinMaxValueNest1, MinMaxValueNest2, MinMaxValueTest, MaxFloatProperty, MinFloatProperty (+2 more)

### Community 163 - ".DrawSerializedProperties"
Cohesion: 0.24
Nodes (6): BoxGroupAttribute, FoldoutAttribute, IGrouping, IEnumerable, IGroupAttribute, SerializedProperty

### Community 164 - "EInfoBoxType"
Cohesion: 0.18
Nodes (10): EInfoBoxType, Error, Normal, Warning, InfoBoxAttribute, Text, Type, RequiredTypeAttribute (+2 more)

### Community 165 - "ScenePropertyDrawer"
Cohesion: 0.38
Nodes (4): GUIContent, Rect, SerializedProperty, ScenePropertyDrawer

### Community 166 - "TestEnum"
Cohesion: 0.24
Nodes (11): EnumFlagsNest1, EnumFlagsNest2, EnumFlagsTest, TestEnum, All, B, C, D (+3 more)

### Community 167 - "SpecialCaseDrawerAttribute"
Cohesion: 0.18
Nodes (6): Attribute, ReorderableListAttribute, ShowNativePropertyAttribute, ShowNonSerializedFieldAttribute, SpecialCaseDrawerAttribute, INaughtyAttribute

### Community 168 - "DropdownList"
Cohesion: 0.24
Nodes (8): IEnumerable, KeyValuePair, IEnumerator, List, DropdownAttribute, ValuesName, DropdownList, IDropdownList

### Community 169 - ".PropertyField_Implementation"
Cohesion: 0.29
Nodes (6): PropertyFieldFunction, GUIContent, Rect, SerializedProperty, SpecialCaseDrawerAttribute, ValidatorAttribute

### Community 170 - "ShowIfAttributeBase"
Cohesion: 0.18
Nodes (8): HideIfAttribute, ShowIfAttribute, Enum, ShowIfAttributeBase, ConditionOperator, Conditions, EnumValue, Inverted

### Community 171 - ".IsEnabled"
Cohesion: 0.24
Nodes (7): EnableIfAttributeBase, MethodInfo, Object, ShowIfAttributeBase, ButtonUtility, EConditionOperator, List

### Community 172 - "LayerPropertyDrawer"
Cohesion: 0.47
Nodes (4): GUIContent, Rect, SerializedProperty, LayerPropertyDrawer

### Community 173 - "SortingLayerPropertyDrawer"
Cohesion: 0.47
Nodes (4): GUIContent, Rect, SerializedProperty, SortingLayerPropertyDrawer

### Community 175 - "DropdownNest1"
Cohesion: 0.31
Nodes (7): DropdownList, List, Vector3, DropdownNest1, StringValues, DropdownNest2, DropdownTest

### Community 176 - "ResizableTextAreaPropertyDrawer"
Cohesion: 0.36
Nodes (4): GUIContent, Rect, SerializedProperty, ResizableTextAreaPropertyDrawer

### Community 177 - "AnimatorParamNest1"
Cohesion: 0.36
Nodes (6): Animator, Button, AnimatorParamNest1, Animator1, AnimatorParamNest2, AnimatorParamTest

### Community 178 - "InfoBoxDecoratorDrawer"
Cohesion: 0.39
Nodes (3): EInfoBoxType, Rect, InfoBoxDecoratorDrawer

### Community 179 - "ButtonAttribute"
Cohesion: 0.29
Nodes (7): ButtonAttribute, SelectedEnableMode, Text, EButtonEnableMode, Always, Editor, Playmode

### Community 180 - "ButtonTest"
Cohesion: 0.36
Nodes (3): Button, IEnumerator, ButtonTest

### Community 181 - "OnValueChangedTest"
Cohesion: 0.32
Nodes (3): OnValueChangedNest1, OnValueChangedNest2, OnValueChangedTest

### Community 182 - "ValidateInputTest.cs"
Cohesion: 0.39
Nodes (4): ValidateInputInheritedNest, ValidateInputNest1, ValidateInputNest2, ValidateInputTest

### Community 183 - "CurveRangeTest"
Cohesion: 0.38
Nodes (6): AnimationCurve, CurveRangeNest1, CurveRangeNest2, CurveRangeNest1, CurveRangeNest2, CurveRangeTest

### Community 184 - "Adding a New Power-Up"
Cohesion: 0.29
Nodes (6): 1. Create a Game Action, 2. Define Enum & Name Key, 3. Author the PowerupDataSO, 4. Bind Logic in PowerupManager, 5. Add to Central Database, Adding a New Power-Up

### Community 186 - "FilePathAttribute"
Cohesion: 0.29
Nodes (6): FilePathAttribute, Directory, Filter, RelativePath, Title, ValidateExists

### Community 187 - ".GetPropertyHeight_Internal"
Cohesion: 0.38
Nodes (4): GUIContent, Rect, SerializedProperty, CurveRangePropertyDrawer

### Community 188 - ".GetPropertyHeight_Internal"
Cohesion: 0.38
Nodes (4): GUIContent, Rect, SerializedProperty, EnumFlagsPropertyDrawer

### Community 189 - ".GetPropertyHeight_Internal"
Cohesion: 0.38
Nodes (4): GUIContent, Rect, SerializedProperty, TagPropertyDrawer

### Community 191 - "HorizontalLineDecoratorDrawer"
Cohesion: 0.33
Nodes (3): DecoratorDrawer, Rect, HorizontalLineDecoratorDrawer

### Community 192 - "Code Style & Contribution Guidelines"
Cohesion: 0.33
Nodes (5): 1. C# Naming Conventions, 2. Event Handling & Subscriptions, 3. Zero-Allocation Philosophy, 4. Unity Inspector & Attributes, Code Style & Contribution Guidelines

### Community 193 - "Common Mistakes & Troubleshooting"
Cohesion: 0.33
Nodes (5): 1. Addressables & LevelData InvalidKeyException, 2. Items Flinging or Tunneling Through Walls, 3. Direct Modification of PlayerData, 4. Broken Inspector References After Renaming, Common Mistakes & Troubleshooting

### Community 194 - "Level Creation Tutorial"
Cohesion: 0.33
Nodes (5): Level Creation Tutorial, Step 1: Open the Template Editor, Step 2: Define Level Properties, Step 3: Design the Physical Layout (Optional), Step 4: Save & Map

### Community 195 - "App Store & Google Play Publishing Checklist"
Cohesion: 0.33
Nodes (5): 1. Compliance & Asset Verification, 2. Rendering & Optimization, 3. Storage & Initialization, 4. Integration, App Store & Google Play Publishing Checklist

### Community 196 - "Shop & Economy Configuration"
Cohesion: 0.33
Nodes (5): 1. Accessing the Shop Editor, 2. Defining a Product, 3. Product Presentation, 4. Integration with Validations, Shop & Economy Configuration

### Community 197 - "EHighlightTarget"
Cohesion: 0.33
Nodes (6): EHighlightTarget, AutoFindItems, GoalCard, Manual, Powerup, SpecificItem

### Community 198 - "ShopOpener"
Cohesion: 0.33
Nodes (3): MatchThemAll.Scripts.Shop, ShopOpener, ShopPanel

### Community 199 - ".OnGUI_Internal"
Cohesion: 0.33
Nodes (4): GUIContent, Rect, SerializedProperty, AllowNestingPropertyDrawer

### Community 200 - "InputAxisTest.cs"
Cohesion: 0.47
Nodes (4): Button, InputAxisNest1, InputAxisNest2, InputAxisTest

### Community 201 - "LayerTest.cs"
Cohesion: 0.47
Nodes (4): Button, LayerNest1, LayerNest2, LayerTest

### Community 202 - "MinMaxSliderTest.cs"
Cohesion: 0.53
Nodes (5): Vector2, Vector2Int, MinMaxSliderNest1, MinMaxSliderNest2, MinMaxSliderTest

### Community 203 - "_NaughtyScriptableObject"
Cohesion: 0.40
Nodes (3): Button, List, _NaughtyScriptableObject

### Community 204 - "ReorderableListTest"
Cohesion: 0.47
Nodes (5): GameObject, List, Vector3, ReorderableListTest, SomeStruct

### Community 205 - "ShowAssetPreviewNest1"
Cohesion: 0.73
Nodes (5): GameObject, Sprite, ShowAssetPreviewNest1, ShowAssetPreviewNest2, ShowAssetPreviewTest

### Community 206 - "SortingLayerTest.cs"
Cohesion: 0.47
Nodes (4): Button, SortingLayerNest1, SortingLayerNest2, SortingLayerTest

### Community 207 - "TagTest.cs"
Cohesion: 0.47
Nodes (4): Button, TagNest1, TagNest2, TagTest

### Community 208 - "GameSettingsSO"
Cohesion: 0.40
Nodes (4): bool, int, GameSettingsSO, MatchThemAll.Scripts.Settings

### Community 209 - "FolderPathAttribute"
Cohesion: 0.40
Nodes (4): FolderPathAttribute, DefaultPath, RelativePath, Title

### Community 210 - "ExpandableTest.cs"
Cohesion: 0.80
Nodes (4): ScriptableObject, ExpandableScriptableObjectNest1, ExpandableScriptableObjectNest2, ExpandableTest

### Community 211 - "LabelTest.cs"
Cohesion: 0.60
Nodes (4): Vector2, LabelNest1, LabelNest2, LabelTest

### Community 213 - "FolderPathTest.cs"
Cohesion: 0.83
Nodes (3): FolderPathNest1, FolderPathNest2, FolderPathTest

### Community 214 - "HorizontalLineTest.cs"
Cohesion: 0.83
Nodes (3): HorizontalLineNest1, HorizontalLineNest2, HorizontalLineTest

### Community 215 - "InfoBoxTest.cs"
Cohesion: 0.83
Nodes (3): InfoBoxNest1, InfoBoxNest2, InfoBoxTest

### Community 216 - "ProgressBarTest.cs"
Cohesion: 0.83
Nodes (3): ProgressBarNest1, ProgressBarNest2, ProgressBarTest

### Community 217 - "ReadOnlyTest.cs"
Cohesion: 0.83
Nodes (3): ReadOnlyNest1, ReadOnlyNest2, ReadOnlyTest

### Community 218 - "ResizableTextAreaTest.cs"
Cohesion: 0.83
Nodes (3): ResizableTextAreaNest1, ResizableTextAreaNest2, ResizableTextAreaTest

## Knowledge Gaps
- **605 isolated node(s):** `TmpFont`, `LevelSpawnedEvent`, `State`, `PreviousState`, `Instance` (+600 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1013 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **25 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LevelEditorWindow` connect `LevelEditorWindow` to `ShopEditorWindow`, `LevelEditorWindow`, `.LoadAll`, `List`, `NaughtyInspector`?**
  _High betweenness centrality (0.191) - this node is a cross-community bridge._
- **Why does `NaughtyInspector` connect `NaughtyInspector` to `.GetTargetObjectWithProperty`, `.DrawSerializedProperties`, `SavedBool`?**
  _High betweenness centrality (0.170) - this node is a cross-community bridge._
- **Why does `Item` connect `Item` to `ItemReachedSpotEvent`, `GoalManager`, `.Activate`, `ItemSpotManager`, `Item`, `MonoBehaviour`, `MatchThemAll.Scripts`, `InputManager`, `ItemPlacer`, `LevelManager`, `.GenerateItemPrefab`, `MergeManager`, `EItemName`, `PowerupContext`, `PowerupManager`, `ItemManagerWindow`, `MergeStartedEvent`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._
- **What connects `TmpFont`, `LevelSpawnedEvent`, `State` to the rest of the system?**
  _605 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `LevelEditorWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.12944523470839261 - nodes in this community are weakly interconnected._
- **Should `ItemManagerWindow` be split into smaller, more focused modules?**
  _Cohesion score 0.11827956989247312 - nodes in this community are weakly interconnected._
- **Should `SaveManager` be split into smaller, more focused modules?**
  _Cohesion score 0.09102564102564102 - nodes in this community are weakly interconnected._