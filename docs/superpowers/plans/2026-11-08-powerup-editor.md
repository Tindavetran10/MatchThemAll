# Power-up Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Power-up Manager editor window that creates, edits, reorders and deletes power-ups (icon, tray model, VFX, sound, row position, unlock level, charges, price, mechanic), backed by a tray that is spawned from data at runtime.

**Architecture:** Each `PowerupDataSO` names a slot prefab. `PowerupManager` spawns those slots in `order` under `Powerup Zone`, and `PowerupTrayLayout` (unchanged) spaces them. The four built-in scene spots become slot prefabs. A generic `PowerupSlot` prefab serves custom power-ups, showing a tray model or the icon. The editor window only edits assets, never MainScene.

**Tech Stack:** Unity 6 (URP), C#, IMGUI editor windows (`EditorWindowStyles`), `PreviewRenderUtility`, `TypeCache`, PrimeTween (runtime only), Unity MCP `Unity_RunCommand` for verification.

**Spec:** `docs/superpowers/specs/2026-11-08-powerup-editor-design.md`

## Global Constraints

- Follow `CLAUDE.md` (project root) for everything below. Key points:
  - No renames of serialized fields (`uiPrefab` keeps its name).
  - Asset references go on prefabs and scene references on instances.
  - `RunCommand` scripts alias `using UiImage = UnityEngine.UI.Image;` and can't reference PrimeTween.
  - Use `PlayerSettings.runInBackground = false` after any test that sets `Application.runInBackground`.
  - Never use `??` / `?.` on `UnityEngine.Object`.
- Power-up ids are unchanged (`vacuum`, `fan`, `freeze`, `spring`), because save data stores charges by id.
- The tray must look identical after migration. Current left→right order: **freeze, fan, spring, vacuum**.
- Each `Powerup` references the `Lock Badge`, `Level Label`, `Amount Text` and `Video Icon` under **its own** slot.
- **No commits** unless the user asks. Each task ends with a checkpoint where the user may commit.
- **No automated test framework exists in this project.** Each "test" step is a `Unity_RunCommand` script, with exact code and expected output. Run Play-mode checks through the real path (`EventBus.Publish(new PowerupClickedEvent(so))`, `SaveManager`). Record and restore save data, then call `SaveManager.Flush()`.
- After each task: `Unity_ReadConsole` (Error/Warning) shows **0 entries**, and `graphify update .` has been run.

## Deviations from the spec (decided while planning, from reading the scene)

1. **No prefab variants.** Every spot's `Slot Background` and `Amount Container` (counter, video icon, lock badge, level label) are already nested prefabs, so they are already shared. Each built-in slot is saved as its own prefab, `Slot_<Name>.prefab`, from the existing scene spot.
2. **`FanOrigin` and `FreezeMuzzle` stay in the scene.** They are screen-centre VFX points directly under `Powerup Zone` (local z 3.5), not inside the slots. Moving them into slots would shift the VFX whenever the tray rescales. `PowerupManager` keeps those two scene references. Only `SuckPoint`, which is inside the vacuum's slot, is looked up from the slot.
3. **`PowerupContext` keeps its fields.** `VacuumSuckPosition` is filled from the tapped slot instead of from a `PowerupManager` field. Effect code is unchanged.

The spec's §1 already records these three points (updated with this plan).

## File Map

| File | Change | Responsibility |
|---|---|---|
| `Scripts/Runtime/PowerUps/PowerupDataSO.cs` | modify | New fields `trayModel`, `activateSound`, `activateVolume`, `activatePitch` + accessors |
| `Scripts/Runtime/Core/SoundManager.cs` | modify | `Sfx(AudioClip, float, float)` |
| `Scripts/Runtime/PowerUps/PowerupEffect.cs` | modify | `virtual bool WaitsForSlotAnimation` |
| `Scripts/Runtime/PowerUps/VacuumEffect.cs` | modify | override `WaitsForSlotAnimation => true` |
| `Scripts/Runtime/PowerUps/Powerup.cs` | modify | `modelAnchor`, `iconSprite`; `Configure` places the tray model or icon |
| `Scripts/Runtime/PowerUps/PowerupManager.cs` | modify | Spawn slots; data-driven sound; generic animation wait; drop `vacuum` / `vacuumSuckPosition` |
| `Scripts/Runtime/Core/SoundEvent.cs` | modify (Task 6) | Remove values 15–18 |
| `Scripts/Editor/SoundManagerWindow.cs` | modify (Task 6) | Remove the four power-up rows |
| `Scripts/Editor/PowerupManagerWindow.cs` | rewrite | The editor window |
| `Scripts/Editor/PowerupSlotPreview.cs` | create | 3D slot preview (`PreviewRenderUtility`) used by the window |
| `Scripts/Editor/PowerupMechanicTemplate.cs` | create | Writes `Custom/<Name>Effect.cs` |
| `Prefabs/PowerUps/Slots/Slot_{Freeze,Fan,Spring,Vacuum}.prefab` | create | Built-in tray slots |
| `Prefabs/PowerUps/Slots/PowerupSlot.prefab` | create | Generic slot for custom power-ups |
| `Resources/Powerups/Powerup_*.asset` | modify | `uiPrefab`, sound clip/volume/pitch, `order` |
| `Scenes/MainScene.unity` | modify (Task 4) | Remove the four spots; `PowerupManager` loses two scene fields |
| `docs/decisions/2026-11-08-data-driven-powerup-tray.md` | create (Task 7) | Decision record |
| `CLAUDE.md` | modify (Task 7) | Pitfalls |

All paths below are relative to `Assets/MatchThemAllTemplate/` unless they start with `docs/` or `CLAUDE.md`.

---

### Task 1: Per-power-up activation sound

**Files:**
- Modify: `Scripts/Runtime/PowerUps/PowerupDataSO.cs` (Runtime header, accessors)
- Modify: `Scripts/Runtime/Core/SoundManager.cs` (Playback region)
- Modify: `Scripts/Runtime/PowerUps/PowerupManager.cs` (the `SoundManager.Sfx(so.id switch …)` block)
- Modify (data): the four `Resources/Powerups/Powerup_*.asset`

**Interfaces:**
- Produces:
  - `GameObject PowerupDataSO.TrayModel`, `AudioClip PowerupDataSO.ActivateSound`, `float ActivateVolume`, `float ActivatePitch`
  - `static void SoundManager.Sfx(AudioClip clip, float volume, float pitch)`

- [ ] **Step 1: Record the current power-up sounds (baseline).** Run in `Unity_RunCommand`:

