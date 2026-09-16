# Level Creation Tutorial

Creating and mapping custom levels is streamlined via custom Editor tooling. Follow this guide to author your first level.

---

## Step 1: Open the Template Editor
Navigate to **Match Them All > Template Editor** in the top Unity menu bar.

## Step 2: Define Level Properties
Click the **"New Level"** button to start a fresh configuration.
- **Level Index:** Set the sequential ID (e.g., 5).
- **Time/Move Limit:** Set countdown duration or max move restrictions.
- **Target Goals:** Add `EItemName` objectives and required quantities (e.g., 5 Diamonds, 10 Coins).
- **Item Pool:** Define the total pool of 3D items generated and matched within the level.

## Step 3: Design the Physical Layout (Optional)
Toggle **Show Scene Layout Preview** to see how items instantiate. Adjust boundaries, spawn densities, or grid locations if you are defining structured start positions.

## Step 4: Save & Map
1. Click **Save Configuration**. The `LevelDataSO` asset is automatically added to `_START_HERE/Levels/`.
2. Ensure you have the `Addressables` window open, and tag your new `LevelDataSO` with the `LevelData` Addressables label.
3. In the Template Editor, switch to the **Level Map Manager** tab to bind this `LevelDataSO` to a visual node button on the `LevelSelect` scene map.
