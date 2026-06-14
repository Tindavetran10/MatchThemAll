using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class ComboManager : MonoBehaviour
    {
        private static ComboManager Instance { get; set; }

        [Header("Settings")]
        [SerializeField] private float comboTimeout = 2.0f;
        [SerializeField] private int bonusTimePerCombo = 2;

        private int CurrentCombo { get; set; }
        private float _lastMergeTime;

        public static Action<int> OnComboUpdated;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else Destroy(gameObject);

            ItemSpotManager.MergeStarted += OnMergeStarted;
            LevelManager.LevelSpawned += OnLevelSpawned;
        }

        private void OnDestroy()
        {
            ItemSpotManager.MergeStarted -= OnMergeStarted;
            LevelManager.LevelSpawned -= OnLevelSpawned;
        }

        private void OnLevelSpawned(Level level) => ResetCombo();

        private void Update()
        {
            if (CurrentCombo <= 0 || !GameManager.Instance.IsGame()) return;
            if (Time.time - _lastMergeTime > comboTimeout)
            {
                ResetCombo();
            }
        }

        private void OnMergeStarted(List<Item> mergedItems)
        {
            if (!GameManager.Instance.IsGame()) return;

            // If this is the first merge, start tracking combo
            if (CurrentCombo == 0)
            {
                CurrentCombo = 1;
            }
            else
            {
                // Increment combo
                CurrentCombo++;
                
                // Grant bonus time
                if (TimerManager.Instance)
                {
                    TimerManager.Instance.AddTime(bonusTimePerCombo);
                }
                
                // Fire event for UI
                OnComboUpdated?.Invoke(CurrentCombo);
            }

            _lastMergeTime = Time.time;
        }

        private void ResetCombo()
        {
            CurrentCombo = 0;
            // Optionally, fire an event to hide combo UI
            OnComboUpdated?.Invoke(0);
        }
    }
}