```csharp
using UnityEngine; using UnityEditor; using MatchThemAll.Scripts;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    var lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>("Assets/MatchThemAllTemplate/Resources/Sound/SoundLibrary.asset");
    foreach (var e in new[]{ SoundEvent.PowerupVacuum, SoundEvent.PowerupSpring, SoundEvent.PowerupFan, SoundEvent.PowerupFreeze })
      if (lib.TryGet(e, out var en)) r.Log("{0}: clips {1} first {2} vol {3} pitch {4}-{5}", e, en.clips.Length, en.clips.Length > 0 ? en.clips[0] : null, en.volume, en.pitchMin, en.pitchMax);
  }
}
```

Expected: four lines, each with one clip. Write the values down. Task 1 Step 5 and Task 6 depend on them.

- [ ] **Step 2: Add the fields to `PowerupDataSO`.** Replace the `[Header("Runtime")]` block and add accessors:

```csharp
        [Header("Runtime")]
        [Tooltip("Slot Prefab: the tray slot spawned for this power-up (PowerupSlot, or a built-in Slot_* prefab).")]
        [SerializeField] private GameObject uiPrefab;          // slot prefab (name kept for serialization)
        [Tooltip("Optional 3D model shown in a PowerupSlot. Empty = the icon is shown instead.")]
        [SerializeField] private GameObject trayModel;
        [SerializeReference] private PowerupEffect effect;     // polymorphic behavior

        [Header("Sound")]
        [Tooltip("Played when the power-up activates. Empty = silent.")]
        [SerializeField] private AudioClip activateSound;
        [Range(0f, 1f)] [SerializeField] private float activateVolume = 1f;
        [Range(0.5f, 2f)] [SerializeField] private float activatePitch = 1f;
```

```csharp
        public GameObject TrayModel => trayModel;
        public AudioClip ActivateSound => activateSound;
        public float ActivateVolume => activateVolume;
        public float ActivatePitch => activatePitch;
```

- [ ] **Step 3: Add `SoundManager.Sfx(AudioClip, float, float)`** in the `#region Playback`, directly under `Sfx(SoundEvent id)`:

```csharp
        /// <summary>Plays <paramref name="clip"/> on the SFX group (power-ups carry their own clip). Safe when no manager/clip exists.</summary>
        public static void Sfx(AudioClip clip, float volume, float pitch)
        {
            if (Instance && clip) Instance.PlayClip(clip, volume, pitch);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            int v = PickVoice();
            var source = _voices[v];
            _voiceStartedAt[v] = Time.unscaledTime;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = library ? library.sfxGroup : null;
            source.Play();
        }
```

- [ ] **Step 4: Use it in `PowerupManager.OnPowerupClickedEvent`.** Replace the whole `SoundManager.Sfx(so.id switch { … });` statement with:

```csharp
            SoundManager.Sfx(so.ActivateSound, so.ActivateVolume, so.ActivatePitch);
```

- [ ] **Step 5: Copy the library sounds onto the four assets.** After compiling (`AssetDatabase.Refresh()`, then check the console), run:

```csharp
using UnityEngine; using UnityEditor; using MatchThemAll.Scripts; using MatchThemAll.Scripts.Power_Ups;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    var lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>("Assets/MatchThemAllTemplate/Resources/Sound/SoundLibrary.asset");
    var map = new (string id, SoundEvent ev)[] { ("vacuum", SoundEvent.PowerupVacuum), ("spring", SoundEvent.PowerupSpring), ("fan", SoundEvent.PowerupFan), ("freeze", SoundEvent.PowerupFreeze) };
    foreach (var (id, ev) in map) {
      var so = AssetDatabase.LoadAssetAtPath<PowerupDataSO>($"Assets/MatchThemAllTemplate/Resources/Powerups/Powerup_{id}.asset");
      lib.TryGet(ev, out var en);
      var s = new SerializedObject(so);
      s.FindProperty("activateSound").objectReferenceValue = en.clips[0];
      s.FindProperty("activateVolume").floatValue = en.volume;
      s.FindProperty("activatePitch").floatValue = en.pitchMin; // pitchMin == pitchMax for all four (Step 1)
      s.ApplyModifiedPropertiesWithoutUndo();
      r.Log("{0}: {1} vol {2} pitch {3}", id, so.ActivateSound, so.ActivateVolume, so.ActivatePitch);
    }
    AssetDatabase.SaveAssets();
  }
}
```

Expected: the four lines match Step 1. If any entry had `pitchMin != pitchMax`, stop and ask the user. A single pitch can't express a range.

- [ ] **Step 6: Play-mode check.** Enter Play mode in MainScene. Then, in one `RunCommand`:
  - Record `SaveManager.GetPowerupCount("fan")`.
  - Publish `new PowerupClickedEvent(fanSo)`.
  - Read the `clip` of every `AudioSource` under the `SoundManager` instance that `isPlaying`.

  Expected: one playing source has the Fan clip, with pitch equal to `ActivatePitch`, and the fan count dropped by 1.

  Then call `SaveManager.AddPowerupCharge("fan", 1)` and `SaveManager.Flush()`, and exit Play mode.

- [ ] **Step 7: Console 0 errors/warnings; `graphify update .`; checkpoint** (offer the user a commit).

---

### Task 2: Animation wait driven by the effect, vacuum point from its slot

**Files:**
- Modify: `Scripts/Runtime/PowerUps/PowerupEffect.cs`
- Modify: `Scripts/Runtime/PowerUps/VacuumEffect.cs` (class body top)
- Modify: `Scripts/Runtime/PowerUps/PowerupManager.cs` (fields, `Awake`/`OnDestroy` subscriptions, `OnPowerupClickedEvent`, `OnVacuumStarted`, `BuildContext`, `FindSource`)

**Interfaces:**
- Consumes: Task 1's `SoundManager.Sfx(AudioClip,…)` line, which stays where it is.
- Produces:
  - `virtual bool PowerupEffect.WaitsForSlotAnimation`
  - `Powerup PowerupManager.FindPowerup(PowerupDataSO)` (private)
  - `static Transform PowerupManager.FindChild(Transform, string)` (private)

- [ ] **Step 1: Baseline the vacuum.** In Play mode, with the level in GAME state and goal items on the board:
  - Record `LevelManager.Instance.Items.Count` and the vacuum charge count.
  - Publish `PowerupClickedEvent(vacuumSo)`.
  - Wait 3 s using a coroutine on `VfxPool.Instance` that logs the count.

  Expected: the item count drops (goal items collected) and charges drop by 1. Restore charges, then `Flush()`.

- [ ] **Step 2: Add to `PowerupEffect`:**

```csharp
        /// <summary>True if activation waits for the slot's "Activate" animation to fire its Started event (vacuum suck).</summary>
        public virtual bool WaitsForSlotAnimation => false;
```

- [ ] **Step 3: Override in `VacuumEffect`** (first member in the class):

```csharp
        public override bool WaitsForSlotAnimation => true;
```

