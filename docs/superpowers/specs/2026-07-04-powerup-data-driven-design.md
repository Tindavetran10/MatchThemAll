# Power Up Data-Driven Overhaul — Design

- **Date:** 2026-07-04
- **Status:** Approved (brainstorming) — pending user spec review
- **Owner:** Tindavetran10
- **Scope:** `Assets/Match Them All/Power Ups/`, `Scripts/Save System/`, `Scripts/Settings/`, `Scripts/Core/`, `UI/`, scenes, editor tooling

## Background & problem

Power-ups today are hardcoded around the `EPowerupType` enum (`Vacuum=0, Spring=1, Fan=2, FreezeGun=3`):

- **Identity:** `EPowerupType` is the key used everywhere (save data, switch statements, UI).
- **Behavior split:** `Vacuum`/`Spring`/`Fan`/`FreezeGun` are near-identical `Powerup : MonoBehaviour` subclasses (an Animator + `Play()` + a `Started` event — UI animation triggers). The **actual gameplay logic lives in `PowerupManager`'s `switch(type)`** (`VacuumPowerup`/`SpringPowerup`/`FanPowerup`/`FreezePowerup`).
- **Save data:** `PlayerData` has hardcoded per-type fields (`vacuumCount`, `springCount`, `fanCount`, `freezeCount`) + `hasInitializedPowerups`. `SaveManager` switches on the enum for get/use/add.
- **Economy:** buying is hardcoded — `const int powerupCost = 50` coins, inline in `PowerupManager`. No unlock-by-level, no per-powerup cost, no currency concept.
- **Tuning:** Spring/Fan tuning (`springHorizontalForceRange`, `fanMagnitude`, etc.) lives globally in `GameSettingsSO`, not per-powerup.
- **UI/order:** power-up buttons are scene-placed; order is whatever `FindObjectsByType<Powerup>` returns (uncontrolled).

**Problem:** adding or tuning a power-up requires code edits in multiple places (enum, switch, save fields, manager). Nothing is designer-tunable.

## Goals

1. **One `PowerupDataSO` per power-up** as the single source of tunable data: identity, display, economy, unlock, ordering, and behavior binding.
2. **Fully data-driven runtime** — no `switch(type)`; behavior is polymorphic via the SO. Adding a new power-up = new `PowerupEffect` subclass + new SO asset (no enum, no manager edit).
3. **Designer-tunable fields:** default amount, unlock level, buy currency + cost, display order, icon/name, per-effect tuning.
4. **Forward-compatible economy:** an `ECurrency` enum (Coins now) that a future UI shop can build on.
5. **Save-data migration:** existing players keep their charges; the per-type fields become an id-keyed map.

## Non-goals

- The UI shop itself (multi-currency spending UI, storefront) — later work; this design only makes the data model ready for it.
- New power-up types beyond the existing 4 (the architecture supports them, but we migrate the 4, not add a 5th).
- Analytics / A-B / remote config.
- Changing the rewarded-ad (`videoIcons`) charge-grant flow.

## Decisions (from brainstorming)

| Decision | Choice |
|---|---|
| Scope | **Full overhaul** in one spec |
| Extensibility | **Fully data-driven** — SO binds behavior; no switch |
| Behavior binding | **A — `[SerializeReference]` polymorphic plain class** |
| Currency model | **`ECurrency` enum** (Coins now) + `int buyCost`, not a flat coin value |
| Locked button UX | **Non-interactable** (dimmed + lock badge + "Lv N") |
| Tuning location | **Per-effect** (Spring/Fan tuning moves into the effect fields; leaves `GameSettingsSO`) |
| `EPowerupType` | **Removed** — the SO `id` is the key |

## Architecture

### New: `PowerupDataSO` (ScriptableObject, one per power-up)

```csharp
[CreateAssetMenu(menuName = "Match Them All/Powerup Data")]
public class PowerupDataSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // unique key, e.g. "vacuum" — replaces EPowerupType
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Progression")]
    [Min(0)] public int order;            // left→right display order
    [Min(0)] public int unlockLevel;      // player level (currentLevelIndex) required
    [Min(0)] public int defaultAmount;    // first-launch charge grant

    [Header("Economy")]
    public ECurrency buyCurrency;         // Coins now; Gems/etc. later
    [Min(0)] public int buyCost;          // spent when activating with 0 charges

    [Header("Runtime")]
    [SerializeField] private GameObject uiPrefab;          // the button prefab
    [SerializeReference] private PowerupEffect effect;     // polymorphic behavior

    // public read-only accessors for UI/manager
}
```

### New: `ECurrency` enum

