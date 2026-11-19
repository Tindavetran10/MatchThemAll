# API Reference

The public API of the classes you are most likely to call when you extend the template. Signatures are
copied from the source. Fields, private members and Unity messages are left out. For how to use the editor
tools, see [USER_GUIDE.md](USER_GUIDE.md); for how the systems fit together, see
[ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md).

Namespaces: most gameplay code is in `MatchThemAll.Scripts`. Power-ups are in
`MatchThemAll.Scripts.Power_Ups`, the shop in `.Shop`, saving in `.SaveSystem`, ads in `.Utilities`, the
ad/IAP contracts in `.Monetization`, and editor tools in `MatchThemAll.Scripts.Editor`.

## Contents
- [Events](#events)
- [Game flow](#game-flow)
- [Gameplay](#gameplay)
- [Power-ups](#power-ups)
- [Save system](#save-system)
- [Shop, ads and IAP](#shop-ads-and-iap)
- [Sound](#sound)
- [Data assets](#data-assets)

---

## Events

### EventBus

A static, type-keyed event bus. Events are `struct`s, defined in `Scripts/Runtime/Core/GameEvents.cs`.

```csharp
public static class EventBus
{
    public static void Subscribe<T>(Action<T> listener) where T : struct;
    public static void Unsubscribe<T>(Action<T> listener) where T : struct;
    public static void Publish<T>(T eventData) where T : struct;
    public static void ClearAll();
}
```

```csharp
private void Awake()     => EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
private void OnDestroy() => EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

private void OnGameStateChanged(GameStateChangedEvent e)
{
    if (e.NewState == EGameState.LEVELCOMPLETE) { /* the player won */ }
}
```

### Game events

| Event | Payload | Raised when |
|---|---|---|
| `GameStateChangedEvent` | `EGameState NewState` | `GameManager.SetGameState` changes state |
| `ItemClickedEvent` | `Item ClickedItem` | the player taps an item on the board |
| `ItemReachedSpotEvent` | `Item Item` | a tapped item lands in the tray (not when it only slides along) |
| `MergeStartedEvent` | `List<Item> MergedItems` | three of a kind start merging |
| `SpotFilledEvent` | — | the tray is full with no merge possible (Game Over) |
| `PowerupClickedEvent` | `PowerupDataSO Powerup` | a power-up is activated |
| `PowerupOfferRequestedEvent` | `PowerupDataSO Powerup` | a power-up with 0 charges is tapped (opens the coins/ad popup) |
| `PowerupItemPickedUpEvent` | `Item PickedItem` | a power-up takes an item off the board |
| `PowerupItemBackToGameEvent` | `Item ReturnedItem` | a power-up returns a tray item to the board |
| `TimerFrozenEvent` | `float Duration` | the Freeze Gun stops the clock |
| `TimerUnfrozenEvent` | — | the freeze ends |
| `ShopPurchaseSucceededEvent` | `ShopProductSO Product` | a shop purchase was granted |

`LevelSpawnedEvent` and `ItemPickedUpEvent` are defined but not raised by the template. To react to a new
level, use `LevelManager.LevelSpawned`. To react to an item leaving the board, use
`ItemSpotManager.ItemPickedUp`.

### EGameState

`MENU`, `GAME`, `LEVELCOMPLETE`, `GAMEOVER`, `PAUSED`, `OUTOFTIME`, `POWERUPOFFER` (the 0-charge popup:
the timer and input pause, the HUD stays). If you add a state, check every `GameStateChangedEvent`
subscriber: `LevelManager` throws on states it doesn't know.

---

## Game flow

### GameManager

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public EGameState State { get; }
    public EGameState PreviousState { get; }

    public bool IsGame();                       // State == GAME
    public void SetGameState(EGameState next);  // publishes GameStateChangedEvent
    public void StartGame();                    // GAME
    public void PauseGame();                    // PAUSED
    public void ResumeGame();                   // GAME
    public void QuitToMenuCallback();           // UI button: back to the Lobby
    public void NextButtonCallback();           // UI button: next level
    public void RetryButtonCallback();          // UI button: same level again
}
```

### SceneLoader

```csharp
public static class SceneLoader
{
    public static string TargetScene { get; }
    public static int RequestedLevelIndex { get; }   // -1 = the player's current level
    public static void Load(string sceneName);         // saves, fades, LoadingScene, then sceneName
    public static void LoadLevel(int levelIndex = -1); // same, into MainScene at that level
}
```

### LevelManager

```csharp
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public static Action<Level> LevelSpawned;

    public List<Item> Items { get; }                 // every item of the current level
    public Transform ItemParent { get; }
    public int CurrentLevelIndex { get; }
    public LevelDataSO CurrentLevelData { get; }
    public int TotalLevelDuration { get; }
    public int TotalLevelCount { get; }
    public IReadOnlyList<string> OrderedLevelIds { get; }
    public Task LoadTask { get; }                    // levels load from Addressables (label "LevelData")
    public bool IsLevelReady { get; }

    public void SaveLevelComplete(int starsEarned);
    public void ResetLevelProgress();
}
```

### TimerManager

```csharp
public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;
    public int CurrentTime { get; }          // seconds left
    public bool IsFrozen { get; }
    public float FreezeRemaining01 { get; }

    public void FreezeTimer();
    public void AddTime(int seconds);
    public void SetTutorialPause(bool isPaused);
}
```

### TutorialManager

```csharp
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    public bool IsTutorialRunning { get; }
    public void CompleteCurrentStep();       // ends a step whose condition is Manual
}
```

---

## Gameplay

### Item

```csharp
public class Item : MonoBehaviour
{
    public EItemName ItemNameKey { get; }
    public ItemSpot Spot { get; }            // null while on the board
    public bool IsMovingToSpot { get; set; }

    public void AssignSpot(ItemSpot spot);
    public void UnassignSpot();
    public void EnablePhysics();  public void DisablePhysics();
    public void EnableShadow();   public void DisableShadow();
    public void Select(Material outlineMaterial);   // adds the outline as a second material
    public void Deselect();
    public void ApplyRandomForce(float magnitude);
    public void ResetState();
}
```

Item models must use one material (see the User Guide, section 5).

### ItemSpotManager (the tray)

```csharp
public class ItemSpotManager : MonoBehaviour
{
    public static ItemSpotManager Instance { get; }
    public static Action<Item> ItemPickedUp;

    public ItemSpot GetRandomOccupiedSpot();             // ignores items still sliding
    public bool HasItemOnBoard(EItemName itemName);      // "board" here means the tray
    public List<Item> GetItemsOnBoard(EItemName itemName);
    public IEnumerable<EItemName> GetOccupiedItemTypes();
    public bool IsBoardFullAndNoItemsMoving();
    public List<Item> GetBestHintItems(int count = 3);
}
```

### MatchSystem

Decides merges and the full-tray Game Over. It runs when a tapped item lands
(`ItemReachedSpotEvent`) and when the tray settles after items were pushed aside.

```csharp
public class MatchSystem : MonoBehaviour
{
    public static void CheckSettledTray();
}
```

### GoalManager, ComboManager

```csharp
public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance;
    public ItemLevelData[] Goals { get; }    // amount = items still to collect
}

public class ComboManager : MonoBehaviour
{
    public static Action<int> OnComboUpdated;
}
```

`MergeManager` (merge animation) and `HintManager` (idle hints) have no public API. They react to events.

### InputManager

```csharp
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; }
    public static bool IsTutorialActive;
    public static Item[] TutorialTargets;    // while a tutorial runs, only these can be tapped
    public Material OutlineMaterial { get; }
    public bool IsPointerActive { get; }
}
```

---

## Power-ups

Each power-up is a `PowerupDataSO` asset whose behaviour is a `PowerupEffect` subclass, stored with
`[SerializeReference]`. To add one, write a subclass and create an asset. There is no enum or switch to
edit. The steps are in the User Guide, section 8.

```csharp
[Serializable]
public abstract class PowerupEffect
{
    public abstract bool CanActivate(PowerupContext ctx);
    public abstract void Activate(PowerupContext ctx);
}

public struct PowerupContext
{
    public List<Item> Items;
    public ItemLevelData[] Goals;
    public ItemSpotManager ItemSpots;
    public TimerManager Timer;
    public GameSettingsSO GameSettings;
    public Transform VacuumSuckPosition, FanOrigin, FreezeMuzzle;
    public Transform Source;                 // the tapped power-up's prop in the tray
    public ParticleSystem ActivateVfx, EndVfx;
    public Action<Item> OnItemPickup, OnItemBackToGame;
    public Action<bool> SetBusy;             // true while running; set false when done
}

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance { get; }
    public event Action<Item> OnItemPickup;
    public event Action<Item> OnItemBackToGame;
    public void ForceActivate(string id);    // testing: ignores unlock level and charges; GAME state only
}
```

`PowerupDataSO` fields: `id`, `displayName`, `icon`, `order`, `unlockLevel`, `defaultAmount`,
`buyCurrency`, `buyCost`, `uiPrefab`, `effect`, `activateVfx`, `endVfx`. All of them are listed in
`Resources/Powerups/PowerupDatabase.asset`.

---

## Save system

`SaveManager` is a static class. It loads `save.json` from `Application.persistentDataPath` on startup.
Always change player data through it, never through `PlayerData` directly.

```csharp
public static class SaveManager
{
    public static event Action<int> OnCoinsChanged;
    public static event Action<int> OnGemsChanged;
    public static event Action OnPowerupsChanged;

