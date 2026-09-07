# Asset Store Publication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform Match Them All project into a commercial-grade Unity Asset Store template package

**Architecture:** Conservative restructure — move all shippable content into single `Assets/MatchThemAllTemplate/` folder, bundle free dependencies, replace paid assets with placeholders, create beginner documentation, achieve zero console errors on import.

**Tech Stack:** Unity 2021.3 LTS, Universal Render Pipeline, NaughtyAttributes, ZLinq, TextMesh Pro

---

## Global Constraints

- Unity version: 2021.3 LTS or newer
- All content must be in single root folder: `Assets/MatchThemAllTemplate/`
- Namespace remains: `MatchThemAll.Scripts` (no code changes)
- Target audience: Beginner developers (no-code editor tools)
- Quality requirement: Zero console errors/warnings on fresh import
- Legal: All third-party assets must have verified redistribution rights
- Mobile-ready: Touch controls, UI canvas scaling verified

---

## Phase 1: Package Isolation & Structure (Week 1)

### Overview

Create new `Assets/MatchThemAllTemplate/` folder structure and migrate all shippable content from current locations.

**Current structure:**
```
Assets/
├── Match Them All/           (88 runtime + 10 editor scripts, prefabs, scenes)
├── 4Tudio/
├── BTM_Assets/
├── Cosmic_Retro_*/
├── SimpleFX/
├── UI Kawaii Bundle/
└── [other third-party folders]
```

**Target structure:**
```
Assets/MatchThemAllTemplate/
├── Scripts/
├── Prefabs/
├── Scenes/
├── Resources/
├── Art/
├── Audio/
├── Shaders/
├── Settings/
├── ThirdParty/
├── Examples/
└── Documentation/
```

---

## Task 1: Create Folder Structure