- [ ] **Step 4: Rewrite the vacuum path in `PowerupManager`.**
  - Delete the fields `[Header("Vacuum Elements")] vacuum`, `vacuumSuckPosition` and `_vacuumRequested`.
  - Flag in the checkpoint message: these were serialized; their scene values (`Vacuum Spot/Vacuum`, `…/SuckPoint`) are now found through the slot.
  - Add `private PowerupDataSO _awaitingAnimation;`.
  - Replace the block from `// Vacuum waits for its suck animation` to the end of `OnPowerupClickedEvent` with:

```csharp
            Powerup slot = FindPowerup(so);
            if (so.Effect.WaitsForSlotAnimation)
            {
                _isBusy = true;
                if (slot && slot.HasAnimator)
                {
                    _awaitingAnimation = so;   // OnSlotAnimationStarted runs the effect in sync with the animation
                    slot.Play();
                }
                else
                {
                    so.Effect.Activate(ctx);   // no Animator wired — run immediately
                }
                UpdateAllPowerupVisuals();
            }
            else
            {
                so.Effect.Activate(ctx);
            }
        }
```

  Replace `OnVacuumStarted` with:

```csharp
        private void OnSlotAnimationStarted()
        {
            if (_awaitingAnimation == null) return;
            var so = _awaitingAnimation;
            _awaitingAnimation = null;
            so.Effect.Activate(BuildContext(so));
        }
```

  Update the subscriptions in `Awake`/`OnDestroy`: `Powerup.Started += OnSlotAnimationStarted;` / `-=`.

  In `BuildContext`, set `Source = source` and `VacuumSuckPosition = FindChild(source, "SuckPoint")`, where `var source = FindSource(so);` is declared above the `return`. Replace `FindSource` with:

```csharp
        /// <summary>The tray slot's Powerup for this power-up (its transform is the prop effects animate).</summary>
        private Powerup FindPowerup(PowerupDataSO so)
        {
            if (_powerupUIElements == null) return null;
            foreach (var pu in _powerupUIElements)
                if (pu && pu.Data == so) return pu;
            return null;
        }

        private Transform FindSource(PowerupDataSO so)
        {
            var pu = FindPowerup(so);
            return pu ? pu.transform : null;
        }

        /// <summary>First descendant named <paramref name="name"/> (per activation, not per frame).</summary>
        private static Transform FindChild(Transform root, string name)
        {
            if (!root) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
```

  Remove `using ZLinq;` / `System.Linq` from `PowerupManager.cs` only if nothing else in the file uses them (grep `AsValueEnumerable` in the file).

- [ ] **Step 5: Re-run Step 1.** Expected: same result as the baseline (items collected, charge used, suck animation played), and no `id == "vacuum"` string left in `PowerupManager.cs`:

```bash
grep -n '"vacuum"' Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/PowerupManager.cs
```

Expected: no output.

- [ ] **Step 6: Console clean; `graphify update .`; checkpoint.**

---

### Task 3: Slot prefabs (built-ins + generic)

**Files:**
- Modify: `Scripts/Runtime/PowerUps/Powerup.cs`
- Create: `Prefabs/PowerUps/Slots/Slot_Freeze.prefab`, `Slot_Fan.prefab`, `Slot_Spring.prefab`, `Slot_Vacuum.prefab`, `PowerupSlot.prefab`
- Modify (data): `Powerup_*.asset` → `uiPrefab`

**Interfaces:**
- Produces:
  - `Powerup.Configure(PowerupDataSO so)` now also places the tray model or icon.
  - `[SerializeField] Transform modelAnchor`, `[SerializeField] SpriteRenderer iconSprite` on `Powerup`.
  - Prefab paths above.

- [ ] **Step 1: Extend `Powerup`.** Add under `[Header("UI References")]`:

```csharp
        [Header("Custom Slot (PowerupSlot only)")]
        [Tooltip("Where PowerupDataSO.trayModel is instantiated. Leave empty on built-in slots.")]
        [SerializeField] private Transform modelAnchor;
        [Tooltip("Shows PowerupDataSO.icon when the power-up has no tray model.")]
        [SerializeField] private SpriteRenderer iconSprite;
```

  Replace `Configure` with:

```csharp
        /// <summary>Assigns the data and builds the slot's look. Called by PowerupManager when it spawns the slot.</summary>
        public void Configure(PowerupDataSO so)
        {
            data = so;
            powerupId = so ? so.id : powerupId;
            if (!so) return;
            if (iconImage && so.Icon) iconImage.sprite = so.Icon;

            if (!modelAnchor) return; // built-in slot: its prop is part of the prefab
            bool hasModel = so.TrayModel;
            if (hasModel)
            {
                var model = Instantiate(so.TrayModel, modelAnchor, false);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false; // the slot's own collider takes taps
            }
            if (iconSprite)
            {
                iconSprite.gameObject.SetActive(!hasModel);
                iconSprite.sprite = so.Icon;
            }
        }
```

  Compile. Console must be clean.

- [ ] **Step 2: Snapshot the tray (baseline for Task 4).** In edit mode, write a JSON file `Temp/tray_snapshot.json` with these fields for every child of `Powerup Zone` that has a `Powerup` in its children:
  - `name`, `localPosition`, `localScale`, `siblingIndex`
  - The `Powerup`'s `powerupId`/`Data.id`
  - The world position and lossy scale of its `Slot Background`, `Amount Container` and prop

```csharp
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using MatchThemAll.Scripts.Power_Ups; using System.Text;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    Transform zone = null;
    foreach (var g in EditorSceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t.name == "Powerup Zone") zone = t;
    var sb = new StringBuilder();
    foreach (Transform spot in zone) {
      var pu = spot.GetComponentInChildren<Powerup>(true); if (!pu) continue;
      sb.AppendLine($"{pu.Data.id}|{spot.GetSiblingIndex()}|{spot.localPosition.ToString("F4")}|{spot.localScale.ToString("F4")}");
      foreach (Transform c in spot) sb.AppendLine($"  {c.name}|{c.position.ToString("F4")}|{c.lossyScale.ToString("F4")}");
    }
    System.IO.File.WriteAllText("Temp/tray_snapshot.txt", sb.ToString());
    r.Log(sb.ToString());
  }
}
```

  Expected order of ids: `freeze, fan, spring, vacuum`.

- [ ] **Step 3: Save the four built-in slots.**
  - Create the folder `Prefabs/PowerUps/Slots`.
  - For each spot (`Freeze Gun Spot` → `Slot_Freeze`, `Fan Spot` → `Slot_Fan`, `Spring Spot` → `Slot_Spring`, `Vacuum Spot` → `Slot_Vacuum`), call `PrefabUtility.SaveAsPrefabAsset(spot.gameObject, path)`. Do **not** connect; the scene spots are removed in Task 4.
  - On each saved prefab asset, set the `Powerup`'s `data` to null. Data is assigned at spawn. The saved `powerupId` stays as a fallback.
  - Then audit each prefab: open it with `PrefabUtility.LoadPrefabContents` and confirm that each of the `Powerup`'s `amountText`, `videoIcons`, `lockBadge`, `levelLabel` and `animator` (when set) is a descendant of the prefab root.