    // Currency
    public static int  GetCoins();   public static void AddCoins(int amount);   public static bool SpendCoins(int amount);
    public static int  GetGems();    public static void AddGems(int amount);    public static bool SpendGems(int amount);
    public static int  GetCurrency(ECurrency currency);
    public static bool Spend(ECurrency currency, int amount);   // false (and nothing spent) if too poor

    // Power-ups
    public static int  GetPowerupCount(string id);
    public static bool UsePowerupCharge(string id);
    public static void AddPowerupCharge(string id, int amount);

    // Levels
    public static int    GetCompletedLevelCount();
    public static int    GetCurrentLevelIndex();
    public static string GetFurthestLevelId();
    public static int    GetLevelStars(string id);
    public static bool   IsLevelUnlocked(string id, IReadOnlyList<string> orderedIds);
    public static void   SetLevelComplete(string id, int stars, bool isFurthest, IReadOnlyList<string> orderedIds);

    // Shop and purchases
    public static bool OwnsEntitlement(string id);      // e.g. EntitlementIds.RemoveAds
    public static bool GrantEntitlement(string id);
    public static bool HasClaimedFirstBonus(string productId);
    public static bool MarkFirstBonusClaimed(string productId);
    public static bool IsIapTransactionProcessed(string transactionId);
    public static void MarkIapTransactionProcessed(string transactionId);

