# Adding a New Power-Up

Power-Ups are activated from the bottom UI bar during gameplay. Here is how to add a completely custom Power-Up.

---

## 1. Create a Game Action
Open `PowerupManager.cs` (`Scripts/Runtime/PowerUps/PowerupManager.cs`) and identify the core behavior block you want to execute (e.g. slowing time, freezing match timers, reshuffling board items).

## 2. Define Enum & Name Key
Open `EPowerupName.cs` and add your custom enum:
```csharp
public enum EPowerupName
{
    Fan,
    Vacuum,
    Freeze,
    Spring,
    MyNewPowerUp // <- ADD HERE
}
```

## 3. Author the PowerupDataSO
1. Right-Click in the Project View > **Create > Match Them All > Power-Up Data**.
2. Assign the ID name, cost-in-gems, UI icon Sprite, and Cooldown values via the Inspector.

## 4. Bind Logic in PowerupManager
In `PowerupManager.cs`, locate the `ActivatePowerup(EPowerupName type)` switch case block and append your logic:

```csharp
case EPowerupName.MyNewPowerUp:
    ExecuteMyNewPowerUpLogic();
    break;
```

## 5. Add to Central Database
Navigate to `Assets/MatchThemAllTemplate/Resources/Powerups/PowerupDatabase.asset` and drag your newly authored `PowerupDataSO` into the list of registered power-ups so the UI systems recognize and render it.