```csharp
// inside the loop, after saving:
var root = PrefabUtility.LoadPrefabContents(path);
var pu = root.GetComponentInChildren<Powerup>(true);
var so = new SerializedObject(pu);
so.FindProperty("data").objectReferenceValue = null;
so.ApplyModifiedPropertiesWithoutUndo();
foreach (var f in new[]{"amountText","videoIcons","lockBadge","levelLabel","animator"}) {
  var o = so.FindProperty(f).objectReferenceValue;
  var t = o is GameObject g ? g.transform : (o as Component) ? ((Component)o).transform : null;
  r.Log("{0}.{1} -> {2}", root.name, f, o == null ? "null" : (t && t.IsChildOf(root.transform) ? "OK " + t.name : "OUTSIDE SLOT!"));
}
PrefabUtility.SaveAsPrefabAsset(root, path);
PrefabUtility.UnloadPrefabContents(root);
```

  Expected: no `OUTSIDE SLOT!`. Only `animator` may be null, on non-vacuum slots.

- [ ] **Step 4: Build `PowerupSlot.prefab`.**
  - Duplicate the `Fan Spot` in memory: `Object.Instantiate`, then `PrefabUtility.UnpackPrefabInstance` is not needed because the spot itself isn't a prefab instance.
  - Delete its `FanCanister` child (`Object.DestroyImmediate`).
  - Add a child `Model` with `layer = LayerMask.NameToLayer("Power Ups")`, at the FanCanister's local position and rotation. Give it:
    - a `Powerup` component;
    - a `SphereCollider` with radius equal to the FanCanister's collider world-bounds extent ÷ the slot's lossy scale (log the value);
    - a child `Icon` with a `SpriteRenderer` (sorting order 2), local scale 0.5, facing the camera like `Slot Background` (copy its rotation).
  - Wire the `Model`'s `Powerup`: `amountText`, `videoIcons`, `lockBadge` and `levelLabel` point to this copy's `Amount Container` children, `modelAnchor` = `Model`, `iconSprite` = `Icon`. Leave `animator` null.
  - Save as `Prefabs/PowerUps/Slots/PowerupSlot.prefab`, destroy the temp copy, and run the Step 3 audit on it (expected all OK; `animator` null).

- [ ] **Step 5: Point the data at the slots.**
  - Set `uiPrefab` on `Powerup_freeze/fan/spring/vacuum.asset` to the matching `Slot_*` prefab.
  - Set `order`: freeze 0, fan 1, spring 2, vacuum 3. This matches the current left→right order. Log the values before and after.
  - Save the assets.

- [ ] **Step 6: Console clean; `graphify update .`; checkpoint.**

---

### Task 4: Spawn the tray from data; remove the scene spots

**Files:**
- Modify: `Scripts/Runtime/PowerUps/PowerupManager.cs` (`Awake`)
- Modify: `Scenes/MainScene.unity` (remove the four spots)

**Interfaces:**
- Consumes:
  - Task 3 prefabs and `Powerup.Configure`
  - `PowerupDatabaseSO.Ordered`
  - `PowerupTrayLayout` (unchanged; lays out children that have a `Powerup`)
- Produces: `[SerializeField] Transform trayRoot` on `PowerupManager` (scene reference to `Powerup Zone`).

- [ ] **Step 1: Spawn in `Awake`.** Add the field:

```csharp
        [Header("Tray")]
        [Tooltip("The Powerup Zone (has PowerupTrayLayout). Each power-up's slot prefab is spawned under it, in order.")]
        [SerializeField] private Transform trayRoot;
```

  Replace the block from `_powerupUIElements = FindObjectsByType<Powerup>(…)` through the `TryAutoResolve` loop with:

```csharp
            var spawned = new List<Powerup>();
            if (trayRoot && database)
            {
                foreach (var so in database.Ordered)
                {
                    if (!so.UIPrefab) { Debug.LogWarning($"[PowerupManager] '{so.id}' has no slot prefab — not shown in the tray."); continue; }
                    var slot = Instantiate(so.UIPrefab, trayRoot, false);
                    slot.name = $"{so.id} Slot";
                    var pu = slot.GetComponentInChildren<Powerup>(true);
                    if (!pu) { Debug.LogWarning($"[PowerupManager] Slot prefab of '{so.id}' has no Powerup component."); continue; }
                    pu.Configure(so);
                    spawned.Add(pu);
                }
            }
            _powerupUIElements = spawned.ToArray();
```

  Add `using System.Collections.Generic;` if missing.

  `TutorialManager` finds power-ups at runtime (`FindObjectsByType<Powerup>`), so spawned slots work with it. No script refers to the spots by name (checked while planning). Serialized references are still possible, so scan every component in MainScene for an object reference that points *into* the four spots, from *outside* them:

```csharp
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using System.Collections.Generic;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    var spots = new HashSet<Transform>(); Transform zone = null;
    foreach (var g in EditorSceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t.name == "Powerup Zone") zone = t;
    foreach (Transform s in zone) if (s.name.EndsWith(" Spot")) foreach (var t in s.GetComponentsInChildren<Transform>(true)) spots.Add(t);
    foreach (var g in EditorSceneManager.GetActiveScene().GetRootGameObjects())
      foreach (var c in g.GetComponentsInChildren<Component>(true)) {
        if (!c || spots.Contains(c.transform)) continue;
        var it = new SerializedObject(c).GetIterator();
        while (it.Next(true)) {
          if (it.propertyType != SerializedPropertyType.ObjectReference || !it.objectReferenceValue) continue;
          var o = it.objectReferenceValue; var t = o is GameObject go ? go.transform : o is Component cc ? cc.transform : null;
          if (t && spots.Contains(t)) r.Log("{0}.{1}.{2} -> {3}", c.gameObject.name, c.GetType().Name, it.propertyPath, t.name);
        }
      }
  }
}
```

  Expected after Task 2: no output (Task 2 removed `PowerupManager.vacuum`/`vacuumSuckPosition`). Any line is a reference that would break: stop and show it to the user.

- [ ] **Step 2: Wire `trayRoot` and remove the spots.**
  - In MainScene, set `PowerupManager.trayRoot` = `ENVIRONMENT/Main Camera/Powerup Zone` (scene reference on the instance).
  - Remove `Freeze Gun Spot`, `Fan Spot`, `Spring Spot` and `Vacuum Spot` with `Undo.DestroyObjectImmediate`.
  - Keep `Renderer`, `FanOrigin` and `FreezeMuzzle`.
  - Check that `PowerupManager.fanOrigin`/`freezeMuzzle` still point to `Powerup Zone/FanOrigin` and `/FreezeMuzzle`.
  - Save the scene.