**Files:**
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Core/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Gameplay/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Shop/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/LevelSystem/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/SaveSystem/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/UI/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Tutorial/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/VFX/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Pixelate/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Runtime/Utilities/`
- Create: `Assets/MatchThemAllTemplate/Scripts/Editor/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/Gameplay/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/UI/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/UI/Level/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/UI/PowerUp/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/UI/Shop/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/PowerUps/`
- Create: `Assets/MatchThemAllTemplate/Prefabs/VFX/`
- Create: `Assets/MatchThemAllTemplate/Scenes/`
- Create: `Assets/MatchThemAllTemplate/Resources/Powerups/`
- Create: `Assets/MatchThemAllTemplate/Resources/Shop/`
- Create: `Assets/MatchThemAllTemplate/Art/Sprites/`
- Create: `Assets/MatchThemAllTemplate/Art/Models/`
- Create: `Assets/MatchThemAllTemplate/Art/Materials/`
- Create: `Assets/MatchThemAllTemplate/Art/Textures/`
- Create: `Assets/MatchThemAllTemplate/Art/Animations/`
- Create: `Assets/MatchThemAllTemplate/Audio/`
- Create: `Assets/MatchThemAllTemplate/Shaders/`
- Create: `Assets/MatchThemAllTemplate/ThirdParty/`
- Create: `Assets/MatchThemAllTemplate/Examples/Levels/`
- Create: `Assets/MatchThemAllTemplate/Examples/Items/`
- Create: `Assets/MatchThemAllTemplate/Documentation/`
- Create: `Assets/MatchThemAllTemplate/Settings/`

**Verification:**
- [ ] All folders created in Unity (appear in Project window)
- [ ] Unity creates .meta files for each folder
- [ ] Folder hierarchy matches target structure exactly

---

## Task 2: Migrate Runtime Scripts

**Files:**
- Copy: `Scripts/Runtime/Core/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Core/`
- Copy: `Scripts/Runtime/Gameplay/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Gameplay/`
- Copy: `Scripts/Runtime/PowerUps/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/`
- Copy: `Scripts/Runtime/Shop/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Shop/`
- Copy: `Scripts/Runtime/LevelSystem/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/LevelSystem/`
- Copy: `Scripts/Runtime/SaveSystem/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/SaveSystem/`
- Copy: `Scripts/Runtime/UI/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/UI/`
- Copy: `Scripts/Runtime/Tutorial/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Tutorial/`
- Copy: `Scripts/Runtime/VFX/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/VFX/`
- Copy: `Scripts/Runtime/Pixelate/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Pixelate/`
- Copy: `Scripts/Runtime/Utilities/*` → `Assets/MatchThemAllTemplate/Scripts/Runtime/Utilities/`

**Verification:**
- [ ] All 88 runtime scripts copied to new locations
- [ ] Each script has matching .meta file
- [ ] Scripts compile without errors (check Console)

---

## Task 3: Migrate Editor Scripts

**Files:**
- Copy: `Scripts/Editor/*` → `Assets/MatchThemAllTemplate/Scripts/Editor/`

**Editor scripts to migrate:**
```
EditorWindowStyles.cs
HierarchySectionHeader.cs
ItemManagerWindow.cs
ItemReferenceOps.cs
LevelEditorWindow.cs
LevelMapBuilder.cs
PowerupDatabaseSetup.cs
PrefabConsolidator.cs
ShopEditorWindow.cs
ShopSetup.cs
```

**Verification:**
- [ ] All 10 editor scripts copied
- [ ] Editor scripts compile (no namespace conflicts with runtime)
- [ ] Unity Editor menu items appear correctly

---

## Task 4: Migrate Prefabs

**Files:**
- Copy: `Prefabs/Gameplay/*` → `Assets/MatchThemAllTemplate/Prefabs/Gameplay/`
- Copy: `Prefabs/UI/*` → `Assets/MatchThemAllTemplate/Prefabs/UI/`
- Copy: `Prefabs/PowerUps/*` → `Assets/MatchThemAllTemplate/Prefabs/PowerUps/`
- Copy: `Prefabs/VFX/*` → `Assets/MatchThemAllTemplate/Prefabs/VFX/`
- Copy: `Prefabs/Levels/*` → `Assets/MatchThemAllTemplate/Prefabs/Levels/`

**Verification:**
- [ ] All prefabs copy with dependencies intact
- [ ] No "Missing Script" warnings on prefabs
- [ ] Prefabs drag into scene correctly

---

## Task 5: Migrate Scenes

**Files:**
- Copy: `Scenes/MainMenu.unity` → `Assets/MatchThemAllTemplate/Scenes/`
- Copy: `Scenes/MainScene.unity` → `Assets/MatchThemAllTemplate/Scenes/`
- Copy: `Scenes/LevelSelect.unity` → `Assets/MatchThemAllTemplate/Scenes/`
- Copy: `Scenes/LoadingScene.unity` → `Assets/MatchThemAllTemplate/Scenes/`

**Verification:**
- [ ] All 4 main scenes copied
- [ ] Scenes open without missing reference errors
- [ ] Build Settings includes all scenes

---

## Task 6: Migrate Resources

**Files:**
- Copy: `Resources/*` → `Assets/MatchThemAllTemplate/Resources/`
- Copy: `Settings/*` → `Assets/MatchThemAllTemplate/Settings/`

**Verification:**
- [ ] ScriptableObjects copy correctly (GameSettings, PowerupDatabase, ShopDatabase)
- [ ] `Resources.Load()` paths still work after restructure
- [ ] Check for hardcoded paths in code that reference Resources

---

## Task 7: Migrate Art Assets

**Files:**
- Copy: `Sprites/*` → `Assets/MatchThemAllTemplate/Art/Sprites/`
- Copy: `Models/*` → `Assets/MatchThemAllTemplate/Art/Models/`
- Copy: `Material/*` → `Assets/MatchThemAllTemplate/Art/Materials/`
- Copy: `Animation/*` → `Assets/MatchThemAllTemplate/Art/Animations/`
- Copy: `Shader Graph/*` → `Assets/MatchThemAllTemplate/Shaders/`

**Verification:**
- [ ] Textures import correctly (check compression settings)
- [ ] Materials reference correct textures
- [ ] Shader graphs compile

---

## Task 8: Migrate Audio

**Files:**
- Copy: `Audio/*` → `Assets/MatchThemAllTemplate/Audio/`

**Verification:**
- [ ] Audio clips import (check compression format for mobile)
- [ ] Audio sources in prefabs reference correct clips
- [ ] SoundManager能找到正确资源

---

## Task 9: Migrate _START_HERE Content

**Files:**
- Copy: `_START_HERE/Levels/*` → `Assets/MatchThemAllTemplate/Examples/Levels/`
- Copy: `_START_HERE/Items/*` → `Assets/MatchThemAllTemplate/Examples/Items/`

**Rename:**
- `_START_HERE/` → `Examples/` (convention standard)

**Verification:**
- [ ] LevelDataSO assets load in Template Editor
- [ ] Item prefabs appear in Item Manager
- [ ] Example content works in game

---

## Task 10: Verify Scene References

**Files:**
- Test: `Assets/MatchThemAllTemplate/Scenes/MainMenu.unity`
- Test: `Assets/MatchThemAllTemplate/Scenes/MainScene.unity`
- Test: `Assets/MatchThemAllTemplate/Scenes/LevelSelect.unity`
- Test: `Assets/MatchThemAllTemplate/Scenes/LoadingScene.unity`

**Verification:**
- [ ] Open MainMenu scene — all references valid (no pink materials, no missing scripts)
- [ ] Open MainScene scene — GameManager, ItemSpotManager reference correct assets
- [ ] Open LevelSelect scene — saga map loads
- [ ] Open LoadingScene scene — loads correctly
- [ ] Check Console for any "Missing" warnings

**Fix process if references broken:**
1. Identify which prefab/ScriptableObject reference is missing
2. Locate the correct asset in new folder structure
3. Re-assign in Inspector
4. Save prefab/ScriptableObject

---

## Task 11: Verify Editor Tools

**Files:**
- Test: `Assets/MatchThemAllTemplate/Scripts/Editor/LevelEditorWindow.cs`
- Test: `Assets/MatchThemAllTemplate/Scripts/Editor/ItemManagerWindow.cs`
- Test: `Assets/MatchThemAllTemplate/Scripts/Editor/ShopEditorWindow.cs`

**Verification:**
- [ ] Menu: `Match Them All → Template Editor` opens window
- [ ] Template Editor displays level list
- [ ] Can create new level, save, delete
- [ ] Preview Layout updates Scene View
- [ ] Menu: `Match Them All → Item Manager` opens window
- [ ] Item Manager shows 3D preview
- [ ] Menu: `Match Them All → Shop Manager` opens window
- [ ] No errors in Console during editor operations

---

## Task 12: Verify Resources.Load Paths

**Files:**
- Inspect: `Assets/MatchThemAllTemplate/Scripts/Runtime/Core/SoundManager.cs`
- Inspect: `Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/PowerupManager.cs`
- Inspect: `Assets/MatchThemAllTemplate/Scripts/Runtime/Shop/ShopManager.cs`
- Inspect: `Assets/MatchThemAllTemplate/Scripts/Runtime/LevelSystem/LevelManager.cs`

**Verification:**
- [ ] Search all scripts for `Resources.Load(`
- [ ] Verify each path matches new folder structure
- [ ] Example: `Resources.Load("Powerups/Fan")` → `"MatchThemAllTemplate/Resources/Powerups/Fan"` is valid
- [ ] Test loading each Resources asset in Play Mode

**Fix process:**
1. Find `Resources.Load("old/path")` in code
2. Update to new path matching `Resources/` subfolder structure
3. Test in Play Mode

---

## Task 13: Clean Up Original Folder

**Files:**
- Delete: Original `Assets/Match Them All/` folder (after verifying migration complete)

**Verification:**
- [ ] All content successfully migrated and verified
- [ ] Original folder can be safely deleted
- [ ] No remaining references to old `Match Them All/` paths
- [ ] Commit migration to git

---

## Phase 2: Asset Cleanup & Legal Compliance (Week 2)

### Overview

Audit all third-party assets, verify licenses, bundle permitted dependencies, replace paid assets with placeholders, write legal notices.

---

## Task 14: Audit Code Dependencies

**Files:**
- Locate: `NaughtyAttributes/` folder (likely in root or Packages/)
- Locate: `ZLinq/` folder (likely in root or Packages/)
- Check license files for each

**Verification:**
- [ ] NaughtyAttributes license allows redistribution (MIT ✓)
- [ ] ZLinq license verified (MIT/Apache/CC0 = bundle; proprietary = external)
- [ ] Document finding in THIRD_PARTY_NOTICES.txt

**If ZLinq license is proprietary:**
- Remove ZLinq usage from code
- Replace `AsValueEnumerable()` with standard LINQ or for-loops
- Document ZLinq as external dependency in README

---

## Task 15: Audit Third-Party Assets

**Files to check licenses:**

Third-party folders to audit:
```
4Tudio/
BTM_Assets/
Cosmic_Retro_Blasters Pack_1_FREE/
Cosmic_Retro_Grenades_Pack_1_Demo/
SimpleFX/
UI Kawaii Bundle/
UI Essential Pack/
UI Mana Soul/
UIBundleFree/
UI Dark Dwellers/
Design Toolbox/
ProPixelizer/
Sci-Fi RTS pack/
Floreswa/
JMO Assets/
Tabsil/
```

**Audit process:**
1. Open each asset folder
2. Look for LICENSE.txt, LICENSE.md, or notice file
3. Check Asset Store page for license terms
4. Document in spreadsheet: Asset Name | License Type | Redistribution Allowed | Action

**Verification:**
- [ ] All third-party assets audited
- [ ] License status documented
- [ ] Decision made: Bundle / Document as External / Replace

---

## Task 16: Bundle Free Dependencies

**Files:**
- Copy: `NaughtyAttributes/` → `Assets/MatchThemAllTemplate/ThirdParty/NaughtyAttributes/`
- Copy: `ZLinq/` (if permissive license) → `Assets/MatchThemAllTemplate/ThirdParty/ZLinq/`
- Copy: `SimpleFX/` (if license permits) → `Assets/MatchThemAllTemplate/ThirdParty/SimpleFX/`
- Copy: Other verified free assets → `Assets/MatchThemAllTemplate/ThirdParty/[AssetName]/`

**Verification:**
- [ ] All bundled assets copy with .meta files
- [ ] Assets import correctly in fresh Unity project
- [ ] No compilation errors from third-party code

---

## Task 17: Create Placeholder Assets

**Files:**
- Create: `Assets/MatchThemAllTemplate/Art/Models/Placeholders/`
- Create: Simple capsule gem prefab
- Create: Simple sphere gem prefab
- Create: Simple cube gem prefab
- Create: Simple cylinder gem prefab
- Create: Simple torus gem prefab

**Placeholder creation process:**
1. Create new sphere/capsule/cube/cylinder/torus primitive in scene
2. Apply metallic material with different colors per shape
3. Create TextMeshPro label "PLACEHOLDER"
4. Add `Item` component with placeholder name
5. Save as prefab in `Art/Models/Placeholders/`

**Verification:**
- [ ] 5 placeholder gem prefabs created
- [ ] Placeholder materials clearly distinct (bright colors, magenta tint)
- [ ] Item component attached with placeholder EItemName values
- [ ] Prefabs drag into scene and function in gameplay

---

## Task 18: Replace BTM_Assets References

**Files:**
- Update: `Assets/MatchThemAllTemplate/Examples/Items/` (gem prefabs)
- Update: `Assets/MatchThemAllTemplate/Prefabs/Gameplay/` (if referencing gems)

**Replacement process:**
1. Identify which prefabs use BTM_Assets
2. Replace MeshFilter.mesh with Unity default (sphere, capsule, etc.)
3. Replace Material with metallic placeholder material
4. Update Item component references if needed
5. Apply prefab changes

**Verification:**
- [ ] No references to BTM_Assets in MatchThemAllTemplate folder
- [ ] Gem placeholders function in gameplay (items can be spawned, matched, etc.)
- [ ] No "Missing" warnings in Console

---

## Task 19: Replace Paid UI Assets

**Files:**
- Update: `Assets/MatchThemAllTemplate/Prefabs/UI/` (panels, buttons)

**Replacement process:**
1. Identify UI prefabs using paid assets (custom sprites, icons)
2. Replace with Unity UI default images (filled, outline sprites)
3. Use solid colors with borders
4. Add TextMeshPro text: "REPLACE WITH YOUR UI"
5. Document in REPLACING_PLACEHOLDERS.md

**Verification:**
- [ ] UI elements functional (buttons clickable, panels show/hide)
- [ ] No missing sprite references
- [ ] Clear visual indication these are placeholders

---

## Task 20: Write THIRD_PARTY_NOTICES.txt

**Files:**
- Create: `Assets/MatchThemAllTemplate/THIRD_PARTY_NOTICES.txt`

**Template format:**
```
================================================================================
[ASSET NAME]
[Copyright/License Info]
Licensed under [LICENSE NAME]
Source: [URL or Asset Store Link]
================================================================================

[FULL LICENSE TEXT OR LINK TO LICENSE]

================================================================================
[ASSET NAME 2]
...
```

**Verification:**
- [ ] All bundled third-party assets listed
- [ ] Each entry includes full license text or reference
- [ ] MIT/Apache/CC0 licenses fully reproduced
- [ ] Asset Store assets reference their pages

---

## Task 21: Write LICENSE.txt

**Files:**
- Create: `Assets/MatchThemAllTemplate/LICENSE.txt`

**Content:**
```
Match Them All - Complete Game Template
Copyright (c) 2026 [Your Name/Handle]

This template includes code and assets created by the publisher, plus
third-party software listed in THIRD_PARTY_NOTICES.txt.

TERMS OF USE:
- You may use this template to create and publish games on any platform
- You may modify, extend, and customize this template for your own projects
- You may NOT resell or redistribute this template as a standalone product
- You may NOT claim this template's code or assets as your own work

SUPPORT:
[Your support contact information]
```

**Verification:**
- [ ] LICENSE.txt exists at package root
- [ ] License terms clear and enforceable
- [ ] Contact information accurate

---

## Task 22: Addressables Audit

**Files:**
- Inspect: `AddressableAssetsData/` folder

**Verification:**
- [ ] Check if Addressables system is used in code (search for `Addressables.LoadAssetAsync`)
- [ ] If used: Rebuild address groups for new folder structure
- [ ] If NOT used: Remove AddressablesData folder entirely

**Recommendation:** Remove Addressables unless critical — Resources.Load() is simpler for beginners

**Verification:**
- [ ] No Addressables references in code OR
- [ ] Addressables groups rebuilt for new paths
- [ ] No Addressables-related warnings in Console

---

## Task 23: Console Error Cleanup

**Files:**
- Test: All scenes in `Assets/MatchThemAllTemplate/Scenes/`
- Test: All editor windows
- Test: Play Mode through complete level

**Cleanup process:**
1. Open Console (Ctrl+Shift+C)
2. Run through all scenes
3. Play through MainScene completely (start → win or lose → menu)
4. Fix all Error-level messages first
5. Then fix all Warning-level messages

**Common fixes:**
- Missing script references → Re-assign in Inspector
- Missing prefab references → Update path or re-assign
- Obsolete API warnings → Update to current API
- Third-party warnings → Update package or wrap in `#pragma warning disable`

**Verification:**
- [ ] Zero Error messages in Console
- [ ] Zero Warning messages in Console
- [ ] All scenes play without issues
- [ ] All editor windows open without errors

---

## Phase 3: Beginner-Focused Documentation (Week 3)

### Overview

Create comprehensive beginner documentation with visual guides, tooltips, and help systems.

---

## Task 24: Write README.md (Quick Start)

**Files:**
- Create: `Assets/MatchThemAllTemplate/README.md`

**Content structure:**
```markdown
# Match Them All - Complete Game Template

🎮 Create your own match-3 mobile game — no coding required!

## ⚡ Quick Start (5 minutes)
1. Import this package
2. Open scene: Scenes/MainMenu
3. Press Play — game works immediately!
4. Open editor: Menu bar → Match Them All → Template Editor
5. Create level: Click + New, configure, Save
6. Test level: Click ▶ Play Level

✅ Done! You created your first custom level.

## 📦 What's Included
[Checklist of features]

## 🎯 Who This Is For
[Beginner-focused description]

## 📋 Requirements
- Unity 2021.3 LTS or newer
- Universal Render Pipeline (URP)
- Input System package
- TextMesh Pro

## 🚀 Next Steps
[Links to documentation]

## ⚠️ Placeholder Assets
[Reference to REPLACING_PLACEHOLDERS.md]

## 📞 Support
[Contact info]

## 📄 License
[Reference to LICENSE.txt and THIRD_PARTY_NOTICES.txt]
```

**Verification:**
- [ ] README.md at package root (not in subfolder)
- [ ] README.md opens in text editor
- [ ] Quick Start section clear and actionable
- [ ] All links point to correct documentation files

---

## Task 25: Write CUSTOMIZATION_GUIDE.md

**Files:**
- Create: `Assets/MatchThemAllTemplate/Documentation/CUSTOMIZATION_GUIDE.md`

**Content structure:**
```markdown
# Customization Guide

## Quick Reference
- [Adding a New Item Type](#adding-item)
- [Creating a New Level](#creating-level)
- [Adding a New Power-Up](#adding-powerup)
- [Customizing UI](#customizing-ui)
- [Changing Game Settings](#game-settings)
- [Replacing Placeholder Assets](#replacing-placeholders)
- [Building for Mobile](#mobile-build)

## Adding a New Item Type
### What You Need
- 3D model
- Icon sprite (64x64+)

### Steps
1. Import 3D model into Examples/Items/
2. Add Item component to prefab
3. Add name to EItemName enum
4. Configure in Template Editor
[Detailed steps with code references]

## Creating a New Level
[Step-by-step guide with references to Template Editor]

[Continue for each task...]
```

**Verification:**
- [ ] Guide covers 7+ common customization tasks
- [ ] Each task has step-by-step instructions
- [ ] Code references accurate (file paths, class names)
- [ ] Clear section anchors for navigation

---

## Task 26: Write REPLACING_PLACEHOLDERS.md

**Files:**
- Create: `Assets/MatchThemAllTemplate/Documentation/REPLACING_PLACEHOLDERS.md`

**Content structure:**
```markdown
# Replacing Placeholder Assets

This template ships with placeholder assets for items requiring separate licensing.

## Which Assets Are Placeholders?

- Gem models in Examples/Items/ (simple capsule meshes)
- Some UI elements (basic colored panels)
- Basic VFX (Unity default particles)

## How to Replace

### Option 1: Recommended Asset Store Assets
[Links to recommended replacement assets]

### Option 2: Your Own Models
[Workflow for importing custom models]

### Option 3: Keep Placeholders
[Note that game works with placeholders]
```

**Verification:**
- [ ] All placeholder types documented
- [ ] Replacement workflow clear
- [ ] Links to recommended assets (if applicable)
- [ ] "Keep placeholders" option valid

---

## Task 27: Write API_REFERENCE.md

**Files:**
- Create: `Assets/MatchThemAllTemplate/Documentation/API_REFERENCE.md`

**Content structure:**
```markdown
# API Reference

## Architecture Overview
[EventBus pattern explanation]

## Core Managers

### GameManager
```csharp
// Usage example
GameManager.Instance.StartGame();
GameManager.Instance.PauseGame();
```

### SaveManager
```csharp
// Usage example
SaveManager.Instance.GetInt("level", 0);
SaveManager.Instance.SetInt("level", completedLevel);
```

[Continue for each manager...]
```

**Verification:**
- [ ] Key manager classes documented
- [ ] Usage examples compile and work
- [ ] XML docs visible in IDE
- [ ] Architecture explanation clear for intermediate users

---

## Task 28: Add Tooltips to Scripts

**Files:**
- Inspect: All public serialized fields in Runtime scripts
- Add: `[Tooltip("description")]` attributes

**Priority fields to tooltip:**
- GameManager: levelDuration, gameSpeed
- PowerupManager: cooldownTime, activationCost
- GoalManager: goalType, targetCount
- Item: itemNameKey, icon
- LevelDataSO: all serialized fields

**Verification:**
- [ ] All public serialized fields have tooltips
- [ ] Tooltips appear in Inspector when field selected
- [ ] Tooltip text is clear and helpful

---

## Task 29: Add XML Documentation

**Files:**
- Update: `Scripts/Runtime/Core/GameManager.cs`
- Update: `Scripts/Runtime/Core/SoundManager.cs`
- Update: `Scripts/Runtime/Core/InputManager.cs`
- Update: `Scripts/Runtime/PowerUps/PowerupManager.cs`
- Update: `Scripts/Runtime/Gameplay/Item.cs`
- Update: `Scripts/Runtime/SaveSystem/SaveManager.cs`
- Update: `Scripts/Runtime/LevelSystem/LevelManager.cs`
- Update: `Scripts/Runtime/Gameplay/GoalManager.cs`

**Format:**
```csharp
/// <summary>
/// Brief description of what this class/method does.
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value</returns>
/// <example>
/// <code>
/// GameManager.Instance.StartGame();
/// </code>
/// </example>
```

**Verification:**
- [ ] All public methods have XML docs
- [ ] IDE shows documentation on hover
- [ ] Examples compile and are accurate

---

## Task 30: Add Help Boxes to Editor Windows

**Files:**
- Update: `Scripts/Editor/LevelEditorWindow.cs`
- Update: `Scripts/Editor/ItemManagerWindow.cs`
- Update: `Scripts/Editor/ShopEditorWindow.cs`

**Add to LevelEditorWindow:**
```csharp
// In OnEnable or header area
EditorGUILayout.HelpBox(
    "💡 Tip: Use 'Preview Layout' to see item positions in Scene View before playing.",
    MessageType.Info
);

// Help button
if (GUILayout.Button("📖 Documentation"))
{
    Application.OpenURL("file:///" + Path.GetFullPath("Documentation/GETTING_STARTED.pdf"));
}
```

**Verification:**
- [ ] Help boxes appear in editor windows
- [ ] Help buttons open correct documentation
- [ ] Tips are actually helpful for beginners

---

## Task 31: Write CHANGELOG.md

**Files:**
- Create: `Assets/MatchThemAllTemplate/CHANGELOG.md`

**Content:**
```markdown
# Changelog

## v1.0 - Initial Release
- Complete match-3 gameplay system
- 4 power-ups: Fan, Freeze, Spring, Vacuum
- No-code level editor
- No-code item manager
- Save system with progress tracking
- Shop system with IAP structure
- Daily rewards system
- Mobile-optimized UI
- Tutorial system
- Audio manager
- Level select saga map
```

**Verification:**
- [ ] CHANGELOG.md exists
- [ ] Version numbering consistent
- [ ] All major features listed

---

## Task 32: Document System Requirements

**Files:**
- Update: `README.md` (Requirements section)
- Create: `Documentation/SYSTEM_REQUIREMENTS.md`

**Content:**
```markdown
# System Requirements

## Unity Version
- Unity 2021.3 LTS or newer (recommended)
- Earlier 2021.x versions may work but untested

## Required Packages
These install automatically or via Unity Package Manager:

### Universal Render Pipeline (URP)
1. Window → Package Manager
2. Unity Registry → Universal RP → Install
3. When prompted, create URP settings asset
4. Project Settings → Graphics → Select URP asset

### Input System
1. Window → Package Manager
2. Unity Registry → Input System → Install
3. When prompted, enable new input backend

### TextMesh Pro
1. Window → TextMeshPro → Import TMP Essential Resources
2. Automatically available after first TextMeshPro component use

## Hardware Requirements
- Minimum: 4GB RAM, OpenGL ES 3.0 GPU
- Recommended: 8GB RAM, dedicated GPU
- Mobile: iOS 12+, Android API 21+

## Build Platforms
- ✅ Android (tested)
- ✅ iOS (tested)
- ⚠️ Windows/Mac (editor only, not optimized)
```

**Verification:**
- [ ] Requirements clear in README
- [ ] Detailed doc covers installation steps
- [ ] Screenshots included for package installation

---

## Phase 4: Validation & Polish (Week 4)

### Overview

Run Asset Store validator, fix issues, test mobile optimization, prepare final package.

---

## Task 33: Run Asset Store Validator

**Files:**
- Test: `Assets/MatchThemAllTemplate/` folder

**Process:**
1. Window → Asset Store → Publishing Tools → Validator
2. Select MatchThemAllTemplate folder
3. Run all validation checks
4. Review results

**Required fixes:**
- All content in single root folder ✓
- No .meta file conflicts ✓
- No missing script references ✓
- No hardcoded absolute paths ✓
- README at root ✓
- LICENSE.txt present ✓

**Verification:**
- [ ] Validator passes 100%
- [ ] Zero critical issues
- [ ] Zero warnings from validator

---

## Task 34: Fresh Project Import Test

**Files:**
- Test: Create NEW Unity project (blank)
- Test: Import only MatchThemAllTemplate package

**Process:**
1. Create new Unity 2021.3 LTS project
2. Install URP, Input System, TMP
3. Import MatchThemAllTemplate.unitypackage
4. Open Console
5. Check for errors/warnings

**Verification:**
- [ ] Package imports without errors
- [ ] Console clean (zero errors, zero warnings)
- [ ] All scenes open
- [ ] Editor windows function
- [ ] Can enter Play Mode

---

## Task 35: Scene Functionality Test

**Files:**
- Test: `Scenes/MainMenu.unity`
- Test: `Scenes/MainScene.unity`
- Test: `Scenes/LevelSelect.unity`
- Test: `Scenes/LoadingScene.unity`

**Test each scene:**
1. Open scene
2. Check Console for errors
3. Enter Play Mode
4. Navigate through UI
5. Complete one interaction (start game, select level, etc.)
6. Exit Play Mode

**Verification:**
- [ ] MainMenu: All buttons work, game starts
- [ ] MainScene: Level loads, gameplay functions
- [ ] LevelSelect: Map displays, levels selectable
- [ ] LoadingScene: Loads correctly
- [ ] Zero errors in any scene

---

## Task 36: Editor Tools Functionality Test

**Files:**
- Test: `Match Them All → Template Editor`
- Test: `Match Them All → Item Manager`
- Test: `Match Them All → Shop Manager`

**Test each editor:**
1. Open editor window
2. Perform basic operation (create level, browse items, view products)
3. Save any changes
4. Close editor
5. Check Console for errors

**Verification:**
- [ ] All editor windows open without errors
- [ ] All operations complete without errors
- [ ] Data persists after closing/reopening editor

---

## Task 37: UI Canvas Scaler Audit

**Files:**
- Inspect: All Canvas components in `Prefabs/UI/` and `Scenes/`

**Check each Canvas:**
1. Select Canvas in Hierarchy
2. Verify Canvas Scaler component:
   - UI Scale Mode: "Scale With Screen Size"
   - Reference Resolution: 1920x1080
   - Match: 0.5 (or appropriate for your UI)
   - Reference Pixels Per Unit: 100

**Verification:**
- [ ] All Canvases use "Scale With Screen Size"
- [ ] Reference resolution appropriate for mobile (1920x1080 or similar)
- [ ] Match setting balanced for your UI type

**Fix process:**
1. Select Canvas
2. Add/fix Canvas Scaler component
3. Set appropriate values
4. Apply to prefab if in prefab

---

## Task 38: Mobile Build Test (Android)

**Files:**
- Test: File → Build Settings → Android

**Process:**
1. Switch to Android platform
2. Add all scenes to Build
3. Build APK (even empty/minimal is fine for validation)
4. Verify build completes without errors

**Verification:**
- [ ] Build starts without missing dependency errors
- [ ] Build completes successfully
- [ ] APK generated (even if minimal)

**If build fails:**
1. Read error message
2. Identify missing dependency
3. Add to package or document as requirement
4. Retry build

---

## Task 39: Mobile Build Test (iOS)

**Files:**
- Test: File → Build Settings → iOS

**Process:**
1. Switch to iOS platform
2. Add all scenes to Build
3. Verify build settings (Signing, Architecture)
4. Attempt build (may require Mac for full build)

**Verification:**
- [ ] Build settings configured
- [ ] No compile errors (even if can't fully build on Windows)
- [ ] Document any iOS-specific requirements

---

## Task 40: Performance Check

**Files:**
- Test: Play Mode in MainScene

**Process:**
1. Open MainScene
2. Window → Analysis → Profiler
3. Enter Play Mode
4. Play through sample level
5. Monitor performance metrics

**Check metrics:**
- Frame Time: Should be <16ms (60fps target)
- CPU Usage: No extreme spikes
- Memory: Stable, no leaks
- Draw Calls: <100 for simple scenes

**Verification:**
- [ ] Frame rate stable at 60fps in editor
- [ ] No memory leaks during gameplay
- [ ] No extreme CPU spikes
- [ ] If issues found, optimize hot paths

---

## Task 41: Aspect Ratio Test

**Files:**
- Test: Game View in Unity

**Test aspect ratios:**
1. Set Game View to 1920x1080 (16:9) — standard phone
2. Set Game View to 2340x1080 (19.5:9) — tall phone
3. Set Game View to 2048x1536 (4:3) — tablet
4. Verify UI elements don't clip or overlap incorrectly

**Verification:**
- [ ] 16:9: UI displays correctly
- [ ] 19.5:9: UI displays correctly (may need scrollable areas)
- [ ] 4:3: UI displays correctly (may need anchoring adjustments)
- [ ] No UI elements cut off or inaccessible

---

## Task 42: Texture Optimization Check

**Files:**
- Inspect: All textures in `Art/` folder

**Check each texture:**
1. Select texture in Project
2. Inspector → Max Size (should be ≤2048 for mobile)
3. Inspector → Compression (ASTC or ETC2 for mobile)
4. Inspector → Generate Mip Maps (off for UI, on for 3D)

**Verification:**
- [ ] No texture exceeds 2048x2048
- [ ] Mobile compression applied (ASTC/ETC2)
- [ ] UI textures have no mipmaps
- [ ] Textures for mobile target appropriate quality

---

## Task 43: Final Console Verification

**Files:**
- Test: Complete fresh import + all scenes + Play Mode

**Final verification process:**
1. Close Unity completely
2. Delete Library folder (force fresh import)
3. Reopen Unity project
4. Open Console
5. Play through each scene
6. Use each editor tool
7. Check for any errors or warnings

**Verification:**
- [ ] Zero errors in Console
- [ ] Zero warnings in Console
- [ ] All functionality works after clean import

---

## Task 44: Package Creation

**Files:**
- Create: `MatchThemAllTemplate.unitypackage`

**Process:**
1. Close Unity
2. File → Export Package
3. Select `Assets/MatchThemAllTemplate/` folder
4. Check "Include dependencies" (include scripts, prefabs, etc.)
5. Export to desktop or project root
6. Verify package size (<500MB ideal, <1GB max)

**Verification:**
- [ ] Package exports without errors
- [ ] Package size reasonable
- [ ] Package contains all required content
- [ ] Excludes development-only files

---

## Task 45: Final Git Commit

**Files:**
- Commit: All Phase 1-4 changes

**Commit message:**
```
feat: Asset Store publication package ready

Phase 1: Package isolation
- Created MatchThemAllTemplate folder structure
- Migrated all scripts, prefabs, scenes, resources
- Verified scene references and editor tools

Phase 2: Asset cleanup
- Bundled NaughtyAttributes, ZLinq, SimpleFX
- Replaced paid assets with placeholders
- Created THIRD_PARTY_NOTICES.txt and LICENSE.txt

Phase 3: Documentation
- README.md with quick start guide
- CUSTOMIZATION_GUIDE.md with task tutorials
- API_REFERENCE.md for developers
- Added tooltips and XML docs to all scripts
- Help boxes in editor windows

Phase 4: Validation
- Asset Store Tools validator passes 100%
- Fresh project import test passed
- All scenes and editor tools functional
- Mobile build test successful
- Console clean (zero errors/warnings)
- Package created and ready for submission

Co-Authored-By: Claude <noreply@anthropic.com>
```

**Verification:**
- [ ] All changes committed
- [ ] Commit message comprehensive
- [ ] Branch pushed if needed

---

## Success Criteria Summary

**Publication Ready When:**
- [ ] Task 33: Asset Store Tools validator passes 100%
- [ ] Task 23: Zero console errors/warnings
- [ ] Task 35: All scenes playable
- [ ] Task 36: All editor tools functional
- [ ] Task 24-32: Documentation complete
- [ ] Task 20-21: Legal files present (NOTICES, LICENSE)
- [ ] Task 38-39: Mobile builds successful
- [ ] Task 44: Package created

**If any criteria fail:** Return to relevant task, fix issue, re-verify.

---

## Risk Areas & Contingencies

**High Risk:**
1. Broken prefab references after restructure → Task 10 verification
2. Third-party license violations → Task 15-16 audit
3. Console errors on fresh import → Task 43 final check
4. Missing dependencies in package → Task 34 fresh test

**Contingency Buffer:**
- 7 buffer days built into 30-day timeline
- If Week 2 takes longer, Week 3 docs can be simplified
- Minimum viable docs: README + CUSTOMIZATION_GUIDE + tooltips
- Can add detailed docs post-launch based on feedback

---

## Notes for Implementation

**When asking about ambiguous assets:**
- Reference Task 15 (Third-Party Audit)
- Provide: Asset folder name, Asset Store link if known, what it's used for
- Decision: Bundle / Document as External / Replace

**For ZLinq license verification:**
- Check package license file in ZLinq folder
- If MIT/Apache/CC0 → Bundle
- If proprietary → Remove usage, document as external dependency

**For Addressables:**
- Recommendation: Remove unless critical
- If used → Rebuild groups in Task 22
- If not used → Delete AddressableAssetsData folder entirely

**Testing workflow:**
- After each major migration task (2, 3, 4, 5), verify scenes open
- Don't wait until Phase 1 end to discover broken references
- Fix issues immediately, not at the end

---

**Plan complete. Ready for execution.**
