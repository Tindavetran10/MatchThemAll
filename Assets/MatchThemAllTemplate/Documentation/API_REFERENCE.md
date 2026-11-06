# API Reference

This document provides technical reference for programmers who want to extend or modify the Match Them All template.

## Table of Contents
- [Core Systems](#core-systems)
- [Gameplay](#gameplay)
- [Power-Up System](#power-up-system)
- [UI System](#ui-system)
- [Save System](#save-system)
- [Event System](#event-system)

---

## Core Systems

### GameManager

The main game state machine.

```csharp
public class GameManager : MonoBehaviour
{
    public GameState CurrentState { get; }
    public static GameManager Instance { get; }
    
    public void ChangeState(GameState newState);
    public void StartGame();
    public void PauseGame();
    public void ResumeGame();
    public void EndGame();
}
```

### EventBus<T>

The event system used for cross-system communication.

```csharp
public class EventBus<T>
{
    public static void Publish(T @event);
    public static void Subscribe(Action<T> handler);
    public static void Unsubscribe(Action<T> handler);
}
```

### InputManager

Handles input routing and raycasting.

```csharp
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; }
    public Vector3 GetWorldPosition(Vector2 screenPosition);
    public bool Raycast(out RaycastHit hit);
}
```

---

## Gameplay

### Item

The core gameplay object.

```csharp
public class Item : MonoBehaviour
{
    public EItemName ItemName { get; set; }
    public Vector2Int GridPosition { get; set; }
    public bool IsSelected { get; set; }
    public bool IsMoving { get; }
    
    public void Select();
    public void Deselect();
    public void MoveTo(Vector2Int targetPosition);
    public void SetColor(Color color);
}
```

### ItemSpotManager

Manages the item grid and placement.

```csharp
public class ItemSpotManager : MonoBehaviour
{
    public static ItemSpotManager Instance { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    
    public Item GetItemAt(Vector2Int position);
    public bool IsSpotEmpty(Vector2Int position);
    public bool IsValidPosition(Vector2Int position);
    public void PlaceItem(Item item, Vector2Int position);
}
```

### MatchSystem

Handles matching logic.

```csharp
public class MatchSystem : MonoBehaviour
{
    public static MatchSystem Instance { get; }
    
    public List<Item> FindMatches();
    public bool IsMatch(List<Item> items);
    public void RemoveItems(List<Item> items);
}
```

### MergeManager

Handles item merging.

```csharp
public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; }
    
    public void MergeItems(List<Item> items);
    public void SpawnMergedItem(Item leftItem, Item rightItem);
}
```

---

## Power-Up System

### PowerupManager

Manages power-up activation and cooldowns.

```csharp
public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance { get; }
    public Dictionary<EItemName, PowerupDataSO> Powerups { get; }
    
    public bool CanActivate(EItemName powerupId);
    public bool ActivatePowerup(EItemName powerupId);
    public void DeactivatePowerup(EItemName powerupId);
    public void AddPowerup(EItemName powerupId, int quantity);
}
```

### PowerupEffect

Base class for power-up effects.

```csharp
public abstract class PowerupEffect : MonoBehaviour
{
    public abstract void Activate(Item target);
    public virtual void Deactivate();
}
```

---

## UI System

### UIManager

Manages UI panels and transitions.

```csharp
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; }
    public Canvas GroupCanvas { get; }
    
    public void ShowPanel(string panelName);
    public void HidePanel(string panelName);
    public void ShowScreen(string screenName);
    public void HideScreen(string screenName);
}
```

### GoalManager

Tracks and displays game goals.

```csharp
public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance { get; }
    public List<GoalData> CurrentGoals { get; }
    
    public void AddGoal(GoalData goal);
    public void RemoveGoal(GoalData goal);
    public void CheckGoals();
    public bool AllGoalsComplete();
}
```

### ComboManager

Tracks combo progress and multiplier.

```csharp
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; }
    public int ComboCount { get; }
    public float ComboMultiplier { get; }
    
    public void AddToCombo();
    public void ResetCombo();
    public void TriggerComboEffect();
}
```

---

## Save System

### SaveManager

Handles player data persistence.

```csharp
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; }
    public PlayerData CurrentData { get; }
    
    public void LoadData();
    public void SaveData();
    public void ResetData();
    public void IncrementGems(int amount);
    public void DecrementGems(int amount);
}
```

### PlayerData

The serializable data model.

```csharp
[System.Serializable]
public class PlayerData
{
    public int Gems;
    public List<int> UnlockedLevels;
    public List<PowerupData> UnlockedPowerups;
    public int CurrentLevel;
    public PlayerProgress Progress;
}
```

---

## Event System

### Game Events

All game events are defined in `GameEvents.cs`:

```csharp
public class GameStateChangedEvent
{
    public GameState PreviousState;
    public GameState NewState;
}

public class ItemClickedEvent
{
    public Item Item;
    public Vector3 ClickPosition;
}

public class ItemReachedSpotEvent
{
    public Item Item;
    public Vector2Int SpotPosition;
}

public class SpotFilledEvent
{
    public Vector2Int Position;
}

public class MergeStartedEvent
{
    public List<Item> MergingItems;
}
```

### Subscribing to Events

```csharp
EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

private void OnGameStateChanged(GameStateChangedEvent e)
{
    if (e.NewState == GameState.Playing)
    {
        // Game started
    }
}
```

---

## Configuration

### GameSettingsSO

ScriptableObject for game configuration.

```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    public float ItemDropSpeed;
    public float MoveAnimationSpeed;
    public int MaxComboMultiplier;
    public Color[] ItemColors;
}
```

---

## Utilities

### AdManager

The single entry point for ads. It creates itself on startup. It uses LevelPlay when the Ads Mediation
package is installed and mock ads otherwise. Setup: `Documentation/MONETIZATION_SETUP.md`.

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

### SoundManager

Plays sounds by `SoundEvent` id (clips are assigned in **Match Them All ▸ Sound Manager**).

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
