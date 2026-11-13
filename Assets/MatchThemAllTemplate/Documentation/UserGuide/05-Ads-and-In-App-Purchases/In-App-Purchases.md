# In-App Purchases

To sell a shop product for real money:

1. Install **In-App Purchasing** (*Window ▸ Package Manager ▸ Unity Registry*).
2. Create the product in **Google Play Console** and/or **App Store Connect**. Note its **product id**.
3. In the [Shop Manager](../03-Editor-Tools/Shop-Manager/Products.md), select the product and paste that id into
   **IAP Product Id**.

The shop card now shows the store's price in the player's currency, and the coin *Amount* is ignored. Rewards,
badges and **Is One Time** work the same as for coin products.

- **Restore Purchases** appears on iOS, so players get their one-time purchases back on a new device.
- A purchase is only confirmed to the store **after** the reward is saved, so a crash can't lose it.
- In the Editor, a fake store window appears when you buy, so you can test without spending money.

> Until In-App Purchasing is installed and the store can be reached, real-money cards show **…** and can't be
> bought.

---
[↑ Ads and In-App Purchases](README.md) · Next: [Build for Android and iOS →](../06-Build-for-Android-and-iOS/README.md)
