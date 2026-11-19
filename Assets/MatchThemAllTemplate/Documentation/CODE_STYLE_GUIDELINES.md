# Code Style & Contribution Guidelines

This document outlines the coding standards, conventions, and patterns used throughout the **Match Them All** project.

---

## 1. C# Naming Conventions

- **PascalCase**: classes, structs, enums, interfaces, public properties, methods, constants and
  `static readonly` fields.
- **camelCase**: local variables, method arguments, and `[SerializeField]` fields (no prefix, so the
  Inspector label reads cleanly).
- **_camelCase**: private fields that are not serialized (runtime state, cached references).
- **E** prefix for enums used as ids (`EItemName`, `EGameState`).

```csharp
public class TimerManager : MonoBehaviour
{
    private const float WarningSeconds = 10f;              // constant: PascalCase

    [SerializeField] private float freezeDuration = 10f;   // serialized: camelCase

    private float _freezeLeft;                             // runtime state: _camelCase

    public bool IsFrozen { get; private set; }

    public void AddTime(int seconds) { /* … */ }
}
```

Never rename a serialized field without `[FormerlySerializedAs]`: the rename breaks Inspector values and
prefab overrides.

---

## 2. Event Handling & Subscriptions
- Systems talk through `EventBus.Subscribe<T>` / `EventBus.Publish(...)` with `struct` events from
  `GameEvents.cs`. Don't add direct references between unrelated managers.
- Always unsubscribe (`EventBus.Unsubscribe<T>`) in `OnDestroy()` or `OnDisable()` to avoid dangling delegates.
- Keep event payloads small: pass the data, not the manager.

---

## 3. Zero-Allocation Philosophy
- Avoid `LINQ` (`System.Linq`) in `Update()`, `FixedUpdate()`, or frequent gameplay loops.
- Use `ZLinq` (`AsValueEnumerable()`) or direct indexed `for` loops.
- Avoid allocating closures or lambda captures in per-frame methods.
- Use cached `WaitForSeconds` and object pools where appropriate.

---

## 4. Unity Inspector & Attributes
- Use `NaughtyAttributes` (e.g. `[BoxGroup]`, `[ShowIf]`, `[Button]`, `[ReadOnly]`) to keep Inspector interfaces clean and readable for designers.
- Always provide tooltips for non-obvious serialized fields:
  ```csharp
  [Tooltip("Hard speed cap in m/s to prevent physics tunneling.")]
  [SerializeField] private float maxSpeed = 12f;
  ```