```csharp
public enum ECurrency { Coins /*, Gems, ... future */ }
```

A future shop extends this; `SaveManager` gains a generic `Spend(ECurrency, int)` that dispatches (Coins → existing coin balance; others added later).

### New: `PowerupDatabaseSO` (ScriptableObject — the registry)

```csharp
[CreateAssetMenu(menuName = "Match Them All/Powerup Database")]
public class PowerupDatabaseSO : ScriptableObject
{
    public List<PowerupDataSO> powerups;   // the single source of truth
    public PowerupDataSO FindById(string id) => ...;
    public IEnumerable<PowerupDataSO> Ordered => powerups.OrderBy(p => p.order);
}
```

### New: `PowerupEffect` (abstract, `[Serializable]`) + subclasses

```csharp
[Serializable]
public abstract class PowerupEffect
{
    public abstract bool CanActivate(PowerupContext ctx);
    public abstract void Activate(PowerupContext ctx);
}
```

Subclasses (plain classes, `[SerializeReference]`-friendly) absorb the logic from `PowerupManager`'s switch:

- **`VacuumEffect`** — collect up to 3 unassigned goal items, vortex them to the suck position, release to pool. Owns any vacuum-specific tuning.
- **`SpringEffect`** — pick a random occupied spot, release the item, apply physics throw. **Owns** `springHorizontalForceRange`, `springVerticalForceRange`, `springSpinSpeedRange`, `springThrowZDirection` (moved from `GameSettingsSO`).
- **`FanEffect`** — apply a random force to all active items. **Owns** `fanMagnitude` (moved from `GameSettingsSO`).
- **`FreezeEffect`** — freeze the timer (`TimerManager.FreezeTimer`).

A small **custom `PropertyDrawer`** for `[SerializeReference] PowerupEffect` provides the subtype dropdown (Vacuum/Spring/Fan/Freeze) so designers pick the effect and fill its fields in the SO inspector.

### New: `PowerupContext` (plain struct — the deps handed to an effect)

```csharp
public struct PowerupContext
{
    public List<Item> Items;              // LevelManager.Instance.Items
    public ItemLevelData[] Goals;         // GoalManager.Instance.Goals
    public ItemSpotManager ItemSpots;     // ItemSpotManager.Instance
    public TimerManager Timer;            // TimerManager.Instance
    public GameSettingsSO GameSettings;   // global, non-powerup tuning only
    public Transform VacuumSuckPosition;
    public Action<Item> OnItemPickup;
    public Action<Item> OnItemBackToGame;
    public Action<bool> SetBusy;          // replaces _isBusy mutations
}
```

Effects reach dependencies **only** through the context (no global-singleton reach-inside effects), so they are testable in isolation and there is no hidden coupling.

### Refactor: `PowerupManager`

- Holds a `[SerializeField] private PowerupDatabaseSO database;`.
- **No `switch`.** On `PowerupClickedEvent`: find the SO by id → if locked (`playerLevel < unlockLevel`) ignore (UI already blocks it) → `effect.CanActivate(ctx)` → use a charge or buy → `effect.Activate(ctx)`.
- Buy: if `0` charges, `SaveManager.Spend(so.buyCurrency, so.buyCost)`; on success continue, on failure show "not enough" and abort.
- Builds the `PowerupContext` once per activation and passes it to the effect.
- `UpdateAllPowerupVisuals` iterates `database.Ordered`.
- The vacuum animation handshake (`Vacuum.Play()` → `Vacuum.Started` → `OnVacuumStarted` → `VacuumPowerup`) is simplified: the generic `PowerupUI` plays the click animation; `VacuumEffect.Activate` runs the collection logic (its existing `Tween.Delay(2.5f)` busy-gating is preserved so the feel is identical).

### Refactor: `PowerupUI` (replaces the 4 subclasses)

One generic `MonoBehaviour` on the button prefab, driven by its `PowerupDataSO`:
- Holds the Animator + `amountText` + `videoIcons` + a **lock badge/"Lv N" label**.
- `Configure(PowerupDataSO so)`: sets icon/name, shows lock state.
- `UpdateVisuals(int amount, bool locked)`: dim + lock badge when locked; amount text when `amount > 0`; ad icons when `amount <= 0`.
- Non-interactable when locked.
- On click: raises `PowerupClickedEvent` carrying its `PowerupDataSO`.

The four `Vacuum`/`Spring`/`Fan`/`FreezeGun` MonoBehaviours are **deleted** (they were identical UI triggers; logic now lives in effects, UI is generic).

### `EPowerupType` removal

