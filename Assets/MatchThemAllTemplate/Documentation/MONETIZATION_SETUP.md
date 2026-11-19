# Monetization setup (ads + in-app purchases)

The template runs out of the box with **mock ads** (a 1-second fake video) and the **coin/gem shop**.
Real ads and real-money purchases switch on by themselves when you install their Unity packages.
No code changes are needed.

| What | Package (Package Manager ▸ Unity Registry) | Without it |
|---|---|---|
| Ads (rewarded + interstitial) | **Ads Mediation** (`com.unity.services.levelplay`, 9.5 or newer) | Mock ads |
| Real-money purchases | **In-App Purchasing** (`com.unity.purchasing`, 5.0 or newer) | Real-money products show "…" and can't be bought |

All keys and switches live in one asset: `Resources/Monetization/MonetizationSettings.asset`.

---

## 1. Ads (LevelPlay)

1. Install **Ads Mediation**. Do not install "Ads IAP Mediation Adaptor" or "Advertisement Legacy".
   If Unity asks to import **Mobile Dependency Resolver**, click **Import**.
2. Switch the build target to **Android** or **iOS** (*File ▸ Build Profiles ▸ Switch Platform*).
   LevelPlay only serves ads on those targets, mock ads in the Editor included.
3. Resolve native dependencies for each platform you ship (Android Resolver / iOS Resolver).
   Builds fail with Gradle or CocoaPods errors if you skip this.
4. In the [LevelPlay dashboard](https://platform.ironsrc.com/), add your app and create a **Rewarded** and an
   **Interstitial** ad unit. Copy the **App Key** and both **Ad Unit IDs** into `MonetizationSettings`
   (separate fields for Android and iOS).
5. If you use AdMob through LevelPlay, enter its app keys in *Ads Mediation ▸ Developer Settings ▸ LevelPlay Mediation Settings*.
6. Android 13+: add `<uses-permission android:name="com.google.android.gms.permission.AD_ID"/>` to your
   AndroidManifest.

Where ads appear:

| Placement | Type | Notes |
|---|---|---|
| Continue (out of time) | Rewarded | Reward only after the video is watched to the end |
| Power-up with 0 charges ▸ "Watch ad" | Rewarded | Same |
| `WatchAdForCoinsButton` (optional component) | Rewarded | Capped per session |
| Next level / Retry | Interstitial | Paced, see below |

Interstitial pacing (in `MonetizationSettings`): none before **First Interstitial Level**, then at most one
every **Levels Between Interstitials** transitions and never more often than **Min Seconds Between Interstitials**.
The defaults are level 4, every 3 transitions, and 6 minutes.

**Remove Ads** (the `remove_ads` shop product) turns interstitials off. Rewarded placements still give their
reward, just without the video.

Testing: in the Editor with an Android/iOS target you get LevelPlay mock ads. On a device, tick
**Launch Test Suite** for a Development Build to check every ad network. Mock ads in the Editor never fail,
so test failure handling with `Mock Ads Fail` (no SDK) or in airplane mode on a device.

## 2. Privacy

Configure these before release. This is technical setup, not legal advice.

- **GDPR**: with **Ask Gdpr Consent** on, the first launch shows `Resources/Monetization/ConsentPopup`
  (restyle it freely). The answer is saved and sent to LevelPlay before it starts.
- **CCPA**: tick **Ccpa Do Not Sell** to report an opt-out of data sale.
- **COPPA**: tick **Child Directed** if the game targets children under 13.
- **iOS App Tracking Transparency**: the prompt runs automatically before ads start
  (`Plugins/iOS/ATTRequester.mm`). Its text is **Ios Tracking Usage Description**, which a post-build step
  writes into Info.plist. Also add LevelPlay's SKAdNetwork IDs to Info.plist and check the iOS privacy
  manifest before submitting.

## 3. In-app purchases (Unity IAP 5)

1. Install **In-App Purchasing**.
2. For each real-money product, set **Iap Product Id** on its `ShopProductSO` (e.g. `gems_2x_starter`).
   Products with **Is One Time** become *non-consumable* (restorable, e.g. a remove-ads pack). All others are
   *consumable*.
3. Create the same ids in **Google Play Console** and **App Store Connect**.
4. Prices on the shop cards come from the store, in the player's currency. The `priceAmount` field is
   ignored for real-money products.
5. **Restore Purchases** is required by Apple. The `ShopPanel` prefab has the button. It only shows on iOS,
   because Google Play restores automatically and Unity IAP doesn't support restore in the Editor.
6. Optional, Google Play: *Services ▸ In-App Purchasing ▸ Receipt Validation Obfuscator* for local receipt
   checks. For Apple, StoreKit 2 validates on the device, and server-side checks use the order's JWS.

How a purchase is granted: the store reports a pending order. The shop grants the reward, saves, and only
then confirms with the store. If the game closes before the save, the store delivers the purchase again
on the next launch. Each transaction id is recorded, so nothing is granted twice.

In the Editor, Unity IAP uses a fake store, so you can buy and restore without a device. Use sandbox / test
accounts on devices.

## 4. Swapping networks

`IAdService` and `IIapService` (in `Scripts/Runtime/Monetization`) are the only contracts. To use another
ad network, implement `IAdService` and set `MonetizationServices.AdServiceFactory` before the first scene
loads, as `LevelPlayAdService` does.
