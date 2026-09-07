# Customization Guide

This guide shows you how to customize the Match Them All template for your game.

## Table of Contents
- [Adding a New Item Type](#adding-a-new-item-type)
- [Creating a New Level](#creating-a-new-level)
- [Changing Game Colors/Theme](#changing-game-colorstheme)
- [Adding a New Power-Up](#adding-a-new-power-up)
- [Replacing Placeholder Assets](#replacing-placeholder-assets)

---

## Adding a New Item Type

To add a new item type (like a heart, star, or diamond):

1. **Create a Prefab**
   - Create a new 3D object or import a model
   - Add the `Item` component to your GameObject
   - Save as a prefab in `Prefabs/Gameplay/`

2. **Add to EItemName Enum**
   - Open `Scripts/Runtime/Utilities/Enums/EItemName.cs`
   - Add your new item name to the enum
   ```csharp
   public enum EItemName
   {
       None,
       Gem,
       YourNewItem,  // <- Add here
       // ... other items
   }
   ```

3. **Configure in Template Editor**
   - Open Window > Match Them All > Template Editor
   - Add your new item configuration
   - Assign your prefab

4. **Test**
   - Open a level scene
   - Use the item in your level
   - Press Play to test

---

## Creating a New Level

1. **Open Level Editor**
   - Window > Match Them All > Template Editor

2. **Create New Level**
   - Click "New Level" button
   - Enter level name and settings
   - Configure grid size, target score, moves, etc.

3. **Design Layout**
   - Use the preview to place items
   - Add obstacles (blocks, crates, etc.)
   - Set up goals

4. **Save Level**
   - Click "Save Level"
   - Level is saved to Resources

5. **Add to Level Select**
   - Open LevelMapBuilder in Template Editor
   - Add your level to the map

---

## Changing Game Colors/Theme

1. **UI Colors**
   - Open `UIManager` script
   - Modify the color variables
   ```csharp
   [SerializeField] private Color primaryColor = Color.blue;
   [SerializeField] private Color secondaryColor = Color.white;
   ```

2. **Item Colors**
   - Open `GameSettingsSO` in Resources
   - Modify item color configurations
   - Each item type has its own color

3. **Background Colors**
   - Open your scene
   - Modify the Camera background color
   - Or add a Background sprite to UI

---

## Adding a New Power-Up

1. **Create Power-Up Prefab**
   - Create a 3D model for the power-up
   - Add your custom script
   - Save as prefab in `Prefabs/PowerUps/`

2. **Add to PowerupManager**
   - Open `PowerupManager.cs`
   - Add your power-up type to the switch statement
   - Implement the effect logic

3. **Create Power-Up Data**
   - Create a new PowerupDataSO
   - Configure cooldown, duration, cost

4. **Add UI**
   - Create power-up card UI
   - Add to power-up panel

---

## Replacing Placeholder Assets

This template ships with placeholder assets for models that require separate licensing.

### Gem Models

**Current:** Simple capsule with metallic material  
**Recommended:** BTM_Assets or your own models

**Steps:**
1. Import your gem models
2. Replace prefabs in `Prefabs/PowerUps/`
3. Update item configurations in Template Editor

### UI Elements

**Current:** Simple colored panels  
**Recommended:** Import a paid UI pack

**Steps:**
1. Import your UI pack
2. Replace UI prefabs in `Prefabs/UI/`
3. Update canvas scaler settings if needed

### VFX

**Current:** Basic Unity particle systems  
**Recommended:** SimpleFX or custom VFX

**Steps:**
1. Import VFX assets
2. Replace VFX prefabs in `Prefabs/VFX/`
3. Adjust particle system settings

---

## Getting Help

For detailed API documentation, see `API_REFERENCE.md`.
