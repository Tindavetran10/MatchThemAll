# Set Up Project

Right after you import the template, run **Match Them All ▸ Set Up Project**. A package can't change your project's settings, so this one click does it for you. Skip it and you'll see pink or missing text, an error about a missing layer when a level starts, and no levels at all.

Run it once after importing, and again after you re-import a newer version of the template. It only fills in what's missing, so running it twice is safe.

> Keep the template in `Assets/MatchThemAllTemplate`. The tool looks for the levels and scenes there.

## What it does

When it finishes, it shows a summary with one line per step. **✔** means it changed something, **•** means it was already in place, and **✖** means it couldn't do that step (the line says why). The same text is in the Console under `[Set Up Project]`.

| Step | What it sets up | Why the template needs it |
|---|---|---|
| **TextMesh Pro essentials** | Imports TextMesh Pro's shaders and default font. | The game's text materials use them. Without them the text is pink or invisible. |
| **Render pipeline** | Assigns `Mobile_RPAsset` in *Project Settings ▸ Graphics* and in each quality level that has none. | The template is made for URP. It never replaces a pipeline you already set, such as the PC one from the Universal 3D template. |
| **Input** | Sets **Active Input Handling** to **Both**. | The game uses the Input System package. |
| **Layers** | Creates the layers `Match Stuff` (6), `Power Ups` (7) and `Tutorial` (8). | Scenes and prefabs store layers by number, and the tutorial looks `Tutorial` up by name. |
| **Levels** | Creates the Addressables settings and registers every level with the label `LevelData`. | Levels load through Addressables. With no entries, no levels load. |
| **Build scenes** | Adds OpenScene, Lobby, LevelSelect, LoadingScene and MainScene to the build, with OpenScene first. | The game switches scenes by name, and builds need them listed. |

If it changes the input setting, it also tells you to **restart Unity**. Do that before you press Play.

## When a line shows ✖

| Line says | What to do |
|---|---|
| TextMesh Pro: package not found | Install **Unity UI** in the Package Manager, then run it again. |
| Render pipeline: not found or not readable | Install **Universal RP** in the Package Manager, make sure the import finished, then run it again. |
| Layers: layer 7 is "…", the template needs "Power Ups" there | One of your own layers is on a number the template uses. Open *Project Settings ▸ Tags and Layers*, move or rename it, and run the tool again. The tool never overwrites your layers. |
| Levels: Addressables is not installed | Install **Addressables** in the Package Manager, then run it again. |
| Levels: no level files found | The template isn't in `Assets/MatchThemAllTemplate`. Move it back. |
| Build scenes: not found | The same, or a scene file is missing. Re-import the template. |

Each step can also be done by hand. The steps are in [Requirements](Requirements.md#after-you-import-the-template).

## Things to know

- **Universal 3D projects.** The template's `Mobile_RPAsset` has the same ID as Unity's own, so Unity merges them and the file ends up in `Assets/Settings`. The tool finds it there. Your project keeps its PC pipeline in the Editor, and Android builds use the Mobile one.
- **It doesn't touch your own settings.** It fills in blanks. Your existing layers, pipelines and scene list stay as they are, except that the template's five scenes go first in the build list.

---
[↑ Getting Started](README.md) · Next: [First Run →](First-Run.md)
