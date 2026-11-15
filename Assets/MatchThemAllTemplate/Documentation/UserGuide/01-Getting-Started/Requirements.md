# Requirements

Install these before you import the template.

| What | Version | How to get it |
|---|---|---|
| Unity | 6000.0 LTS (Unity 6) | Unity Hub ▸ Installs ▸ Install Editor |
| [PrimeTween](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960) | 1.x | Free on the Asset Store. Steps below. |
| [ZLinq](https://github.com/Cysharp/ZLinq) | latest | Two installs: NuGetForUnity, then ZLinq itself. Steps below. |
| Universal Render Pipeline, Input System, Addressables, Unity UI | | *Window ▸ Package Manager ▸ Unity Registry*. Install any your project doesn't have yet. TextMesh Pro comes with Unity UI. |
| NaughtyAttributes | | Already in `ThirdParty/NaughtyAttributes`. Nothing to do. |
| Ads Mediation (LevelPlay), In-App Purchasing | optional | Only needed for real ads and real-money products. See [Ads and In-App Purchases](../05-Ads-and-In-App-Purchases/README.md). |

### Install PrimeTween

1. Open the [PrimeTween page on the Asset Store](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960) and click **Add to My Assets**. Sign in with the same Unity account you use in the Unity Hub.
2. In Unity, open *Window ▸ Package Manager*, switch the list to **My Assets**, and select **PrimeTween**.
3. Click **Download**, then **Import**.

### Install ZLinq

ZLinq comes in two parts, and the template needs both. If you only install the second, you get a wall of compile errors from inside the ZLinq package.

1. Open *Window ▸ Package Manager*, click **+** at the top left, choose **Add package from git URL**, paste this and click **Add**. This installs NuGetForUnity:

   `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`

2. A **NuGet** menu now appears in Unity's menu bar. Open *NuGet ▸ Manage NuGet Packages*, search for **ZLinq**, and click **Install**.
3. Back in the Package Manager, add a second git URL the same way:

   `https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity`

Unity fetches git packages with Git, so [Git](https://git-scm.com/downloads) must be installed on your computer. If you get an error about Git, that's why. ZLinq's own page is [github.com/Cysharp/ZLinq](https://github.com/Cysharp/ZLinq).

### After you import the template

Import the template last (*Assets ▸ Import Package ▸ Custom Package*, or from My Assets). A package can't change your project settings, so without a little setup you'll see pink or missing text, no render pipeline, and no levels.

Choose **Match Them All ▸ Set Up Project**. It does the five things below and shows a summary: ✔ for something it changed, • for something already in place, ✖ for something it couldn't do (the line says why). It only fills in what's missing, so running it again is safe. If it says **Restart Unity**, restart.

If you'd rather do it by hand, or a line shows ✖, here are the steps:

1. **TextMesh Pro.** Choose *Window ▸ TextMeshPro ▸ Import TMP Essential Resources* and click **Import**. Without it the font materials have no shader.
2. **Render pipeline.** Open *Edit ▸ Project Settings ▸ Graphics* and set **Default Render Pipeline Asset** to `MatchThemAllTemplate/Settings/Mobile_RPAsset`. Then open **Quality** and set the same asset as **Render Pipeline Asset** on the quality level you use.
3. **Input.** Open *Edit ▸ Project Settings ▸ Player ▸ Other Settings* and set **Active Input Handling** to **Both**. Unity restarts.
4. **Levels.** The levels are loaded through Addressables, so register them once:
   1. Open *Window ▸ Asset Management ▸ Addressables ▸ Groups* and click **Create Addressables Settings**.
   2. In the Project window, open `MatchThemAllTemplate/_START_HERE/Levels`, select all the `LevelData` files, and drag them into the **Default Local Group**.
   3. With them still selected in the group, set the **label** of each to `LevelData`. In the Groups window, right-click the entries ▸ *Manage Labels*, add `LevelData`, and tick it for every level.

   Levels you make later with the Template Editor's **+ New** are registered for you.
5. **Scenes.** Open *File ▸ Build Profiles ▸ Scene List* and add the five scenes from `MatchThemAllTemplate/Scenes`, with **OpenScene** first: OpenScene, Lobby, LevelSelect, LoadingScene, MainScene.

> Stick with Unity 6000.0. A newer Unity version upgrades some project files when you open it, and if you then go
> back to 6000.0 the build can fail.

---
[↑ Getting Started](README.md) · Next: [First Run →](First-Run.md)