- [ ] **Step 3: Compare against the snapshot.**
  - Enter Play mode and wait until `Time.time > 1`. If it's stuck at 0.02, set `Application.runInBackground = true`.
  - Produce the same text as Task 3 Step 2 from the spawned slots, sorted by sibling index, and diff it against `Temp/tray_snapshot.txt`.
  - Expected:
    - Same ids in the same order.
    - Each slot's `localPosition`/`localScale` within 0.001.
    - Each child's world position within 0.001.
  - Child *names* are the same. Slot names differ (`fan Slot` vs `Fan Spot`), and that's expected.

- [ ] **Step 4: Play-mode behaviour of all four** (same Play session; record `GetPowerupCount` for each id first):
  - **vacuum:** publish the click. After 3 s, goal items were removed and charges -1.
  - **fan:** publish the click. Within 1 s at least one item's `Rigidbody.linearVelocity.magnitude > 1`, and charges -1.
  - **freeze:** publish the click. `TimerManager.Instance.IsFrozen == true`, and charges -1.
  - **spring:** publish the click. Within 1 s the tray item count drops, or `Item.Spot == null` for an item that was in the tray (fill the tray first by publishing `ItemClickedEvent` on 2 items). Charges -1.
  - **0 charges:** use `SaveManager.UsePowerupCharge("fan")` until 0, publish the click, and expect the `PowerupOfferPanel` GameObject to be `activeInHierarchy` and charges still 0.
  - **lock:** set `Powerup_fan.unlockLevel = 49`, then re-enter Play mode. The fan slot's `Lock Badge` is active, `Level Label` reads `Lv 50`, its collider is disabled, and a click leaves charges unchanged. Restore `unlockLevel = 0`.
  - Restore all charges to the recorded values, call `SaveManager.Flush()`, set `PlayerSettings.runInBackground = false`, and exit Play mode.

- [ ] **Step 5: Console clean; `graphify update .`; checkpoint.** Tell the user the MainScene spots are gone and the tray is now spawned.

---

### Task 5: The Power-up Manager window

**Files:**
- Rewrite: `Scripts/Editor/PowerupManagerWindow.cs`
- Create: `Scripts/Editor/PowerupSlotPreview.cs`
- Create: `Scripts/Editor/PowerupMechanicTemplate.cs`

**Interfaces:**
- Consumes:
  - `PowerupDataSO` (all fields; serialized names `id`, `displayName`, `icon`, `order`, `unlockLevel`, `defaultAmount`, `buyCurrency`, `buyCost`, `uiPrefab`, `trayModel`, `effect`, `activateVfx`, `endVfx`, `activateSound`, `activateVolume`, `activatePitch`)
  - `PowerupDatabaseSO.powerups`
  - `EditorWindowStyles.CardButton(bool)`
  - `PowerupSlotPreview`
- Produces:
  - `PowerupSlotPreview` with `void Draw(Rect rect, PowerupDataSO so)`, `void ResetCamera()`, `void Dispose()`
  - `static string PowerupMechanicTemplate.Create(string name)` returns the asset path

- [ ] **Step 1: `PowerupSlotPreview.cs`.** Same camera, controls and lighting approach as `ItemManagerWindow.DrawDockPreview` (read it first: lines ~409–620). Code:

```csharp
#if UNITY_EDITOR
using MatchThemAll.Scripts.Power_Ups;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>Live 3D preview of a power-up's tray slot (prefab + tray model / icon), orbit/pan/zoom like the Item Manager.</summary>
    public class PowerupSlotPreview
    {
        private PreviewRenderUtility _pru;
        private GameObject _instance;
        private PowerupDataSO _shown;
        private int _shownHash;
        private float _yaw = 180f, _pitch = 15f, _dist = 3f;
        private Vector2 _pan;
        private Vector3 _focus;

        public void ResetCamera() { _yaw = 180f; _pitch = 15f; _dist = 3f; _pan = Vector2.zero; }

        public void Dispose()
        {
            if (_pru != null) { _pru.Cleanup(); _pru = null; }
            _instance = null; _shown = null;
        }

        public void Draw(Rect rect, PowerupDataSO so)
        {
            if (Event.current.type == EventType.Layout) return;
            if (_pru == null)
            {
                _pru = new PreviewRenderUtility();
                _pru.camera.fieldOfView = 30f;
                _pru.camera.clearFlags = CameraClearFlags.SolidColor;
                _pru.camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 1f);
                _pru.camera.nearClipPlane = 0.01f;
                _pru.camera.farClipPlane = 100f;
                _pru.lights[0].intensity = 1.3f;
                _pru.lights[0].transform.rotation = Quaternion.Euler(50f, 330f, 0f);
                _pru.lights[1].intensity = 0.6f;
            }
            Rebuild(so);
            HandleInput(rect);

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var cam = _pru.camera.transform;
            cam.position = _focus + rot * new Vector3(_pan.x, _pan.y, -_dist);
            cam.rotation = rot;

            _pru.BeginPreview(rect, GUIStyle.none);
            _pru.camera.Render();
            _pru.EndAndDrawPreview(rect);

            if (!so || !so.UIPrefab)
                GUI.Label(rect, "  No slot prefab assigned", EditorStyles.whiteLabel);
        }

        /// <summary>Re-instantiates the slot when the selection or any of its look-affecting fields change.</summary>
        private void Rebuild(PowerupDataSO so)
        {
            int hash = so ? (so.UIPrefab ? so.UIPrefab.GetInstanceID() : 0) * 31 + (so.TrayModel ? so.TrayModel.GetInstanceID() : 0) * 17 + (so.Icon ? so.Icon.GetInstanceID() : 0) : 0;
            if (so == _shown && hash == _shownHash && _instance) return;
            _shown = so; _shownHash = hash;
            if (_instance) Object.DestroyImmediate(_instance);
            if (!so || !so.UIPrefab) return;

            _instance = _pru.InstantiatePrefabInScene(so.UIPrefab);
            var pu = _instance.GetComponentInChildren<Powerup>(true);
            if (pu)
            {
                pu.Configure(so);
                pu.UpdateVisuals(so.defaultAmount, false);
            }

            var b = new Bounds(_instance.transform.position, Vector3.zero);
            foreach (var r in _instance.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            _focus = b.center;
            _dist = Mathf.Max(0.5f, b.extents.magnitude * 3f);
        }

        private void HandleInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseDrag && e.button == 0) { _yaw += e.delta.x * 0.5f; _pitch = Mathf.Clamp(_pitch + e.delta.y * 0.5f, -80f, 80f); e.Use(); }
            else if (e.type == EventType.MouseDrag && e.button == 2) { _pan += new Vector2(-e.delta.x, e.delta.y) * _dist * 0.002f; e.Use(); }
            else if (e.type == EventType.ScrollWheel) { _dist = Mathf.Max(0.2f, _dist * (1f + e.delta.y * 0.05f)); e.Use(); }
        }
    }
}
#endif
```