The enum is deleted. The SO `id` (string) is the key. Sites to update (the implementation plan maps every one):
- `PowerupClickedEvent` (carries `PowerupDataSO` instead of the `Powerup` MonoBehaviour).
- `PowerupManager` (switch, `CanUsePowerup`, buy cost).
- `SaveManager` (`GetPowerupCount`/`UsePowerupCharge`/`AddPowerupCharge`/`InitializePowerups`).
- `PlayerData` (per-type fields → map).
- `Powerup.type` field (gone; identity is the SO).
- Any other `EPowerupType` references surfaced by grep (e.g. `GameEvents`, tutorial, daily reward).

## Save-data migration

`PlayerData` replaces the four count fields with:

```csharp
[Serializable] public class PowerupSaveEntry { public string id; public int count; }
public List<PowerupSaveEntry> powerups = new();
public bool hasInitializedPowerups = false;
```

**Migration on load:** if `powerups` is empty AND any legacy field (`vacuumCount`/`springCount`/`fanCount`/`freezeCount`) is non-default, build entries with the known ids (`"vacuum"`, `"spring"`, `"fan"`, `"freeze"`) from those fields, then clear the legacy fields. `SaveManager` becomes id-keyed (`GetCount(id)`, `UseCharge(id)`, `AddCharge(id, int)`), reading/writing the map — no switch.

`InitializePowerups(database)`: on first launch, seed each SO's `defaultAmount` into the map by id.

## UI & ordering

- A container (layout group) spawns one `uiPrefab` per `database.Ordered` entry. Order is designer-controlled via each SO's `order`.
- Each `uiPrefab` carries the generic `PowerupUI`; `Configure(so)` wires identity/icon/lock.
- Locked (`currentLevelIndex < unlockLevel`): dimmed, lock badge + "Lv N", **non-interactable**.

## Editor tooling

- A designer-facing inspector for `PowerupDataSO`: NaughtyAttributes `[ShowIf]`/`[EnableIf]` to surface currency-relevant fields; the `[SerializeReference]` drawer for the effect subtype.
- A small database manager (create/list/validate `PowerupDataSO` entries; warn on duplicate ids or missing effects). Can be standalone or folded into the existing editor window — decided during planning.

## Data flow

1. Game boots → `PowerupManager` loads `PowerupDatabaseSO`, spawns `PowerupUI` per ordered entry via `Configure(so)`.
2. `SaveManager.InitializePowerups(database)` seeds first-launch charges by id.
3. `UpdateAllPowerupVisuals` reads counts + lock state per SO, pushes to each `PowerupUI`.
4. Click an unlocked power-up → `PowerupClickedEvent(so)` → `PowerupManager` resolves SO → unlock/CanActivate checks → charge-or-buy → `effect.Activate(ctx)` → visuals refresh via `OnPowerupsChanged`.

## Error handling & safety

- **Duplicate ids:** the database manager validates uniqueness; `FindById` returns the first match and logs a warning on duplicates.
- **Missing effect:** if a SO's `effect` is null, `Activate` logs and no-ops (no crash).
- **Save migration:** idempotent — once migrated, legacy fields are cleared so it doesn't re-run.
- **Unknown id in save** (e.g. a removed power-up): ignored gracefully (count kept but never surfaced if no SO matches).
- **Buy with insufficient funds:** aborts cleanly with a "not enough" message; no charge consumed.

## Testing (manual checklist — project has no test assembly)

1. Create 4 `PowerupDataSO` assets (vacuum/spring/fan/freeze) + a `PowerupDatabaseSO`; assign effects, costs, unlock levels, order.
2. Fresh save: each power-up starts with its `defaultAmount`; buttons appear left→right by `order`.
3. Activate each: behavior matches today (vacuum collects, spring throws, fan pushes, freeze freezes). Spring/Fan tuning reads from the effect fields.
4. Run out of charges → click → spends `buyCost` coins; with insufficient coins → "not enough", no spend.
5. Set one power-up's `unlockLevel` above the player's level → it shows dimmed + lock badge + "Lv N", non-interactable.
6. **Migration:** load an old save (with `vacuumCount` etc.) → charges carry over to the new map; old fields cleared.
7. Rewarded-ad (`videoIcons`) still grants charges when `amount <= 0`.
8. `EPowerupType` no longer exists anywhere (grep clean).

## Open / future

- **UI shop:** multi-currency storefront built on `ECurrency` + `Spend(ECurrency, int)` — separate spec.
- **5th+ power-up types:** now possible with zero core edits (new `PowerupEffect` subclass + SO).
- **Remote config / tuning hot-reload:** not in scope.
