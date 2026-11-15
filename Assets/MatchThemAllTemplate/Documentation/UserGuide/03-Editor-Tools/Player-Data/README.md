# Player Data

Open it from *Match Them All ▸ Player Data*. This window changes your own test save on this computer, and it
never touches real players. Use it to test the shop with piles of coins, to try a power-up at 0 charges, or to
replay the unlocks from the start.

![The Player Data window](../../Images/tools/pd-window.png)

| # | Field | What it does |
|---|---|---|
| 1 | **Coins** / **Gems** | How many you have. |
| 2 | **Power-up Charges** | Charges for each power-up. Set one to 0 to test the "buy or watch an ad" popup. |
| 3 | **Furthest level** | How far you've got. Later levels are locked, and so are power-ups that unlock later. Level 1 is like a fresh start. Moving it back also clears the stars from that level onward, while earlier levels keep theirs. |
| 4 | **Reload** | Reads the save again and throws away what you typed. |
| 5 | **Apply** | Writes your values to the save. It works with the game stopped or running, and while it runs the power-up bar updates straight away. |

> To start completely fresh, select the **LevelManager** object in `MainScene` and click **Reset Level Progress**
> (1) in the Inspector. That deletes the whole save: coins, gems, charges and progress.

![Reset Level Progress on the LevelManager](../../Images/tools/inspector-level-manager.png)

---
[↑ Editor Tools](../README.md) · Next: [Daily Rewards and Settings →](../../04-Daily-Rewards-and-Settings/README.md)
