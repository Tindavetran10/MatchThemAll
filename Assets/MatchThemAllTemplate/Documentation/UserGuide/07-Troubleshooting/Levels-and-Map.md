# Levels and the Map

| Problem | Fix |
|---|---|
| The map is empty in a build, or the Console shows an `InvalidKeyException` | The level data wasn't built. Do [Build the Level Data](../06-Build-for-Android-and-iOS/Build-the-Level-Data.md), then build again. |
| A level I made doesn't appear | Make levels with the Template Editor's **+ New**, which registers them. A level file copied by hand isn't registered. |
| Levels are in the wrong order | Levels are sorted by file name. Rename them (`LevelData01`, `LevelData02`…). |
| **Amount** shows a warning | Amounts must be multiples of 3: 3, 6, 9… |
| The summary says **⚠ Needs at least 1 goal** | Set at least one item's **Goal?** to **GOAL** ([Items in a Level](../03-Editor-Tools/Template-Editor/Items-in-a-Level.md)). |
| A level lists a missing item | Use **Check item references** ▸ **Remove all broken entries** ([Library, Trash and Health](../03-Editor-Tools/Template-Editor/Library-Trash-and-Health.md)). |

---
[↑ Troubleshooting](README.md) · Next: [Items and Visuals →](Items-and-Visuals.md)
