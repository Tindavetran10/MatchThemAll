# Build the Level Data

Levels are packed separately from the rest of the game (with Unity's *Addressables*). **The normal build doesn't
do this for you.** A build without this step has **no levels**, and the map is empty.

**Do this every time you add, change or delete a level, or change a tutorial:**

1. Open *Window ▸ Asset Management ▸ Addressables ▸ Groups*.
2. In that window, click *Build ▸ New Build ▸ Default Build Script*.
3. Wait until it finishes. Then make your build ([Make the Build](Make-the-Build.md)).

> In the Editor you don't need this step: Play mode reads the levels directly.

Levels made with the Template Editor's **+ New** are registered for this automatically (label `LevelData`).

---
[↑ Build for Android and iOS](README.md) · Next: [Make the Build →](Make-the-Build.md)
