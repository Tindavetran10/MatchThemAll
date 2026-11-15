# Library, Trash and Health

These parts of the Template Editor deal with the items themselves, not just one level.

![Trash Bin and Item Reference Health](../../Images/tools/te-trash-health.png)

### Prefab Library

| Action | What happens |
|---|---|
| Click an item | Adds it to the selected level ([Items in a Level](Items-in-a-Level.md)). |
| Type in the **Search** box | Shows only items whose name contains the text. |
| Small red **✕** on an item (1) | Moves it to the Trash Bin, after asking. It's also removed from every level that uses it, and the window tells you how many. |
| Right-click an item | *Move to Trash*, or **Delete Permanently…** (asks first, and you can't undo it). |

To make new items, use the [Item Manager](../Item-Manager/README.md).

### Trash Bin (2)

Removed items wait here, greyed out (the picture shows it empty). Click the green **⟲** on an item to restore it.
It goes back into the library and, where possible, back into the levels it was removed from.

### Item Reference Health (3)

If an item file gets deleted outside the Template Editor, levels can end up pointing at nothing.

1. Click **Check item references** (4).
2. If you see **✓ No broken item references** (5), you're fine. Otherwise you get a list such as
   *LevelData03 → slot #2: missing item*.
3. To clean them up, click **Remove all broken entries**, which appears under that list.

---
[↑ Template Editor](README.md) · Next: [Global Settings →](Global-Settings-Tab.md)
