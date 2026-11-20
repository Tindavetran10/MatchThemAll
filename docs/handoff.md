# Handoff: Match Them All, release prep (2026-11-20)

Read the project `CLAUDE.md` first (rules and pitfalls). This file is only the current state.

## Goal
Publish the template on the Unity Asset Store as **v1.0.0**. The code, guide and package are ready. Final testing and the portal submission are still open.

## Where things are
- **PR #14 is merged** into `main` (merge commit `6dec70bc`). It held the third-party asset replacement, ads and IAP, power-up manager, the user guide, and the fixes below.
- **Branch `release/v1.0.0`** is pushed (commit `05c6c5c6`): only the v1.0.0 changelog. **No PR yet**, so `main` still has the old changelog.
- **Release package:** `E:\Unity Projects\MatchThemAll_export\MatchThemAllTemplate_v1.0.0.unitypackage` (15.9 MB, 807 files, only `Assets/MatchThemAllTemplate`). Built in a clean checkout at `E:\Unity Projects\MatchThemAll_release` (a git worktree of the release branch, safe to delete after upload: `git worktree remove`). 0 compile errors there.
- **Store listing draft:** `docs/asset-store-listing.md` (uncommitted).
- **Guide:** `Assets/MatchThemAllTemplate/Documentation/UserGuide/`, mirrored in the Notion "User Guide" page tree (edit both when the guide changes).

## What was found and fixed this session
- Template did not compile in a fresh project (`LevelManager` relied on the course folder's `Transform.Clear`; a scratch `IRunCommand` script sat in `Editor/`).
- New **Match Them All ▸ Set Up Project** (`Scripts/Editor/ProjectSetup.cs`): TMP essentials, render pipeline (found by GUID), input handler, layers 6/7/8, Addressables level registration, build scenes.
- Freeze Gun could not be tapped in its tutorial step (`TutorialManager.BeginStep` now sets `InputManager.IsTutorialActive` for power-up steps). Verified in Play mode with a simulated mouse press.
- ZLinq needs NuGetForUnity plus the git package; Git is now a requirement. Guide updated.

## Open items (not verified)
1. **Android build** (needs the Android module in Unity Hub; rebuild Addressables with Android active).
2. **Fresh-project Play-through:** import the v1.0.0 package into a new Universal 3D project, follow the guide, run Set Up Project, play from `OpenScene` to level 2, tap the Freeze Gun.
3. **Asset Store submission** (user only): icon, cover, screenshots (reuse `UserGuide/Images`), price, category, sign-in, agreement, submit. Check the portal's current rules.
4. Decide whether to PR `release/v1.0.0` into `main`.
5. `Editor/UIOverhaul` ships in the package and hardcodes scene positions; decide whether buyers should get it.

## Uncommitted in the working tree (not mine unless noted)
- `MainScene.unity`: real UI edits (text colour, orthographic flag, anchor overrides). Intentional or not? Commit separately or discard.
- `SceneLoader.cs`: comment typo only ("tweens" to "tween"). Discard.
- `LilitaOne` font asset and `LevelPlayVersions.json`: Unity regenerates these. Discard.
- `.claude/settings.local.json`, `Assets/.obsidian/`, `CLAUDE.md.bak-2026-11-19`, `docs/asset-store-listing.md`, this file.

## Handy facts
- Main project save file: `C:\Users\gadet\AppData\LocalLow\DefaultCompany\My project\save.json` (back it up before Play tests).
- Unity 6000.0.75f1 only. Test projects from earlier work are in `%TEMP%\mta-clean-test` (safe to delete).
