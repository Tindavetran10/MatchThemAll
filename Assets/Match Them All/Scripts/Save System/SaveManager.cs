using System;
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Power_Ups;
using UnityEngine;

namespace MatchThemAll.Scripts.SaveSystem
{
    // LevelManager (MatchThemAll.Scripts) referenced fully-qualified to avoid a namespace import cycle.
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
        public static event Action<int> OnGemsChanged;
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

        // ── Gems (premium currency) ─────────────────────────────────────────

        public static int GetGems() => Data.gems;

        public static void AddGems(int amount)
        {
            if (amount == 0) return;
            Data.gems += amount;
            MarkDirty();
            OnGemsChanged?.Invoke(Data.gems);
        }

        public static bool SpendGems(int amount)
        {
            if (Data.gems < amount) return false;
            Data.gems -= amount;
            MarkDirty();
            OnGemsChanged?.Invoke(Data.gems);
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

        // ── Shop: first-purchase bonus claims ──────────────────────────────

        /// <summary>True if this product's one-time first-purchase bonus was already claimed.</summary>
        public static bool HasClaimedFirstBonus(string productId)
            => !string.IsNullOrEmpty(productId)
               && Data.claimedFirstBonusProductIds != null
               && Data.claimedFirstBonusProductIds.Contains(productId);

        /// <summary>
        /// Atomically claims a product's first-purchase bonus. Returns true if THIS call claimed it
        /// (caller may grant the bonus), false if it was already claimed or the id is empty.
        /// </summary>
        public static bool MarkFirstBonusClaimed(string productId)
        {
            if (string.IsNullOrEmpty(productId) || HasClaimedFirstBonus(productId)) return false;
            Data.claimedFirstBonusProductIds ??= new List<string>();
            Data.claimedFirstBonusProductIds.Add(productId);
            MarkDirty();
            Flush();
            return true;
        }

        /// <summary>Seeds each database entry's defaultAmount on first launch.</summary>
        public static void InitializePowerups(PowerupDatabaseSO database)
        {
            if (Data.hasInitializedPowerups) return;
            Data.hasInitializedPowerups = true;

            if (database)
            {
                foreach (var so in database.Ordered)
                {
                    if (so && so.defaultAmount > 0)
                        Data.SetPowerupCount(so.id, so.defaultAmount);
                }
            }
            MarkDirty();
            Flush(); // First launch — write immediately
        }

        // ── Currency ───────────────────────────────────────────────────────

        /// <summary>Spends an amount of a currency. Dispatches per ECurrency.</summary>
        public static bool Spend(ECurrency currency, int amount) => currency switch
        {
            ECurrency.Coins => SpendCoins(amount),
            ECurrency.Gems  => SpendGems(amount),
            _ => false
        };

        public static int GetCurrency(ECurrency currency) => currency switch
        {
            ECurrency.Coins => Data.coins,
            ECurrency.Gems  => Data.gems,
            _ => 0
        };

        // ── Level Progress (keyed by stable level identity) ─────────────────

        /// <summary>ID of the furthest unlocked level. Empty if no progress.</summary>
        public static string GetFurthestLevelId() => Data.furthestLevelId ?? "";

        /// <summary>Best stars for a level id, or 0.</summary>
        public static int GetLevelStars(string id) => Data.GetLevelStars(id);

        /// <summary>True if the level id is unlocked (== furthest or any level before it in the ordered list).</summary>
        public static bool IsLevelUnlocked(string id, IReadOnlyList<string> orderedIds)
        {
            if (string.IsNullOrEmpty(id) || orderedIds == null || orderedIds.Count == 0) return false;
            string furthest = Data.furthestLevelId;
            if (string.IsNullOrEmpty(furthest)) return id.Equals(orderedIds[0]); // nothing played → only first open
            int furthestIdx = IndexOfId(orderedIds, furthest);
            int targetIdx = IndexOfId(orderedIds, id);
            return furthestIdx >= 0 && targetIdx >= 0 && targetIdx <= furthestIdx;
        }

        /// <summary>
        /// Records completion of a level by id. Sets best stars, and advances furthestLevelId to the
        /// next level when <paramref name="isFurthest"/> (first-time frontier clear).
        /// </summary>
        public static void SetLevelComplete(string id, int stars, bool isFurthest, IReadOnlyList<string> orderedIds)
        {
            if (string.IsNullOrEmpty(id)) return;
            Data.SetLevelStars(id, stars);

            if (isFurthest && orderedIds != null)
            {
                int idx = IndexOfId(orderedIds, id);
                if (idx >= 0 && idx + 1 < orderedIds.Count)
                    Data.furthestLevelId = orderedIds[idx + 1];
                // last level cleared → furthest stays (campaign complete)
            }
            MarkDirty();
            Flush(); // Level complete = natural save point
        }

        // ── Index bridge (for callers that still think in indices, e.g. PowerupManager unlock level) ──

        /// <summary>Position of the furthest level in the current ordered list, or 0.</summary>
        public static int GetCurrentLevelIndex()
        {
            var ids = LiveOrderedIds;
            string furthest = Data.furthestLevelId;
            if (ids == null || ids.Count == 0 || string.IsNullOrEmpty(furthest)) return 0;
            int idx = IndexOfId(ids, furthest);
            return idx >= 0 ? idx : 0;
        }

        /// <summary>The live ordered level id list from LevelManager, or null if unavailable (menu scenes).</summary>
        private static IReadOnlyList<string> LiveOrderedIds
            => LevelManager.Instance ? LevelManager.Instance.OrderedLevelIds : null;

        private static int IndexOfId(IReadOnlyList<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == id) return i;
            return -1;
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