- [ ] **Step 2: `PowerupMechanicTemplate.cs`.**

```csharp
#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>Writes a ready-to-fill PowerupEffect subclass into Scripts/Runtime/PowerUps/Custom.</summary>
    public static class PowerupMechanicTemplate
    {
        private const string Folder = "Assets/MatchThemAllTemplate/Scripts/Runtime/PowerUps/Custom";

        /// <summary>Returns the new script's path, or null if the name is invalid or the file exists.</summary>
        public static string Create(string name)
        {
            string cls = Regex.Replace(name ?? "", "[^A-Za-z0-9]", "");
            if (cls.Length == 0 || char.IsDigit(cls[0])) return null;
            if (!cls.EndsWith("Effect")) cls += "Effect";
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{cls}.cs";
            if (File.Exists(path)) return null;

            File.WriteAllText(path, $@"using System;
using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{{
    /// <summary>Custom power-up mechanic. Pick it in Match Them All ▸ Power-up Manager ▸ Mechanic.</summary>
    [Serializable]
    public class {cls} : PowerupEffect
    {{
        [Tooltip(""Example setting — shown in the Power-up Manager under Mechanic."")]
        public int amount = 10;

        // Return false to refuse the tap (no charge is used). Example: only when the timer is running.
        public override bool CanActivate(PowerupContext ctx) => ctx.Timer != null;

        public override void Activate(PowerupContext ctx)
        {{
            // What ctx gives you:
            //   ctx.Items      — every item on the board (List<Item>)
            //   ctx.Goals      — the level's goals (ItemLevelData[])
            //   ctx.ItemSpots  — the tray (ItemSpotManager)
            //   ctx.Timer      — the level timer (TimerManager), e.g. ctx.Timer.AddTime(amount)
            //   ctx.Source     — the tapped slot's transform (animate it, spawn VFX at it)
            //   ctx.ActivateVfx / ctx.EndVfx — the VFX set on this power-up
            //   ctx.SetBusy(true/false) — block other power-ups while a long effect runs
            Debug.Log(""[{cls}] activated"");
        }}
    }}
}}
");
            AssetDatabase.ImportAsset(path);
            return path;
        }
    }
}
#endif
```

- [ ] **Step 3: Rewrite `PowerupManagerWindow.cs`.** Follow `SoundManagerWindow` for structure and look. Read its lines 1–110 (palette, `EnsureStyles`), 205–291 (`OnGUI`, divider, toolbar) and 493–553 (`Section`/`EndSection`, audio preview via `AudioUtil`). Copy those pieces with these differences:
  - Constants: `DatabasePath = "Assets/MatchThemAllTemplate/Resources/Powerups/PowerupDatabase.asset"`, `AssetFolder = "Assets/MatchThemAllTemplate/Resources/Powerups"`, `SlotPrefabPath = "Assets/MatchThemAllTemplate/Prefabs/PowerUps/Slots/PowerupSlot.prefab"`.
  - Menu: `[MenuItem("Match Them All/Power-up Manager")]`, title `"Power-up Manager"`, `minSize = new Vector2(820, 560)`.
  - State:
    - `PowerupDatabaseSO _db`
    - `PowerupDataSO _selected`
    - `SerializedObject _so` (rebuilt when the selection changes)
    - `PowerupSlotPreview _preview = new()`
    - scrolls, `_listWidth`, `_draggingDivider`, `_isDirty`
  - `OnDisable`: `_preview.Dispose()`, stop the audio preview, destroy owned textures (as in Sound Manager).
  - **Toolbar** (38 px):
    - "⚡ Power-up Manager" in `AccentBlue`.
    - FlexibleSpace.
    - Warning count (`⚠ N problem(s)`, orange) from `Problems(so)` summed over the database.
    - "● Unsaved Changes" + "💾 Save" (green) when `_isDirty`.
    - Buttons: `+ New Power-up`, `Duplicate` (disabled without a selection), `Delete` (red tint; disabled without a selection), `New Mechanic…`.
  - **List** (left): for each `p` in `_db.Ordered`, one row:
    - a card button, height 44, rich text: `$"<b>{Name(p)}</b>\n<size=10><color=#9999A6>{(p.unlockLevel == 0 ? "from start" : $"level {p.unlockLevel + 1}")}</color></size>"`, prefixed with `"⚠ "` when `Problems(p) != null`;
    - the icon drawn in a 36×36 rect at the row's left via `GUI.DrawTexture(r, AssetPreview.GetAssetPreview(p.Icon) ?? p.Icon.texture)` when the icon exists (`??` here is on `Texture2D` from an API that returns C# null, which is OK, but write it as an explicit `if` for consistency with CLAUDE.md);
    - two 22-wide buttons `▲`/`▼` calling `Move(p, -1/+1)`.
  - Footer: `"{_db.powerups.Count} power-ups · order = position in the tray row (left → right)"`.
  - **Detail** (right), each block wrapped in `Section("…")` / `EndSection()`:
    1. **Identity:**
       - `displayName`.
       - `id` via `EditorGUILayout.DelayedTextField`. On change, normalise with `Regex.Replace(v.ToLowerInvariant(), "[^a-z0-9_]", "")` and refuse duplicates (`EditorUtility.DisplayDialog`). If the old id was non-empty, warn: "Players lose saved charges of '<old>'."
       - `icon` (`PropertyField`).
    2. **Tray:**
       - `uiPrefab` labelled "Slot Prefab".
       - `trayModel` labelled "Tray Model", with help text under it: "Empty = the icon is shown. Built-in slots (Slot_*) ignore this — their prop is part of the prefab."
       - "Reset Camera" button.
       - The preview: `Rect pr = GUILayoutUtility.GetRect(10, 260, GUILayout.ExpandWidth(true)); _preview.Draw(pr, _selected);`
    3. **Progression:**
       - "Unlocks at level" int field showing `unlockLevel + 1`, storing `Mathf.Max(1, v) - 1`.
       - `defaultAmount` labelled "Starting charges".
    4. **Economy:** `buyCurrency`, `buyCost` labelled "Price (when out of charges)".
    5. **Mechanic:**
       - Popup over `TypeCache.GetTypesDerivedFrom<PowerupEffect>()` filtered `!t.IsAbstract`, plus "(none)" at index 0.
       - Current index from `_selected.Effect?.GetType()`. This is a plain C# object, not a UnityEngine.Object, so `?.` is fine here.
       - On change: if an effect exists, confirm "Switch mechanic? Its settings reset."; then set `_so.FindProperty("effect").managedReferenceValue = type == null ? null : Activator.CreateInstance(type)`.
       - Then draw the effect's children: `var ep = _so.FindProperty("effect"); if (ep.managedReferenceValue != null) { var end = ep.GetEndProperty(); var it = ep.Copy(); for (bool enter = true; it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end); enter = false) if (it.depth == ep.depth + 1) EditorGUILayout.PropertyField(it, true); }`.
       - Help text: "New Mechanic… (toolbar) creates a script you can fill in; it appears here after Unity compiles."
    6. **Effects & Sound:**
       - `activateVfx`, `endVfx`.
       - `activateSound` with a ▶ button (`PlayClip(_selected.ActivateSound, false)`) and a ■ button (`StopPreview()`).
       - `activateVolume` slider 0–1, `activatePitch` slider 0.5–2.
  - After drawing: `if (_so.ApplyModifiedProperties()) { EditorUtility.SetDirty(_selected); _isDirty = true; }`.
  - **Helpers** (exact):

