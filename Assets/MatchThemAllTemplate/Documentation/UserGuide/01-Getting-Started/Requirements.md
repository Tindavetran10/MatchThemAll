# Requirements

Install these before you open the template.

| What | Version | How to get it |
|---|---|---|
| Unity | 6000.0 LTS (Unity 6) | Unity Hub ▸ Installs ▸ Install Editor |
| [PrimeTween](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960) | 1.x | Free on the Asset Store. Import it before the template. Steps below. |
| [ZLinq](https://github.com/Cysharp/ZLinq) | latest | Installed from a git URL in the Package Manager. Steps below. |
| Universal Render Pipeline, Input System, Addressables | | Come with the project. Nothing to do. |
| NaughtyAttributes | | Already in `ThirdParty/NaughtyAttributes`. Nothing to do. |
| Ads Mediation (LevelPlay), In-App Purchasing | optional | Only needed for real ads and real-money products. See [Ads and In-App Purchases](../05-Ads-and-In-App-Purchases/README.md). |

### Install PrimeTween

1. Open the [PrimeTween page on the Asset Store](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960) and click **Add to My Assets**. Sign in with the same Unity account you use in the Unity Hub.
2. In Unity, open *Window ▸ Package Manager*, switch the list to **My Assets**, and select **PrimeTween**.
3. Click **Download**, then **Import**.

### Install ZLinq

1. Open *Window ▸ Package Manager*, click **+** at the top left, and choose **Add package from git URL**.
2. Paste this and click **Add**:

   `https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity`

Unity fetches git packages with Git, so [Git](https://git-scm.com/downloads) must be installed on your computer. If you get an error about Git, that's why. ZLinq's own page is [github.com/Cysharp/ZLinq](https://github.com/Cysharp/ZLinq).

> Stick with Unity 6000.0. A newer Unity version upgrades some project files when you open it, and if you then go
> back to 6000.0 the build can fail.

---
[↑ Getting Started](README.md) · Next: [First Run →](First-Run.md)
