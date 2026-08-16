#if UNITY_EDITOR || DEVELOPMENT_BUILD
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// DEVELOPMENT ONLY — auto-spawned cheat panel for play-testing.
    /// Select the "DebugCheats" object in the Hierarchy during Play mode to see
    /// the buttons; F1/F2/F3 work in-game without focusing the Inspector.
    /// Compiled out of release builds (same gate as DebugGrantCurrencyButton).
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private int coinsPerGrant = 5000;
        [SerializeField] private int gemsPerGrant = 100;
        [SerializeField] private int secondsPerGrant = 600;   // ~10 min — effectively infinite for a test run
        [SerializeField] private int powerupRefillAmount = 10;

        [Button("Cheat: +Coins (F1)")]
        public void AddCoins() => SaveManager.AddCoins(coinsPerGrant);

        [Button("Cheat: +Gems")]
        public void AddGems() => SaveManager.AddGems(gemsPerGrant);

        [Button("Cheat: +10 min Time (F2)")]
        public void AddTime()
        {
            if (TimerManager.Instance != null)
                TimerManager.Instance.AddTime(secondsPerGrant);
            else
                Debug.LogWarning("[DebugCheats] No TimerManager in this scene.");
        }

        [Button("Cheat: Refill All Powerups (F3)")]
        public void RefillPowerups()
        {
            var db = Resources.Load<PowerupDatabaseSO>("Powerups/PowerupDatabase");
            if (db == null)
            {
                Debug.LogWarning("[DebugCheats] PowerupDatabase not found in Resources.");
                return;
            }
            foreach (var so in db.Ordered)
            {
                if (!so) continue;
                int missing = powerupRefillAmount - SaveManager.GetPowerupCount(so.id);
                if (missing > 0)
                    SaveManager.AddPowerupCharge(so.id, missing);
            }
        }

        private void Update()
        {
            var kb = Keyboard.current; // project uses Input System only — UnityEngine.Input would throw
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) AddCoins();
            if (kb.f2Key.wasPressedThisFrame) AddTime();
            if (kb.f3Key.wasPressedThisFrame) RefillPowerups();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DebugCheats>() != null) return;
            var go = new GameObject("DebugCheats");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugCheats>();
        }
    }
}
#endif
