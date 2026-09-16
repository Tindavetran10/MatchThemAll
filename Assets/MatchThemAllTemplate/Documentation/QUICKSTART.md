# Quickstart Guide

Get up and running with **Match Them All Template** in under 5 minutes!

---

## 1. Project Requirements
- **Unity Version:** Unity 2021.3 LTS or newer (Universal Render Pipeline / URP).
- **Dependencies:** 
  - *NaughtyAttributes* (bundled under `ThirdParty/NaughtyAttributes`)
  - *ZLinq* (included via Package Manager / UPM)
  - *Addressables* (com.unity.addressables)

---

## 2. Running the Game
1. Open the project in Unity Editor.
2. In the Project window, navigate to:  
   `Assets/MatchThemAllTemplate/Scenes/`
3. Open `Lobby.unity` or `MainScene.unity`.
4. Press the **Play** button in the Unity Editor toolbar.

---

## 3. Creating Your First Level
1. Go to the top menu bar: **Match Them All > Template Editor** (or **Level Editor**).
2. Click **New Level** to create a new level configuration.
3. Configure your target goals (e.g., 3 Blue Potions, 3 Bombs), timer countdown, and item pool.
4. Click **Save Level**. The level will be saved as a ScriptableObject under `_START_HERE/Levels/`.
5. Open `LevelMapBuilder` in the editor to register your level on the saga map.

---

## 4. Customizing Items
- All starter items are located in `Assets/MatchThemAllTemplate/_START_HERE/Items/`.
- Open **Match Them All > Item Manager** to view 3D previews, configure dock alignment offsets, or capture high-resolution icons.

---

## 5. Next Steps
- Read [CUSTOMIZATION_GUIDE.md](CUSTOMIZATION_GUIDE.md) to learn how to add custom items, powerups, and visual themes.
- Read [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md) for a breakdown of EventBus and manager lifecycles.
- Read [REPLACING_PLACEHOLDERS.md](REPLACING_PLACEHOLDERS.md) to replace placeholder gems with your custom models.
