# Make the Build

1. **Pick the platform.** Go to *File ▸ Build Profiles*, select **Android** or **iOS**, and click *Switch Platform*.
2. **Check the scenes.** All five must be in the list, with OpenScene first: OpenScene, Lobby, LevelSelect,
   LoadingScene, MainScene.
3. **Fill in your app's details.** In *Edit ▸ Project Settings ▸ Player*, set the company name, product name,
   package / bundle id, version and icons.
4. **Check the level data.** Make sure you've done [Build the Level Data](Build-the-Level-Data.md) since your last
   level change.
5. **Using ads?** Run the Android Resolver or iOS Resolver menu that comes with LevelPlay. Then follow
   `Documentation/MONETIZATION_SETUP.md` for the Android advertising-id permission and the iOS settings.
6. **Build**, install it on a phone, and play a few levels, the shop and the daily reward before you publish.

> Build with Unity 6000.0. A project that has been opened in a newer Unity version can fail to build in 6000.0.

---
[↑ Build for Android and iOS](README.md) · Next: [Troubleshooting →](../07-Troubleshooting/README.md)
