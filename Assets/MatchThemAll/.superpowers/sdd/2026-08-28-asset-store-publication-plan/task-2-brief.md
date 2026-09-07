# Task 2: Move Runtime Scripts to New Structure

**Goal:** Move all runtime C# scripts from `Assets/Match Them All/Scripts/Runtime/` to `Assets/MatchThemAllTemplate/Scripts/Runtime/`

## Requirements

**Files to move (78 scripts in ~12 folders):**

### From (existing):
- `Assets/Match Them All/Scripts/Runtime/Core/*.cs`
- `Assets/Match Them All/Scripts/Runtime/Gameplay/*.cs`
- `Assets/Match Them All/Scripts/Runtime/PowerUps/*.cs`
- `Assets/Match Them All/Scripts/Runtime/Shop/*.cs`
- `Assets/Match Them All/Scripts/Runtime/LevelSystem/*.cs`
- `Assets/Match Them All/Scripts/Runtime/SaveSystem/*.cs`
- `Assets/Match Them All/Scripts/Runtime/UI/*.cs`
- `Assets/Match Them All/Scripts/Runtime/Tutorial/*.cs`
- `Assets/Match Them All/Scripts/Runtime/VFX/*.cs`
- `Assets/Match Them All/Scripts/Runtime/Pixelate/*.cs`
- `Assets/Match Them All/Scripts/Runtime/Utilities/*.cs`

### To (new structure):
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Core/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Gameplay/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Shop/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/LevelSystem/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/SaveSystem/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/UI/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Tutorial/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/VFX/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Pixelate/`
- `Assets/MatchThemAllTemplate/Scripts/Runtime/Utilities/`

**Implementation approach:**
1. Use bash `cp` commands to copy files (faster than individual MCP calls)
2. Verify copy succeeded
3. Delete original files
4. Use `assets-refresh` Unity MCP tool

## Verification

After moving:
1. Verify all scripts exist in new location (78 total)
2. Verify no scripts remain in old `Scripts/Runtime/` location
3. Verify .meta files exist alongside .cs files

## Report

Write report to: `.superpowers/sdd/2026-08-28-asset-store-publication-plan/task-2-report.md`

Report format:
- Status: DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED
- Scripts moved: [count]
- Any issues encountered
- Verification results