```csharp
        private static string Name(PowerupDataSO p) => string.IsNullOrEmpty(p.DisplayName) ? (string.IsNullOrEmpty(p.id) ? p.name : p.id) : p.DisplayName;

        /// <summary>First problem with this power-up, or null. Shown as ⚠ in the list and counted in the toolbar.</summary>
        private string Problems(PowerupDataSO p)
        {
            if (string.IsNullOrEmpty(p.id)) return "No id";
            int same = 0;
            foreach (var q in _db.powerups) if (q && q.id == p.id) same++;
            if (same > 1) return $"Id '{p.id}' is used {same} times";
            if (p.Effect == null) return "No mechanic";
            if (!p.Icon) return "No icon";
            if (!p.UIPrefab) return "No slot prefab";
            return null;
        }

        /// <summary>Swaps this power-up with its neighbour in tray order, then renumbers 0..n-1.</summary>
        private void Move(PowerupDataSO p, int dir)
        {
            var list = new List<PowerupDataSO>(_db.Ordered);
            int i = list.IndexOf(p), j = i + dir;
            if (j < 0 || j >= list.Count) return;
            (list[i], list[j]) = (list[j], list[i]);
            for (int k = 0; k < list.Count; k++)
            {
                Undo.RecordObject(list[k], "Reorder power-ups");
                list[k].order = k;
                EditorUtility.SetDirty(list[k]);
            }
            _isDirty = true;
        }

        private void CreatePowerup(PowerupDataSO copyFrom)
        {
            string baseId = copyFrom ? copyFrom.id + "_copy" : "new_powerup";
            string id = baseId;
            for (int n = 2; _db.powerups.Exists(q => q && q.id == id); n++) id = baseId + n;

            var so = copyFrom ? Instantiate(copyFrom) : CreateInstance<PowerupDataSO>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{AssetFolder}/Powerup_{id}.asset");
            AssetDatabase.CreateAsset(so, path);
            var s = new SerializedObject(so);
            s.FindProperty("id").stringValue = id;
            s.FindProperty("order").intValue = _db.powerups.Count;
            if (!copyFrom)
            {
                s.FindProperty("displayName").stringValue = "New Power-up";
                s.FindProperty("defaultAmount").intValue = 3;
                s.FindProperty("unlockLevel").intValue = 0;
                s.FindProperty("uiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            }
            s.ApplyModifiedPropertiesWithoutUndo();

            Undo.RecordObject(_db, "Add power-up");
            _db.powerups.Add(so);
            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            Select(so);
        }

        private void DeletePowerup(PowerupDataSO p)
        {
            if (!EditorUtility.DisplayDialog("Delete power-up", $"Delete '{Name(p)}'? The asset goes to the trash. Players' saved charges for '{p.id}' are left unused.", "Delete", "Cancel")) return;
            Undo.RecordObject(_db, "Delete power-up");
            _db.powerups.Remove(p);
            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(p));
            Select(null);
        }

        private void Select(PowerupDataSO p)
        {
            _selected = p;
            _so = p ? new SerializedObject(p) : null;
            GUI.FocusControl(null);
        }
```

  - **New Mechanic…**: a small `EditorWindow`-less prompt. Use `EditorInputDialog` only if it exists in this Unity version (check with `Unity_RunCommand`: `typeof(EditorWindow).Assembly.GetType("UnityEditor.EditorInputDialog")`). Otherwise show an inline text field in the toolbar row (`_newMechanicName`) with a "Create" button. On create: `var path = PowerupMechanicTemplate.Create(_newMechanicName); if (path == null) DisplayDialog("Invalid or existing name"); else EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(path));`.
  - On `OnEnable`: load `_db`, then `Select(first in _db.Ordered)`. If `_db` is null, draw the Sound Manager's "no library" style empty state with a button that calls the `Tools/Powerups/Create Default Database` menu item (`EditorApplication.ExecuteMenuItem`).

- [ ] **Step 4: Compile and check the window by numbers** (`RunCommand`, edit mode):
  - Open the window (`EditorWindow.GetWindow(type)`), then `Repaint()` and read the console. Expected: 0 errors (IMGUI layout errors show up as errors).
  - **Mechanic list:** `TypeCache.GetTypesDerivedFrom<PowerupEffect>()` non-abstract count == 4. Log the names.
  - **Reorder:**
    - Call `Move(springSo, -1)` via reflection, then check the orders are freeze 0, spring 1, fan 2, vacuum 3.
    - Call `Move(springSo, +1)` and check they're back to freeze 0, fan 1, spring 2, vacuum 3.

- [ ] **Step 5: Screenshot for the user.** Ask the user to open `Match Them All ▸ Power-up Manager` and confirm the look matches the Shop and Sound Managers. This is a visual check only they can make. Wait for their answer before Task 6.

- [ ] **Step 6: Console clean; player-scripts compile:**

```csharp
using UnityEditor; using UnityEditor.Build.Player;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    var res = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings { target = BuildTarget.Android, group = BuildTargetGroup.Android }, "Temp/PlayerScriptCheck");
    r.Log("assemblies {0}", res.assemblies.Count);
  }
}
```

  Expected: no compile errors in the console, and assemblies > 0. Then `graphify update .`; checkpoint.

---

### Task 6: Remove the four power-up `SoundEvent`s

**Files:**
- Modify: `Scripts/Runtime/Core/SoundEvent.cs` (lines with `PowerupVacuum = 15` … `PowerupFreeze = 18`)
- Modify: `Scripts/Editor/SoundManagerWindow.cs` (`Describe` lines 187–190, default table lines 593–596)
- Modify (data): `Resources/Sound/SoundLibrary.asset` (entries with id 15–18)

