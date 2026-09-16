# Shop & Economy Configuration

The template includes a modular soft-currency (Gems) economy system that reads isolated definitions from `ShopDatabaseSO` assets.

---

## 1. Accessing the Shop Editor
Navigate to **Match Them All > Shop Editor**.
This custom window allows you to author new shop tabs, product bundles, and currency packages visually without hunting through ScriptableObjects.

## 2. Defining a Product
In the Shop Editor, click **Add Product**:
- **Product ID:** The unique string identifier.
- **Purchase Type:** Whether this costs soft currency (Gems) or invokes real-money IAP flow (placeholder validation).
- **Reward:** The payload added to `SaveManager.Instance.PlayerData` upon success.
- **Visuals:** Provide an Icon sprite and Title string.

## 3. Product Presentation
Products instantiate using variations of the `ShopProductCard.prefab`. You can swap out layouts or create specialized presentation cards for VIP bundles by altering the `Prefabs/UI/Shop` folder assets and linking them in the Shop Editor.

## 4. Integration with Validations
The `ShopManager.cs` currently mocks real-money transactions (always simulating success). To integrate Apple IAP or Google Billing, locate the `ProcessRealCurrencyTransaction(ShopProductSO)` stub method and hook up standard Unity IAP validation callbacks.
