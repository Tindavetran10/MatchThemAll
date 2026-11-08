using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Settings;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Data-driven power-up controller. Resolves clicks to a <see cref="PowerupDataSO"/>,
    /// checks unlock + CanActivate, spends a charge (or buys one), then runs the SO's
    /// polymorphic <see cref="PowerupEffect"/>. No switch on powerup type.
    /// </summary>
    public class PowerupManager : MonoBehaviour
    {
        [Header("Database")]
        [Tooltip("The registry of all power-ups. If unassigned, loaded from Resources/Powerups/PowerupDatabase.")]
        [SerializeField] private PowerupDatabaseSO database;

        [Header("Tray")]
        [Tooltip("The Powerup Zone (has PowerupTrayLayout). Each power-up's slot prefab is spawned under it, in order.")]
        [SerializeField] private Transform trayRoot;

        [Header("Fan Elements")]
        [SerializeField] private Transform fanOrigin;          // where the fan VFX originates

        [Header("Freeze Elements")]
        [SerializeField] private Transform freezeMuzzle;       // the freeze gun muzzle

        [Header("Global Settings")]
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("State")]
        private bool _isBusy;
        private PowerupDataSO _awaitingAnimation; // effect waiting for its slot's Started animation event

        public event Action<Item> OnItemPickup;
        public event Action<Item> OnItemBackToGame;

        private Powerup[] _powerupUIElements;

        public static PowerupManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (!database)
                database = Resources.Load<PowerupDatabaseSO>("Powerups/PowerupDatabase");

            if (!gameSettings)
                gameSettings = Resources.Load<GameSettingsSO>("GameSettings");

            // Seed first-launch powerup counts from the database (defaultAmount per SO).
            SaveManager.InitializePowerups(database);

            // Spawn one tray slot per power-up, in display order; PowerupTrayLayout spaces them.
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

            UpdateAllPowerupVisuals();

            Powerup.Started += OnSlotAnimationStarted;     // animation handshake (e.g. vacuum suck)
            EventBus.Subscribe<PowerupClickedEvent>(OnPowerupClickedEvent);
            SaveManager.OnPowerupsChanged += UpdateAllPowerupVisuals;
            // Level ids load async: until then GetCurrentLevelIndex() is 0 and every lock shows. Redraw once the level is in.
            LevelManager.LevelSpawned += OnLevelSpawned;
        }

        private void OnDestroy()
        {
            Powerup.Started -= OnSlotAnimationStarted;
            EventBus.Unsubscribe<PowerupClickedEvent>(OnPowerupClickedEvent);
            SaveManager.OnPowerupsChanged -= UpdateAllPowerupVisuals;
            LevelManager.LevelSpawned -= OnLevelSpawned;
        }

        private void OnLevelSpawned(Level level) => UpdateAllPowerupVisuals();

        private void OnPowerupClickedEvent(PowerupClickedEvent evt)
        {
            PowerupDataSO so = evt.Powerup;
            if (so == null || so.Effect == null) return;
            if (_isBusy) return;
            if (GameManager.Instance.State != EGameState.GAME) return;
            // The state is GAME before the level finishes spawning (async); effects need its items.
            if (!LevelManager.Instance || !LevelManager.Instance.IsLevelReady) return;

            int playerLevel = SaveManager.GetCurrentLevelIndex();
            if (so.IsLockedAt(playerLevel)) return;

            var ctx = BuildContext(so);
            if (!so.Effect.CanActivate(ctx)) return;

            // Use a charge; if none, let the player choose coins or an ad (PowerupOfferPanel) — never spend silently.
            if (!SaveManager.UsePowerupCharge(so.id))
            {
                EventBus.Publish(new PowerupOfferRequestedEvent(so));
                return;
            }

            SoundManager.Sfx(so.ActivateSound, so.ActivateVolume, so.ActivatePitch);

            // Effects like the vacuum wait for their slot's animation; the others run immediately.
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

        private void OnSlotAnimationStarted()
        {
            if (_awaitingAnimation == null) return;
            var so = _awaitingAnimation;
            _awaitingAnimation = null;
            so.Effect.Activate(BuildContext(so));
        }

        /// <summary>Builds the per-activation dependency context handed to an effect.</summary>
        private PowerupContext BuildContext(PowerupDataSO so)
        {
            var source = FindSource(so);
            return new PowerupContext
            {
                Items = LevelManager.Instance != null ? LevelManager.Instance.Items : null,
                Goals = GoalManager.Instance != null ? GoalManager.Instance.Goals : null,
                ItemSpots = ItemSpotManager.Instance,
                Timer = TimerManager.Instance,
                GameSettings = gameSettings,
                VacuumSuckPosition = FindChild(source, "SuckPoint"),
                ActivateVfx = so.ActivateVfx,
                EndVfx = so.EndVfx,
                FanOrigin = fanOrigin,
                FreezeMuzzle = freezeMuzzle,
                Source = source,
                OnItemPickup = InvokeItemPickup,
                OnItemBackToGame = InvokeItemBackToGame,
                SetBusy = busy => _isBusy = busy,
            };
        }

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

        private void InvokeItemPickup(Item item) => OnItemPickup?.Invoke(item);
        private void InvokeItemBackToGame(Item item) => OnItemBackToGame?.Invoke(item);

        private void UpdateAllPowerupVisuals()
        {
            if (_powerupUIElements == null) return;
            int playerLevel = SaveManager.GetCurrentLevelIndex();
            foreach (var pu in _powerupUIElements)
            {
                if (!pu || pu.Data == null) continue;
                bool locked = pu.Data.IsLockedAt(playerLevel);
                int count = locked ? 0 : SaveManager.GetPowerupCount(pu.Data.id);
                pu.UpdateVisuals(count, locked);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ── Debug: force-activate (bypasses unlock level + cost) ──────────
        [Button("Debug: Activate Vacuum")]
        public void DebugActivateVacuum() => ForceActivate("vacuum");

        [Button("Debug: Activate Fan")]
        public void DebugActivateFan() => ForceActivate("fan");

        [Button("Debug: Activate Freeze")]
        public void DebugActivateFreeze() => ForceActivate("freeze");

        [Button("Debug: Activate Spring")]
        public void DebugActivateSpring() => ForceActivate("spring");

        /// <summary>Runs a power-up's effect immediately, ignoring unlock level and charges. Needs GAME state.</summary>
        public void ForceActivate(string id)
        {
            if (database == null) return;
            PowerupDataSO so = database.FindById(id);
            if (so == null || so.Effect == null)
            {
                Debug.LogWarning($"[PowerupManager] No effect found for '{id}'.");
                return;
            }
            if (GameManager.Instance.State != EGameState.GAME)
            {
                Debug.LogWarning($"[PowerupManager] ForceActivate('{id}') needs GAME state (current: {GameManager.Instance.State}).");
                return;
            }
            so.Effect.Activate(BuildContext(so));
        }
#endif
    }
}
