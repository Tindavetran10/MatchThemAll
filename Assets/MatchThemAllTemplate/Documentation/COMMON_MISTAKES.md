# Common Mistakes & Troubleshooting

A quick guide to resolving common pitfalls when extending or modifying Match Them All.

---

## 1. Addressables & LevelData InvalidKeyException
- **Symptom:** `InvalidKeyException` or `List<string>` missing key error when loading levels on the saga map.
- **Root Cause:** A newly created `LevelDataSO` asset exists in your project but has not been added to the Addressables group labeled `"LevelData"`.
- **Solution:** Open **Window > Asset Management > Addressables > Groups**, drag your new `LevelDataSO` into the Default Local Group, and assign the label `"LevelData"`.

---

## 2. Items Flinging or Tunneling Through Walls
- **Symptom:** Fast-moving items bounce out of the playing area when power-ups (like Fan or Spring) are used.
- **Root Cause:** Rigidbody velocity exceeding physics collision sub-stepping thresholds.
- **Solution:** The `Item` component includes a `maxSpeed` clamp and continuous speculative collision detection. Ensure any custom item prefab retains the `Item` component and a configured `Rigidbody`.

---

## 3. Direct Modification of PlayerData
- **Symptom:** Currency, level progress, or settings do not persist across app restarts or scene loads.
- **Root Cause:** Directly writing to properties on `PlayerData` rather than calling API methods on `SaveManager.Instance`.
- **Solution:** Always invoke persistence through `SaveManager`:
  ```csharp
  // Correct:
  SaveManager.Instance.AddGems(50);
  
  // Incorrect:
  // SaveManager.Instance.PlayerData.Gems += 50;
  ```

---

## 4. Broken Inspector References After Renaming
- **Symptom:** Missing field warnings in Unity Console after refactoring code.
- **Root Cause:** Renaming serialized C# variables without `[FormerlySerializedAs("oldName")]`.
- **Solution:** When modifying serialized field names, either add `FormerlySerializedAs` or re-assign references in the inspector.
