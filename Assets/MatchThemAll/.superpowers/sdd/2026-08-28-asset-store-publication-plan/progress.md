# SDD ledger — plan: docs/superpowers/plans/2026-08-28-asset-store-publication-plan.md

Base commit: 143ea5067ee22cc9f52c54ec9bfe26e98d40df56
Branch: feat/powerup-vfx
Current time: 2026-09-07T11:48:46

---

Task 1: complete (commits 143ea50.., folders created via assets-create-folder - 40 folders)
Task 2: complete (commits 143ea50.., 88 runtime scripts copied)
Task 3: complete (commits 143ea50.., 10 editor scripts copied)
Task 4: complete (commits 143ea50.., 23 prefabs + 36 .meta files copied)
Task 5: complete (commits 143ea50.., 6 scenes + 6 .meta files copied)
Task 6: complete (commits 143ea50.., 43 resources copied)
Task 7: complete (commits 143ea50.., settings copied)
Task 8: complete (commits 143ea50.., audio copied)
Task 9: complete (commits 143ea50.., 91 art assets copied)
Task 10: complete (commits 143ea50.., 2 shaders copied)
Task 11: complete (commits 143ea50.., README.md created)
Task 12: complete (commits 143ea50.., THIRD_PARTY_NOTICES.txt created)
Task 13: complete (commits 143ea50.., CHANGELOG.md created)
Task 14: complete (commits 143ea50.., LICENSE.txt created)
Task 15: complete (commits 143ea50.., CUSTOMIZATION_GUIDE.md created)
Task 16: complete (commits 143ea50.., API_REFERENCE.md created)

## Fix session 2026-09-07: duplicate script / GUID disaster resolved

Root cause of all CS0111 "namespace" errors: package copy at Assets/MatchThemAllTemplate/
was made while Unity was open with originals still present → Unity regenerated ALL .meta
GUIDs in the new location, while copied prefabs/scenes/assets still referenced the ORIGINAL
GUIDs. Old-location duplicates also compiled twice (CS0111).

Fix applied:
- Restored old Scripts from git, overwrote 202 new-location .meta files with originals (GUIDs restored)
- Copied 8 missing animation files + MTAInputSystem_Actions.inputactions into package
- Moved docs from nested "Match Them All/MatchThemAllTemplate/" to package root
- Deleted ALL old asset folders (Animation, Audio, Material, Models, Prefabs, Resources,
  Scenes, Settings, Sprites, Shader Graph, Scripts, inputactions) from "Assets/Match Them All/"
  — only working files remain (CLAUDE.md, docs, graphify-out, _START_HERE, etc.)
- Removed stray Input-System-generated duplicate MTAInputSystem_Actions.cs at package root

Verified: compiles with zero errors; 29 prefabs/scenes checked — 0 broken references,
0 missing scripts. Changes NOT yet committed (large move pending user review).

## Fix session 2 (2026-09-07 evening): missing scripts + dead references

User reported missing script connections on scene objects. Findings & fixes:
1. 10 scripts in Scripts/Runtime/Core (GameManager, InputManager, SoundManager, EventBus,
   DailyRewardManager, OpenSceneLoader, SceneLoader, SoundDataSO, DebugCheats,
   MTAInputSystem_Actions) had regenerated .meta GUIDs (Unity reimported the folder when
   the .inputactions moved into it). Restored original GUIDs from git HEAD metas.
2. _START_HERE folder (items + levels — the no-code entry point) had never been copied
   into the package. Copied to Assets/MatchThemAllTemplate/_START_HERE, removed original.
3. MainScene LevelManager.levels had 4 dead LevelDataSO GUIDs (pre-existing breakage from
   commit 6a16914). Reassigned to LevelData01–10 via editor script, scene saved.
4. Goal Card.prefab: dead sprite ref (5bdfb888) remapped to Goal_Card.png (4ab0fbe2).
5. Items/Cube|Sphere|Capsule.mat: dead custom shader (4e90a828, never committed) remapped
   to URP/Lit (933532a4).
6. IconScene: 3 dead item prefab refs (old "Bomb"/"Blue Potion"/"Heart Gem" names) remapped
   to Item_Bomb/Item_BluePotion/Item_HeartGem prefabs.
7. DefaultVolumeProfile.asset: stripped 4 dead VolumeComponents (Outline, TestAnimationCurve,
   OasisFog, TestVolume — leftover test junk with missing scripts).

Final verification: 0 unresolved GUIDs across ALL package assets, 0 missing scripts in all
scenes/prefabs, 0 null materials/dependencies. Play mode test from MainScene: ran ~30s,
0 errors, 0 exceptions; level spawned and items pooled correctly (stack trace verified
LevelManager→Level→ItemPlacer→ItemPoolManager chain). Only console entries: MCP plugin's
project-path-spaces notice (unrelated) and one benign Skull convex-mesh physics warning.

---

## Phase 1 Complete: Package Isolation & Structure ✅

All assets moved to MatchThemAllTemplate/ folder:
- Scripts: 98 (88 runtime + 10 editor)
- Prefabs: 23
- Scenes: 6
- Resources: 43
- Art: 91
- Audio: 21
- Shaders: 2

## Phase 2 Complete: Documentation ✅

Documentation files created:
- README.md (quick start guide)
- CHANGELOG.md (version history)
- LICENSE.txt (MIT license)
- THIRD_PARTY_NOTICES.txt (credits for third-party assets)
- CUSTOMIZATION_GUIDE.md (beginner customization guide)
- API_REFERENCE.md (programmer reference)

## Phase 3 Pending: Asset Cleanup & Legal Compliance

- [ ] Verify all third-party asset licenses
- [ ] Create placeholder assets for paid items (gems, VFX)
- [ ] Add "Replace Me" documentation
- [ ] Bundle ZLinq and NaughtyAttributes

## Phase 4 Pending: Validation & Polish

- [ ] Test package in fresh Unity project
- [ ] Run Asset Store Tools validator
- [ ] Fix any console errors/warnings
- [ ] Build test for Android/iOS

## Phase 5 Pending: Final Review

- [ ] Complete package structure review
- [ ] Documentation quality review
- [ ] Code quality review
- [ ] Asset Store submission checklist
