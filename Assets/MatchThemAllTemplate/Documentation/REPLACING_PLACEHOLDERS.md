# Replacing Placeholder Assets

The template ships with clean, lightweight geometric placeholders (spheres, capsules, cubes, cylinders, and toruses) for easy prototyping and complete licensing independence.

Here is how to swap them out for production-ready 3D models and visuals.

---

## 1. Replacing 3D Item Models
1. Import your 3D model (FBX, OBJ, or prefab) into `Assets/MatchThemAllTemplate/Art/Models/`.
2. Open the Item prefab you want to update (e.g. `_START_HERE/Items/Item_HeartGem.prefab`).
3. In the Inspector:
   - Replace the `MeshFilter.sharedMesh` with your new 3D mesh.
   - Replace the `MeshRenderer.sharedMaterial` with your material/texture.
   - If using a `MeshCollider`, update its `sharedMesh` and ensure `Convex` is enabled.
4. Open **Match Them All > Item Manager** to adjust dock rotation and icon captures.

---

## 2. Replacing UI Sprites
1. Import your UI textures and icons into `Assets/MatchThemAllTemplate/Art/Sprites/`.
2. Select your imported images and ensure **Texture Type** is set to **Sprite (2D and UI)**.
3. Open UI prefabs in `Assets/MatchThemAllTemplate/Prefabs/UI/` (e.g., `Goal Card.prefab`, `ShopProductCard.prefab`, `DailyRewardPanel.prefab`).
4. Swap the `Image.sprite` field with your new sprite assets.

---

## 3. Replacing Audio & SFX
1. Import audio files (`.wav`, `.mp3`, `.ogg`) into `Assets/MatchThemAllTemplate/Audio/Audio Clip/`.
2. Navigate to `Assets/MatchThemAllTemplate/Audio/Audio Data/`.
3. Open the corresponding `SoundDataSO` asset (e.g. `SFX_Merge.asset`, `SFX_LevelComplete.asset`) and assign your new audio clip.
