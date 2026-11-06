# Common Mistakes & Troubleshooting

Pitfalls to avoid when you extend or modify Match Them All. For problems while using the editor tools,
see also the troubleshooting table in [USER_GUIDE.md](USER_GUIDE.md#12-troubleshooting).

---

## 1. Levels don't load (`InvalidKeyException … List`1[System.String]`)
- **Cause:** no `LevelDataSO` matches the Addressables label `LevelData`, or there is no Addressables build.
  The `List<string>` in the message is Addressables wrapping the label. It means zero assets matched.
- **Fix:** create levels with the Template Editor's **+ New**, which adds the asset to the Default Local Group
  with the `LevelData` label. Then run *Window ▸ Asset Management ▸ Addressables ▸ Groups ▸ Build ▸ New Build
  ▸ Default Build Script*. Do this again after every level change, before a player build.

## 2. Items lose their colours when tapped
- **Cause:** `Item.Select` replaces the renderer's materials with `[base material, outline]`. A model with
  several materials or sub-meshes keeps only the first one.
- **Fix:** use one mesh with one material per item model.

## 3. Changing player data directly
- **Symptom:** coins, charges or progress don't persist, or the HUD doesn't update.
- **Cause:** writing to `PlayerData` fields instead of going through `SaveManager`.
- **Fix:** use the static `SaveManager` methods. They fire the change events and save:
  ```csharp
  SaveManager.AddCoins(50);                       // correct
  if (SaveManager.SpendCoins(100)) { /* … */ }    // false if the player can't afford it
  SaveManager.AddPowerupCharge("vacuum", 1);
  // wrong: editing PlayerData.coins directly
  ```

## 4. Spending currency without asking
- **Rule:** anything that costs coins must be an explicit player choice (for example the power-up offer
  popup), never an automatic fallback inside a tap handler.

## 5. Adding a game state
- **Cause:** `LevelManager` throws on `EGameState` values it doesn't know, and `TimerManager` must only
  resume a timer that was running.
- **Fix:** when you add a state, check every `GameStateChangedEvent` subscriber.

## 6. Tray logic that depends on timing
- **Cause:** tray items finish their slides in the same frame, in creation order. A check that runs only
  when the tapped item lands can miss a full tray.
- **Fix:** let `MatchSystem` decide (it rechecks when the tray settles). Skip items that are still
  `IsMovingToSpot` in your own tray code.

## 7. Using `??` or `?.` on Unity objects
- **Cause:** a destroyed `UnityEngine.Object` is "fake null". `GetComponent<T>() ?? AddComponent<T>()` never
  adds the component.
- **Fix:** use `TryGetComponent`, or an explicit `if (!obj)` check.

## 8. Referencing an ad or IAP SDK from game code
- **Cause:** `Unity.Services.LevelPlay` or `UnityEngine.Purchasing` types in Assembly-CSharp break the
  build for anyone without those packages.
- **Fix:** go through `AdManager`, `ShopManager`, and the `IAdService` / `IIapService` contracts.

## 9. Renaming serialized fields
- **Symptom:** missing references in the Inspector and prefabs after a rename.
- **Fix:** add `[FormerlySerializedAs("oldName")]` to the renamed field, or reassign the references.

## 10. Items flying out of the board
- **Cause:** a custom item prefab without the `Item` component's speed clamp, or without a `Rigidbody`.
- **Fix:** create items with the Item Manager. `Item` clamps speed (`maxSpeed`) and uses continuous
  speculative collision detection.
