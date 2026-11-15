# Build the Level Data

Levels are packed separately from the rest of the game, using Unity's *Addressables*, and the normal build doesn't
do that for you. If you skip this step, the build has no levels and the map is empty.

Repeat it every time you add, change or delete a level, or change a tutorial:

1. Open *Window ▸ Asset Management ▸ Addressables ▸ Groups*.
2. In that window, click *Build ▸ New Build ▸ Default Build Script*.
3. Wait for it to finish, then make your build ([Make the Build](Make-the-Build.md)).

> In the Editor you can skip this, as long as the levels are registered with Addressables (step 4 of
> [After you import the template](../01-Getting-Started/Requirements.md)). Without that, no levels load at all.

Levels made with the Template Editor's **+ New** are registered for this automatically (label `LevelData`).

---
[↑ Build for Android and iOS](README.md) · Next: [Make the Build →](Make-the-Build.md)
