# Match Them All — Feature Roadmap
> Last updated: 2026-06-13  
> Status key: 🔴 Not started | 🟡 In progress | ✅ Done

---

## ✅ Already Implemented

| System | Notes |
|---|---|
| Level data (LevelDataSO) | ScriptableObject, loaded from Resources/Levels |
| Item spawning + physics | Seeded random, spawn zone BoxCollider |
| 7-slot bottom row + merge logic | ItemSpotManager, MergeManager |
| Goals tracking | GoalManager, GoalCard UI |
| Timer | TimerManager, FreezeTimer powerup |
| Powerups: Vacuum, Spring, Fan, Freeze | PowerupManager, counts saved |
| Save / Load | PlayerData via SaveManager (JSON) |
| Stars (1–3) on level complete | Saved per level |
| Tutorial | TutorialManager |
| Level Editor (Unity Editor tool) | LevelEditorWindow, ItemCreatorWindow |

---

## 🔴 Sprint 1 — Core Gameplay Loop (do BEFORE visuals & SFX)

### 1. Combo System
**Priority:** High  
**Why:** #1 engagement mechanic in the genre. Rewards fast, skilled play.  
**How it works:**
- Merging 3 of the same type within ~2s of the previous merge = combo
- Combo multiplier: ×2, ×3, ×4 (resets on timeout or wrong placement)
- Each combo level adds bonus time (e.g. +2s per combo tick)

**Files to touch:**
- `ItemSpotManager.cs` → `MergeItems()` — fire a combo event here
- New: `ComboManager.cs` — tracks combo counter + timer, exposes `Action<int> ComboUpdated`
- `TimerManager.cs` → add `AddTime(float seconds)` method
- UI: combo label / pulse animation on HUD

---

### 2. Time Bonus on Goal Complete
**Priority:** High  
**Why:** Completing a goal currently has zero reward feel. +time = instant gratification.  
**How it works:**
- Each time a full goal is completed, add +5 seconds to the timer
- Show a floating "+5s" text at the goal card position

**Files to touch:**
- `GoalManager.cs` → `CompleteGoals()` — call `TimerManager.Instance.AddTime(5)`
- `TimerManager.cs` → add `AddTime(float seconds)`
- UI: floating text spawner (reuse for combo too)

---

### 3. Coin Economy
**Priority:** High  
**Why:** Currency is the backbone of the powerup and monetization loop.  
**How it works:**
- Award coins on level complete: 1 star = 10, 2 stars = 20, 3 stars = 30
- Display coin count in HUD and main menu
- Powerups can be purchased with coins (in addition to existing charges)
- Coins saved in `PlayerData`

**Files to touch:**
- `PlayerData.cs` (SaveSystem) → add `int coins` field
- `LevelManager.cs` → `SaveLevelComplete()` — award coins based on stars
- New: `CoinManager.cs` (or fold into SaveManager) — `AddCoins()`, `SpendCoins()`
- UI: coin display widget, coin reward popup on win screen

---

## 🔴 Sprint 2 — Retention & Progression

### 4. Continue After Game Over (Watch Ad / Spend Coins)
**Priority:** High  
**Why:** Prevents hard exits. Standard in every game in this genre.  
**How it works:**
- On `GAMEOVER` state: show "Continue?" panel with 5-second countdown
- Option A: Watch rewarded ad → +15 seconds, resume
- Option B: Spend 30 coins → +15 seconds, resume
- If countdown expires → show normal game over screen

**Files to touch:**
- `GameManager.cs` / `TimerManager.cs` → pause timer on GAMEOVER
- New: `ContinuePanel.cs` UI script
- `TimerManager.cs` → `AddTime()` + resume

---

### 5. Hint System
**Priority:** Medium  
**Why:** Reduces frustration for stuck players. Standard casual mechanic.  
**How it works:**
- Costs 1 hint charge (starts with 3, purchasable with coins)
- Scans `_itemMergeDataDictionary` in ItemSpotManager for an item type already in the bottom row
- If found → highlight that item type in the pile (outline glow)
- If not found → highlight the item type with the highest remaining goal count

**Files to touch:**
- `ItemSpotManager.cs` → expose `GetBestHintItem()` method
- `PowerupManager.cs` → add `HintPowerup()` to the switch
- New: `EPowerupType.Hint` enum value
- UI: hint button, item highlight shader/outline effect

---

### 6. Configurable Slot Count per Level
**Priority:** Medium  
**Why:** Easy difficulty scaling without changing item counts. 5 slots = brutal, 9 slots = easy.  
**How it works:**
- Add `int spotCount` field to `LevelDataSO` (default 7)
- On level spawn, `ItemSpotManager` hides/shows spots based on SO value
- Level Editor window already supports adding new SO fields

**Files to touch:**
- `LevelDataSO.cs` → add `spotCount` (default 7, range 5–9)
- `ItemSpotManager.cs` → `OnLevelSpawned()` — activate only first N spots
- `LevelEditorWindow.cs` → add slider for spot count in Settings Card

---

### 7. Streak / Daily Reward
**Priority:** Medium  
**Why:** Day-1 and day-7 retention. Simple to implement.  
**How it works:**
- Save `lastPlayedDate` (ISO string) in `PlayerData`
- On app open: compare date to today
  - Same day → no reward
  - 1 day later → streak continues, show reward popup (coins/powerups)
  - 2+ days later → streak resets to 1
- Rewards escalate by streak day (day 1: 10 coins, day 7: 1 of each powerup)

**Files to touch:**
- `PlayerData.cs` → add `string lastPlayedDate`, `int loginStreak`
- New: `DailyRewardManager.cs` → check on `Awake`, fire reward event
- UI: daily reward popup panel

---

## 🔴 Sprint 3 — Polish Features (after visuals & SFX)

### 8. Score System
Award points per merge scaled by combo multiplier. Displayed on win screen and leaderboard-ready.

### 9. Level Difficulty Label
Add `EDifficulty` (Easy / Normal / Hard) to `LevelDataSO`. Display badge on level select and HUD. Hard = shorter timer or fewer slots.

### 10. Shuffle Powerup (upgrade Fan)
A "controlled" Fan — teleports all pile items to new random spawn positions instead of applying physics force. Cleaner feel.

---

## Notes & Decisions

- **No PvP or async multiplayer** — out of scope for this project size
- **IAP shop** — implement after gameplay is fully locked (post-Sprint 2)
- **All powerup charges** should eventually be purchasable with coins, not just refilled by the current hardcoded `_vacuumPuCount = 3` fallback in `PowerupManager`
- **ComboManager and CoinManager** should be singletons registered in the same scene as GameManager
- **Floating text** (for +5s, combo, coins) — build ONE reusable `FloatingTextSpawner` component, not separate systems per feature