    // Privacy, daily rewards, settings
    public static bool? GetGdprConsent();               // null = not asked yet
    public static void  SetGdprConsent(bool accepted);
    public static (string lastPlayedDate, int loginStreak) GetDailyRewardData();
    public static void  SaveDailyRewardData(int loginStreak, string lastPlayedDate);
    public static (float musicVolume, float sfxVolume, bool hapticsEnabled) GetSettings();
    public static void  SaveMusicVolume(float value);
    public static void  SaveSfxVolume(float value);
    public static void  SaveHaptics(bool enabled);

    // File
    public static void Flush();          // write now
    public static bool TryFlush();       // write now; false if the write failed
    public static void Wipe();           // delete all progress
    public static void ForceReload();
}
```

---

## Shop, ads and IAP

### ShopManager

```csharp
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; }
    public event Action PricesUpdated;                          // store prices arrived

    // Coin/gem products: returns the result now. Real-money products: returns false at once,
    // onComplete reports the real result after the store answers.
    public bool TryPurchase(ShopProductSO product, Action<bool> onComplete = null);
    public string GetLocalizedPrice(ShopProductSO product);     // store price, or "…" until known
    public void RestorePurchases(Action<bool> onComplete = null);
    public void SetIapService(IIapService service);
}
```

Store orders are granted in one place, `ShopManager.GrantStorePurchase`. It records the transaction id and
saves before the order is confirmed with the store.

### AdManager

The single entry point for ads. It creates itself on startup. It uses LevelPlay when the Ads Mediation
package is installed and mock ads otherwise. Setup: [MONETIZATION_SETUP.md](MONETIZATION_SETUP.md).

```csharp
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; }
    public bool IsReady { get; }               // SDK started (after consent / tracking prompts)
    public bool IsRewardedAvailable { get; }

    // onRewardEarned only after the video was watched to the end.
    public void ShowRewardedAd(Action onRewardEarned, Action onFailed = null, string placement = "default");
    // Paced interstitial on a level transition; always runs `next`, with or without an ad.
    public void ShowInterstitialThen(Action next, string placement = "level_transition");
}
```

To use another ad network or store, implement `IAdService` / `IIapService` (in
`Scripts/Runtime/Monetization`) and register it with `MonetizationServices` before the first scene loads.

---

## Sound

Plays sounds by `SoundEvent` id. Clips are assigned in **Match Them All ▸ Sound Manager**.

```csharp
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; }

    public static void Sfx(SoundEvent id);      // safe when no manager or clip exists
    public void PlayMusic(SoundEvent id);
    public void SetSFXVolume(float volume01);
    public void SetMusicVolume(float volume01);
}
```

---

## Data assets

| Asset type | Create menu | Edited with |
|---|---|---|
| `LevelDataSO` | Match Them All ▸ Level Data | Template Editor |
| `GameSettingsSO` | Match Them All ▸ Game Settings | Template Editor ▸ Settings |
| `PowerupDataSO`, `PowerupDatabaseSO` | Match Them All ▸ Powerup Data / Powerup Database | Inspector |
| `ShopDatabaseSO`, `ShopTabSO`, `ShopProductSO` | Match Them All ▸ Shop Database / Shop Tab / Shop Product | Shop Manager |
| `SoundLibrarySO` | Match Them All ▸ Sound Library | Sound Manager |
| `MonetizationSettingsSO` | Match Them All ▸ Monetization Settings | Inspector |

`ItemLevelData` (one row of a level): `itemPrefab`, `isGoal`, `amount` (a multiple of 3), `multiplier`
(board copies = amount × multiplier).
