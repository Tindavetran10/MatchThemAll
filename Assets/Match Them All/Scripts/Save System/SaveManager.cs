using System;
using System.IO;
using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts.Settings;
using UnityEngine;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>
    /// Centralized data access layer for all persistent player data.
    /// Holds an in-memory cache and flushes to disk only when needed
    /// (scene transitions, level completion, app pause/quit).
    ///
    /// Usage:
    ///   SaveManager.Initialize();               // call once at game boot
    ///   int coins = SaveManager.GetCoins();      // read from cache (free)
    ///   SaveManager.AddCoins(10);                // mutate cache, mark dirty
    ///   SaveManager.Flush();                     // write to disk if dirty
    ///   SaveManager.Wipe();                      // delete save file (full reset)
    ///
    /// File location: Application.persistentDataPath/save.json
    /// </summary>
    public static class SaveManager
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        // ── In-Memory Cache ─────────────────────────────────────────────────
        private static PlayerData _cache;
        private static bool _isDirty;

        // ── Events ──────────────────────────────────────────────────────────
        public static event Action<int> OnCoinsChanged;
        public static event Action OnPowerupsChanged;

        // ── Initialization ──────────────────────────────────────────────────

        /// <summary>
        /// Loads save data from disk into memory. Safe to call multiple times;
        /// only the first call actually reads the file.
        /// </summary>
        public static void Initialize()
        {
            if (_cache != null) return;
            _cache = LoadFromDisk();
            _cache.MigrateLegacyPowerups();   // id-keyed map (one-time, idempotent)
        }

        /// <summary>
        /// Ensures the cache is populated. All getters call this internally
        /// so consumers never need to worry about init order.
        /// </summary>
        private static PlayerData Data
        {
            get
            {
                if (_cache == null) Initialize();
                return _cache;
            }
        }

        // ── Coins ───────────────────────────────────────────────────────────

        public static int GetCoins() => Data.coins;

        public static void AddCoins(int amount)
        {
            Data.coins += amount;
            MarkDirty();
            OnCoinsChanged?.Invoke(Data.coins);
        }

        public static bool SpendCoins(int amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount;
            MarkDirty();
            OnCoinsChanged?.Invoke(Data.coins);
            return true;
        }

        // ── Powerups ────────────────────────────────────────────────────────

        /// <summary>Charge count for a power-up id (e.g. "vacuum"). 0 if unknown/absent.</summary>
        public static int GetPowerupCount(string id) => Data.GetPowerupCount(id);

        /// <summary>Consumes one charge; false if the player has none.</summary>
        public static bool UsePowerupCharge(string id)
        {
            int current = Data.GetPowerupCount(id);
            if (current <= 0) return false;
            Data.SetPowerupCount(id, current - 1);
            MarkDirty();
            OnPowerupsChanged?.Invoke();
            return true;
        }

        /// <summary>Adds charges to an id (e.g. from daily rewards).</summary>
        public static void AddPowerupCharge(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount == 0) return;
            Data.SetPowerupCount(id, Data.GetPowerupCount(id) + amount);
            MarkDirty();
            OnPowerupsChanged?.Invoke();
        }

        /// <summary>Seeds each database entry's defaultAmount on first launch.</summary>
        public static void InitializePowerups(PowerupDatabaseSO database)
        {
            if (Data.hasInitializedPowerups) return;
            Data.hasInitializedPowerups = true;

            if (database != null)
            {
                foreach (var so in database.Ordered)
                {
                    if (so != null && so.defaultAmount > 0)
                        Data.SetPowerupCount(so.id, so.defaultAmount);
                }
            }
            MarkDirty();
            Flush(); // First launch — write immediately
        }

        // ── Currency ───────────────────────────────────────────────────────

        /// <summary>Spends an amount of a currency. Dispatches per ECurrency (Coins now).</summary>
        public static bool Spend(ECurrency currency, int amount) => currency switch
        {
            ECurrency.Coins => SpendCoins(amount),
            _ => false
        };

        public static int GetCurrency(ECurrency currency) => currency switch
        {
            ECurrency.Coins => Data.coins,
            _ => 0
        };

        // ── Level Progress ──────────────────────────────────────────────────

        public static int GetCurrentLevelIndex() => Data.currentLevelIndex;

        public static int GetLevelStars(int levelIndex) => Data.GetLevelStars(levelIndex);

        /// <summary>
        /// Saves progress and stars on level completion.
        /// Only advances currentLevelIndex if this was a new (non-replay) level.
        /// Flushes to disk immediately since this is a natural save point.
        /// </summary>
        public static void SaveLevelComplete(int levelIndex, int savedProgressIndex, int starsEarned, out int newProgressIndex)
        {
            bool isNewLevel = levelIndex == savedProgressIndex;
            newProgressIndex = savedProgressIndex;
            if (isNewLevel)
            {
                newProgressIndex++;
                Data.currentLevelIndex = newProgressIndex;
            }

            Data.SetLevelStars(levelIndex, starsEarned);
            MarkDirty();
            Flush(); // Level complete = natural save point
        }

        // ── Daily Rewards ───────────────────────────────────────────────────

        public static (string lastPlayedDate, int loginStreak) GetDailyRewardData()
            => (Data.lastPlayedDate, Data.loginStreak);

        public static void SaveDailyRewardData(int loginStreak, string lastPlayedDate)
        {
            Data.loginStreak = loginStreak;
            Data.lastPlayedDate = lastPlayedDate;
            MarkDirty();
            Flush(); // Daily reward = natural save point
        }

        // ── Settings ────────────────────────────────────────────────────────

        public static (float musicVolume, float sfxVolume, bool hapticsEnabled) GetSettings()
            => (Data.musicVolume, Data.sfxVolume, Data.hapticsEnabled);

        public static void SaveMusicVolume(float value)
        {
            Data.musicVolume = value;
            MarkDirty();
        }

        public static void SaveSfxVolume(float value)
        {
            Data.sfxVolume = value;
            MarkDirty();
        }

        public static void SaveHaptics(bool enabled)
        {
            Data.hapticsEnabled = enabled;
            MarkDirty();
        }

        // ── Lifecycle ───────────────────────────────────────────────────────

        /// <summary>
        /// Writes the in-memory cache to disk if anything has changed.
        /// Call this on scene transitions, app pause, or app quit.
        /// </summary>
        public static void Flush()
        {
            if (!_isDirty || _cache == null) return;

            string json = JsonUtility.ToJson(_cache, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            _isDirty = false;

#if UNITY_EDITOR
            Debug.Log($"[SaveManager] Flushed to: {SavePath}");
#endif
        }

        /// <summary>
        /// Deletes the save file and resets the in-memory cache.
        /// The next access will return fresh defaults.
        /// </summary>
        public static void Wipe()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            _cache = new PlayerData();
            _isDirty = false;
            Debug.Log("[SaveManager] Save file wiped.");
        }

        // ── Internal ────────────────────────────────────────────────────────

        private static void MarkDirty() => _isDirty = true;

        private static PlayerData LoadFromDisk()
        {
            if (!File.Exists(SavePath))
                return new PlayerData();

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
            }
            catch
            {
                Debug.LogWarning("[SaveManager] Save file corrupted — returning defaults.");
                return new PlayerData();
            }
        }

        // ── Auto-Flush Hook ─────────────────────────────────────────────────
        // This is registered by a tiny MonoBehaviour bootstrapper (SaveManagerBootstrapper)
        // that calls Flush() on OnApplicationPause and OnApplicationQuit.

        /// <summary>
        /// Force-reloads the cache from disk. Only needed after an external
        /// tool (like the Template Editor) modifies the save file.
        /// </summary>
        public static void ForceReload()
        {
            _cache = LoadFromDisk();
            _isDirty = false;
        }
    }
}
