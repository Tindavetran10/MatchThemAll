# Coin/gem fly-to-counter effect and win rewards

**Date:** 2026-11-08

## Context
Granted coins only changed the number on the counter. The Level Complete panel didn't show what it granted.

## Decisions
- **`CurrencyGrantedEvent`** (`GameEvents.cs`: currency, amount, screen position, delay) is published right after
  `SaveManager.AddCoins/AddGems` at each grant site: `WinPanelManager`, `DailyRewardManager.GrantReward` and
  `ShopManager.GrantRewards` (soft-currency and store purchases). Grant sites don't reference any UI.
- **`CurrencyFlyEffect` sits on the `CoinPill` / `GemPill` prefabs** (no scene manager). It reacts only to its own
  currency, so any scene with a pill gets the effect. It pools copies of the pill's own `Icon` (no separate coin
  prefab, and gems work too) on a `CurrencyFlyLayer` canvas (override sorting 500) under the root canvas.
- **The counter can't drift from the save.** While coins fly, the pill's `CoinDisplay`/`GemDisplay` is disabled and
  the text counts up per landed coin. After the last coin it's re-enabled, and its `OnEnable` shows the saved value.
- **Win panel:** `Card/Reward` shows `+{coinsEarned}` from the same variable that is granted. The burst starts there.
  The HUD (`Game`) panel hides on LEVELCOMPLETE, so `Level Complete` has its own `CoinPill` instance at the HUD
  pill's position. The win grants coins only (no gems or power-up charges), so nothing else is shown.
- Shop and daily grants burst from the screen centre (`CurrencyGrantedEvent.ScreenCenter`). Daily-reward power-up
  charges have no counter to fly to and play no effect.

- **The win event is published in `WinPanelManager.LateUpdate`**, not `OnEnable`. The panel's own CoinPill is a child
  and subscribes after the parent's `OnEnable`, so it missed the event and no coins were visible.
- **Daily reward is saved on claim, not on show.** The old code wrote the date when the popup appeared, so the Lobby
  button re-opened a claimable panel every time (unlimited rewards), and closing the popup lost the day's reward.
  Now `UnclaimedStreakDay()` returns 0 once today is claimed, and the panel opens with Claim disabled.

## Tuning knobs
`CurrencyFlyEffect`: maxCoins, coinSize, burstRadius, burstDuration, flyDuration, stagger, sortingOrder.
