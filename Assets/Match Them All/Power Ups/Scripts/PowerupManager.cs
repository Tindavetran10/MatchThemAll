using System;
using System.Collections.Generic;
using ZLinq;
using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Settings;
using NaughtyAttributes;
using UnityEngine;
using PrimeTween;

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

        [Header("Vacuum Elements")]
        [SerializeField] private Vacuum vacuum;              // the vacuum button (plays the suck animation)
        [SerializeField] private Transform vacuumSuckPosition;

        [Header("Global Settings")]
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("State")]
        private bool _isBusy;
        private bool _vacuumRequested;

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

            _powerupUIElements = FindObjectsByType<Powerup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pu in _powerupUIElements)
            {
                if (pu) pu.TryAutoResolve(database);
            }

            UpdateAllPowerupVisuals();

            Powerup.Started += OnVacuumStarted;            // animation handshake (vacuum)
            EventBus.Subscribe<PowerupClickedEvent>(OnPowerupClickedEvent);
            SaveManager.OnPowerupsChanged += UpdateAllPowerupVisuals;
        }

        private void OnDestroy()
        {
            Powerup.Started -= OnVacuumStarted;
            EventBus.Unsubscribe<PowerupClickedEvent>(OnPowerupClickedEvent);
            SaveManager.OnPowerupsChanged -= UpdateAllPowerupVisuals;
        }

        private void OnPowerupClickedEvent(PowerupClickedEvent evt)
        {
            PowerupDataSO so = evt.Powerup;
            if (so == null || so.Effect == null) return;
            if (_isBusy) return;
            if (GameManager.Instance.State != EGameState.GAME) return;

            int playerLevel = SaveManager.GetCurrentLevelIndex();
            if (so.IsLockedAt(playerLevel)) return;

            var ctx = BuildContext(so);
            if (!so.Effect.CanActivate(ctx)) return;

            // Use a charge; if none, buy one with the SO's currency/cost.
            if (!SaveManager.UsePowerupCharge(so.id))
            {
                if (!SaveManager.Spend(so.buyCurrency, so.buyCost))
                {
                    Debug.Log($"Not enough {so.buyCurrency} to buy {so.id}! Needs {so.buyCost}.");
                    return;
                }
            }

            // Vacuum waits for its suck animation; the others run immediately.
            if (so.id == "vacuum")
            {
                _isBusy = true;
                if (vacuum != null && vacuum.HasAnimator)
                {
                    // Sync collection to the suck animation via the Started animation event.
                    _vacuumRequested = true;
                    vacuum.Play();
                }
                else
                {
                    // No Animator wired (placeholder button) — run the effect immediately.
                    so.Effect.Activate(ctx);
                }
                UpdateAllPowerupVisuals();
            }
            else
            {
                so.Effect.Activate(ctx);
            }
        }

        private void OnVacuumStarted()
        {
            if (!_vacuumRequested) return;
            _vacuumRequested = false;

            // Find the vacuum SO and run its effect.
            PowerupDataSO vacuumSo = database != null ? database.FindById("vacuum") : null;
            if (vacuumSo != null && vacuumSo.Effect != null)
                vacuumSo.Effect.Activate(BuildContext(vacuumSo));
            else
                _isBusy = false;
        }

        /// <summary>Builds the per-activation dependency context handed to an effect.</summary>
        private PowerupContext BuildContext(PowerupDataSO so)
        {
            return new PowerupContext
            {
                Items = LevelManager.Instance != null ? LevelManager.Instance.Items : null,
                Goals = GoalManager.Instance != null ? GoalManager.Instance.Goals : null,
                ItemSpots = ItemSpotManager.Instance,
                Timer = TimerManager.Instance,
                GameSettings = gameSettings,
                VacuumSuckPosition = vacuumSuckPosition,
                OnItemPickup = InvokeItemPickup,
                OnItemBackToGame = InvokeItemBackToGame,
                SetBusy = busy => _isBusy = busy,
            };
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Spring trajectory preview, read from the SpringEffect on the spring SO.
            if (database == null) return;
            PowerupDataSO springSo = database.FindById("spring");
            if (springSo == null || !(springSo.Effect is SpringEffect spring)) return;

            ItemSpot spot = FindAnyObjectByType<ItemSpot>();
            Vector3 startPos = spot != null ? spot.transform.position : transform.position;
            spring.DrawGizmos(startPos);
        }
#endif
    }
}
