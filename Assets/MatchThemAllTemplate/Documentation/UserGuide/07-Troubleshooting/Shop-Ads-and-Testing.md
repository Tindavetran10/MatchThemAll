# Shop, Ads and Testing

| Problem | Fix |
|---|---|
| Real-money products show **…** and can't be bought | Either In-App Purchasing isn't installed, the store can't be reached, or the **IAP Product Id** doesn't match the store exactly ([In-App Purchases](../05-Ads-and-In-App-Purchases/In-App-Purchases.md)). |
| No real ads in the Editor | That's expected. Real ads only run in an Android or iOS build, and the Editor uses pretend ads. |
| A power-up is still locked while testing | Your test save hasn't reached its unlock level. Set **Furthest level** in [Player Data](../03-Editor-Tools/Player-Data/README.md). |
| I want to test from a fresh start | In [Player Data](../03-Editor-Tools/Player-Data/README.md), set **Furthest level** to Level 1. Or click **Reset Level Progress** on the LevelManager to delete the whole save. |
| A tutorial didn't appear | A power-up tutorial waits until the power-up can be used. The Spring, for example, needs an item in the tray first. Also check that the **Powerup Id** matches the power-up's **Id** exactly. |

---
[↑ Troubleshooting](README.md) · [Back to the start →](../README.md)
