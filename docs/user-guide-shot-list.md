# User guide: screenshot shot list

Plain screenshots for the new user guide. Claude adds the numbered highlight boxes and writes the text under each one.
This file is for you, not for buyers. It lives in `docs/`, outside the template package.

## How to capture

- **One window per shot.** Use Win+Shift+S, choose *Window mode*, and click the editor window. Leave the Unity title bar out.
- **Same size every time.** Make the floating manager window about **1280 × 800** before you start, and don't resize it between shots.
- **Dark theme**, the default font size, and no personal paths or names on screen.
- **Save as PNG** in `docs/user-guide-shots/raw/` with the exact file name below. A shot is fine if it's a little off: the highlights are added afterwards, so you don't need to point at anything.
- If a shot needs a button held or a menu open, the note says so.

## Shots

### Menu
| File | What to show |
|---|---|
| `menu-match-them-all.png` | The Unity menu bar with **Match Them All** open, showing all six tools. |

### Template Editor (`Match Them All ▸ Template Editor`)
| File | What to show |
|---|---|
| `te-overview.png` | The whole window. Select **LevelData02** in the list and scroll to the top. |
| `te-new-level.png` | After clicking **+ New**: the name field with **✓** and **✕** visible. |
| `te-level-settings.png` | The **Level Settings** and **Rewards & Monetization** cards (Spot Count through Time For 2 Stars). |
| `te-configured-items.png` | **Configured Items** with a few rows, plus the **Prefab Library** and its Search box. |
| `te-tutorial-steps.png` | **Tutorial Steps** on LevelData02, with the Freeze step open. |
| `te-trash-health.png` | **Trash Bin** and **Item Reference Health**, after clicking **Check item references**. |
| `te-preview-play.png` | The **👁 Preview Layout** and **▶ Play Level** buttons, plus the Scene view after Preview Layout. A side-by-side capture is fine. |
| `te-settings-tab.png` | The Settings tab (**GLOBAL GAME SETTINGS**). |

### Item Manager (`Match Them All ▸ Item Manager`)
| File | What to show |
|---|---|
| `im-overview.png` | An item selected: **Dock Placement Overrides**, **Tray Slot (all items)** and the **3D Dock Preview**. |
| `im-dock-overrides.png` | The same item with **Use Custom Rotation** and **Use Custom Offset** both ticked, so their fields show. |
| `im-create.png` | After clicking **+ Create**: the whole **Item Prefab Generator**. |
| `im-new-type.png` | After clicking **New Type...**: the name field and **Add**. |
| `im-icon-settings.png` | **Auto-Generate Icon** ticked, showing **Icon Camera Settings** and the icon preview. |

### Shop Manager (`Match Them All ▸ Shop Manager`)
| File | What to show |
|---|---|
| `sh-overview.png` | The **Coins** tab with **500 Coins** selected, scrolled to the top. |
| `sh-tabs.png` | The tab bar with **+**, **◀**, **▶** and **✕**, after clicking **+** so the **New tab id** field shows. |
| `sh-product-rewards.png` | A product with **Rewards** and **First Purchase Bonus** expanded. |
| `sh-merchandising.png` | Scrolled down to **Merchandising** (badges and Description). |

### Sound Manager (`Match Them All ▸ Sound Manager`)
| File | What to show |
|---|---|
| `so-overview.png` | A sound effect selected (e.g. **Item Select**): Clips, Playback and Mixer Routing. |
| `so-music.png` | **Music Menu** selected. |

### Power-up Manager (`Match Them All ▸ Power-up Manager`)
| File | What to show |
|---|---|
| `pu-overview.png` | **Freeze Gun** selected, scrolled to the top (Identity, Tray, Preview). |
| `pu-details.png` | The same power-up scrolled down: Progression, Economy, Mechanic, Effects & Sound. |
| `pu-custom.png` | A custom power-up (click **Duplicate** on Fan, then switch its Slot prefab to **PowerupSlot**) so the **Tray model** field shows. Delete it afterwards. |
| `pu-new-mechanic.png` | The **New Mechanic…** bar open, with a name typed in. |

### Player Data (`Match Them All ▸ Player Data`)
| File | What to show |
|---|---|
| `pd-window.png` | The whole window, with the **Furthest level** dropdown open. |

### Settings assets (Inspector)
| File | What to show |
|---|---|
| `inspector-game-settings.png` | `Resources/GameSettings` in the Inspector (daily rewards and global settings). Two captures if it's tall. |
| `inspector-monetization.png` | `Resources/Monetization/MonetizationSettings` in the Inspector. Two captures if it's tall. |
| `inspector-level-manager.png` | MainScene **LevelManager** in the Inspector, showing the **Reset Level Progress** button. |

Total: 28 shots.

## New guide structure (for reference)

```
Documentation/UserGuide/
  README.md                       ← start page and contents
  01-Getting-Started/             requirements, first run
  02-How-The-Game-Plays/          GIF clips: tap & match, goals & stars, power-ups, win/lose, lobby
  03-Editor-Tools/                tour of the Match Them All menu, then one folder per tool:
    Template-Editor/              window tour, create a level, settings, items, tutorial steps, trash & health, global settings
    Item-Manager/                 window tour, create an item, dock placement, tray slot look, icon generator
    Shop-Manager/                 window tour, tabs, products (identity, price, rewards, merchandising)
    Sound-Manager/                window tour, clips, volume & pitch, mixer routing
    Power-up-Manager/             window tour, edit, tray order, new power-up, new mechanic
    Player-Data/                  coins, gems, charges, furthest level
  04-Daily-Rewards-and-Settings/
  05-Ads-and-In-App-Purchases/
  06-Build-for-Android-and-iOS/
  07-Troubleshooting/
```