- [ ] **Step 1: Grep for every use:**

```bash
grep -rn "PowerupVacuum\|PowerupSpring\|PowerupFan\|PowerupFreeze" Assets/MatchThemAllTemplate/Scripts
```

  Expected: only `SoundEvent.cs` and `SoundManagerWindow.cs`. The Task 1 switch was removed. Anything else: stop and report.

- [ ] **Step 2: Remove library entries 15–18 first** (while the enum still compiles):

```csharp
using UnityEditor; using MatchThemAll.Scripts;
internal class CommandScript : IRunCommand {
  public void Execute(ExecutionResult r) {
    var lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>("Assets/MatchThemAllTemplate/Resources/Sound/SoundLibrary.asset");
    int removed = lib.entries.RemoveAll(e => (int)e.id >= 15 && (int)e.id <= 18);
    EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets();
    r.Log("removed {0}, remaining {1}", removed, lib.entries.Count);
  }
}
```

  Expected: `removed 4`.

- [ ] **Step 3: Delete the four enum lines and the eight `SoundManagerWindow` lines.** Recompile.

- [ ] **Step 4: Check.**
  - Open the Sound Manager window. Expected: no power-up rows, no "missing clip" warning caused by this change.
  - Play mode: tap the fan. Expected: its sound still plays (Task 1 path).
  - Restore the charge and `Flush()`.

- [ ] **Step 5: Console clean; `graphify update .`; checkpoint.**

---

### Task 7: End-to-end custom power-up, docs, cleanup

**Files:**
- Create (temporary): `Scripts/Runtime/PowerUps/Custom/TestTimeEffect.cs`, `Resources/Powerups/Powerup_test_time.asset`
- Create: `docs/decisions/2026-11-08-data-driven-powerup-tray.md`
- Modify: `CLAUDE.md` (Known Pitfalls)

- [ ] **Step 1: Create through the window's own code paths:**
  - Call `PowerupMechanicTemplate.Create("TestTime")` and wait for compilation.
  - Edit the generated `Activate` to `ctx.Timer.AddTime(amount);` (keep `amount = 10`).
  - Call `CreatePowerup(null)` (reflection on the open window).
  - Set `id = "test_time"`, `displayName = "Test Time"`, `icon` = the fan's icon, effect = `TestTimeEffect`, and move it to position 1 with `Move`.
  - Expected: the list shows it second and the toolbar shows 0 problems.

- [ ] **Step 2: Play mode:**
  - The spawned tray has 5 slots in the order freeze, test_time, fan, spring, vacuum. Read the sibling order of children with a `Powerup`.
  - `PowerupTrayLayout` shrank them to fit (log `localScale`).
  - The test slot shows the icon `SpriteRenderer` active.
  - Publishing its click raises the timer by 10 s (read `TimerManager`'s remaining time before and after) and uses one charge (it starts with 3).
  - Restore save data (remove the `test_time` charge entry if `SaveManager` exposes that; otherwise note it in the report) and `Flush()`. Exit Play mode.

- [ ] **Step 3: Clean up the test:**
  - Delete the power-up via `DeletePowerup` (confirm the dialog manually or call the inner steps).
  - Delete `TestTimeEffect.cs` + `.meta` and the `Custom` folder if empty, with `git rm`-free deletion: `File.Delete` + `AssetDatabase.Refresh()`, since they were never committed.
  - Check `git status` shows no leftovers from the test.

- [ ] **Step 4: Decision record** `docs/decisions/2026-11-08-data-driven-powerup-tray.md`:

```markdown
# Data-driven power-up tray (2026-11-08)

**Decision:** The power-up tray is spawned at runtime from `PowerupDatabase` (each `PowerupDataSO.uiPrefab`, sorted by `order`) under `Powerup Zone`. MainScene no longer contains power-up spots.

**Why:** Template buyers create power-ups in `Match Them All ▸ Power-up Manager` without editing scenes; row order and unlocks are pure data.

**Consequences:**
- Built-in slots are `Prefabs/PowerUps/Slots/Slot_*.prefab`; custom ones use `PowerupSlot.prefab` (tray model or icon).
- Scene-level VFX points `FanOrigin` / `FreezeMuzzle` stay under `Powerup Zone` and on `PowerupManager`.
- Activation sounds live on each `PowerupDataSO`, not in the Sound Library.
- Mechanics are `PowerupEffect` subclasses; `New Mechanic…` writes one into `Scripts/Runtime/PowerUps/Custom`.
```

- [ ] **Step 5: CLAUDE.md.** Replace the existing "Power-up unlocks" pitfall with:

```markdown
- **Power-ups are data-driven.** The tray is spawned from `PowerupDatabase` at runtime (`PowerupManager.trayRoot` = `Powerup Zone`); MainScene has no power-up spots. Add/edit/reorder in `Match Them All ▸ Power-up Manager`. `unlockLevel` is a 0-based index (players see index + 1; the window shows player numbers). A slot prefab's `Powerup` must reference the badge/label/counter under its *own* slot (Fan once pointed at Vacuum's badge). Activation sounds are on the `PowerupDataSO` (clip/volume/pitch). Effects that sync to a slot animation override `WaitsForSlotAnimation`. Never key behaviour on `so.id` strings.
```

- [ ] **Step 6: Final checks:**
  - Console 0 errors/warnings.
  - Player-scripts compile (Task 5 Step 6 script).
  - `PlayerSettings.runInBackground == false`.
  - `git status` lists only the files in the File Map, plus `graphify-out/`.
  - `graphify update .`

  Report to the user what changed and what wasn't verified, then offer a commit.

---

## Self-review notes

- **Spec coverage:**

  | Spec item | Task |
  |---|---|
  | Data fields + sound | 1 |
  | Animation wait not keyed on id | 2 |
  | Slot prefabs (adjusted: no variants) | 3 |
  | Spawning + scene removal + snapshot diff | 4 |
  | Window: toolbar, list, six cards, preview, New/Duplicate/Delete/New Mechanic | 5 |
  | Removal of the four `SoundEvent`s | 6 |
  | End-to-end test, decision note, CLAUDE.md, player compile | 7 |
  | Lock and 0-charge offer checks | 4, Step 4 |

- **Checked while planning:**
  - Charges are restored with `SaveManager.AddPowerupCharge(string id, int amount)` (`SaveManager.cs:122`).
  - `TutorialManager` finds power-ups with `FindObjectsByType<Powerup>` at runtime.
  - No script names the scene spots.
  - Task 4 Step 1's reference scan covers serialized references.
- **Stops for the user:**
  - Task 1 Step 5 if a built-in sound uses a pitch *range*.
  - Task 4 Step 1 if the scan finds a reference into a spot.
  - Task 5 Step 5 for the visual check of the window.
