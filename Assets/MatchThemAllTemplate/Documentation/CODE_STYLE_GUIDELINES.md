# Code Style & Contribution Guidelines

This document outlines the coding standards, conventions, and patterns used throughout the **Match Them All** project.

---

## 1. C# Naming Conventions

- **PascalCase**: Classes, Structs, Enums, Interfaces, Public Properties, Methods, Serialized Events.
- **camelCase**: Local variables, method arguments, private non-serialized fields (prefixed with `_`).
- **_camelCase**: Private serialized/inspector fields or cached private references.
- **k_PascalCase** or **UPPER_SNAKE_CASE**: Constants and read-only static fields.

```csharp
public class GoalManager : MonoBehaviour
{
    private const float k_DefaultAnimDuration = 0.5f;

    [SerializeField] private List<GoalCard> _goalCards;
    
    private int _remainingGoalCount;

    public int RemainingGoalCount => _remainingGoalCount;

    public void TrackProgress(EItemName itemName, int amount)
    {
        // Method body
    }
}
```

---

## 2. Event Handling & Subscriptions
- Always unsubscribe from `EventBus<T>` in `OnDestroy()` or `OnDisable()` to prevent memory leaks and dangling delegates.
- Ensure event struct payloads are lightweight and pass read-only data.

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
