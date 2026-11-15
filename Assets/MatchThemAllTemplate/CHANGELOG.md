# Changelog

## Unreleased

### Added
- Power-up Manager window: the power-up tray is now data-driven. Edit price, unlock level, sound, order and model offset/rotation, or add your own power-ups.
- Player Data window for your own test save: coins, gems, power-up charges and a Furthest level picker to replay unlocks.
- Real ads (LevelPlay) and real-money purchases (Unity IAP 5). Both are optional and switch on when you install their packages. The Editor uses pretend ads and a fake store.
- Out-of-charges popup: the player chooses to pay coins or watch an ad for a free charge.
- Tutorial steps for each power-up on its unlock level (levels 2 to 5).
- Gem counter, and a coin fly-to-counter effect on the win screen.
- Item Manager: tray slot look (mesh and material) and a reset button for dock placement.
- **Match Them All ▸ Set Up Project**: one click does the first-time setup after importing (TextMesh Pro essentials, render pipeline, input handling, level registration, build scenes). Safe to run twice.
- New illustrated user guide with a page for every tool, gameplay GIFs and annotated screenshots.

### Changed
- Every third-party asset, and the gameplay code that came from a course, was replaced with original, CC0 or OFL content. The template is licensed under the Asset Store EULA.
- UI overhaul: cleaner Lobby and HUD, UI built from prefabs, a shared "MATCH THEM ALL" title prefab, PrimeTween UI motion.
- Sound Manager overhaul. Music volume edits now apply while the track plays, and the default music volume is lower.
- Power-up visuals redone: new VFX, a bendable Vacuum hose, locked power-ups darkened with a lock badge, and Freeze now shows the time left inside the timer.
- Editor windows keep the Save button visible, so dragging a slider no longer drops the drag.

### Fixed
- The template failed to compile in a fresh project: `LevelManager` relied on a course helper outside the package, and a one-off AI Assistant script sat in the Editor folder.
- The tray froze when the last free slot was filled by an item landing mid-row.
- Power-up locks stayed on after the player reached the unlock level.
- The Lobby settings button turned dark in Play mode.
- `UnassignedReferenceException` when using the Vacuum.
- Daily reward and Next Level bugs, and a Windows build failure.
- The Shop Manager description label painted over the tab bar.

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