# Make the Build

1. **Pick the platform.** *File ▸ Build Profiles*, select **Android** or **iOS**, and click *Switch Platform*.
2. **Check the scenes.** All five scenes must be in the list, with **OpenScene first**: OpenScene, Lobby,
   LevelSelect, LoadingScene, MainScene.
3. **Your app's details.** *Edit ▸ Project Settings ▸ Player*: company name, product name, package / bundle id,
   version and icons.
4. **Level data.** Make sure you've done [Build the Level Data](Build-the-Level-Data.md) since your last level change.
5. **Ads installed?** Run the Android Resolver or iOS Resolver menu that comes with LevelPlay. Then follow
   `Documentation/MONETIZATION_SETUP.md` for the Android advertising-id permission and the iOS settings.
6. **Build**, install it on a phone, and play a few levels, the shop and the daily reward before you publish.

> Use **Unity 6000.0** for builds. A project once opened in a newer Unity version can fail to build in 6000.0.

---
[↑ Build for Android and iOS](README.md) · Next: [Troubleshooting →](../07-Troubleshooting/README.md)
