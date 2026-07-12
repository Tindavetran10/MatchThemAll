using System.Collections.Generic;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>One power-up's saved charge count, keyed by the PowerupDataSO.id.</summary>
    [System.Serializable]
    public class PowerupSaveEntry
    {
        public string id;
        public int count;
    }

    /// <summary>One level's saved star rating, keyed by the LevelDataSO.Id (stable identity).</summary>
    [System.Serializable]
    public class LevelProgressEntry
    {
        public string id;
        public int stars;
    }
    /// <summary>
    /// Plain data container for all persistent player progress.
    /// Serialized to JSON by SaveManager — no MonoBehaviour, no Unity lifecycle.
    /// Add new fields here as the game grows; SaveManager.Load() will default
    /// missing fields to 0/false automatically on older save files.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        // ── Level Progress (keyed by stable level identity) ────────────────
        /// <summary>Id (LevelDataSO.Id) of the furthest unlocked level. Source of truth for progression.</summary>
        public string furthestLevelId = "";
        /// <summary>Best stars per level, keyed by LevelDataSO.Id.</summary>
        public List<LevelProgressEntry> levelProgress = new();

        // ponytail: legacy positional fields — read once by MigrateLevelsToKeyed, then cleared. Removed Stage 3.
        public int currentLevelIndex;
        /// <summary>Best star rating (0-3) earned per level index (legacy positional).</summary>
        public int[] levelStars = System.Array.Empty<int>();

        // ── Player Engagement ──────────────────────────────────────────────
        public string lastPlayedDate = "";
        public int loginStreak = 0;

        // ── Economy ────────────────────────────────────────────────────────
        public int coins;

        // ── Powerup Charges ────────────────────────────────────────────────
        public bool hasInitializedPowerups = false;
        public List<PowerupSaveEntry> powerups = new();   // id-keyed map

        // ── Settings ───────────────────────────────────────────────────────
        public float musicVolume    = 1f;
        public float sfxVolume      = 1f;
        public bool  hapticsEnabled = true;

        /// <summary>
        /// Ensures the powerup map exists. (Old per-type fields were removed in Stage 3a;
        /// this is a no-op aside from the null-guard. Kept as the load hook for future migrations.)
        /// </summary>
        public void MigrateLegacyPowerups()
        {
            if (powerups == null) powerups = new List<PowerupSaveEntry>();
        }

        /// <summary>
        /// Migrates legacy positional level progress (currentLevelIndex + levelStars[]) into the
        /// id-keyed map, using the current ordered level id list. Idempotent: once levelProgress is
        /// populated it never re-runs, and legacy fields are cleared. Safe to call repeatedly.
        /// </summary>
        public void MigrateLevelsToKeyed(IReadOnlyList<string> orderedIds)
        {
            if (levelProgress == null) levelProgress = new List<LevelProgressEntry>();

            // Already keyed — just ensure legacy fields stay cleared.
            if (levelProgress.Count > 0 || !string.IsNullOrEmpty(furthestLevelId))
            {
                currentLevelIndex = 0;
                levelStars = System.Array.Empty<int>();
                return;
            }

            // Nothing legacy to migrate from.
            if (levelStars == null || levelStars.Length == 0)
            {
                currentLevelIndex = 0;
                return;
            }

            // Zip positional stars → keyed entries by id.
            if (orderedIds != null && orderedIds.Count > 0)
            {
                for (int i = 0; i < levelStars.Length && i < orderedIds.Count; i++)
                {
                    if (levelStars[i] > 0)
                        levelProgress.Add(new LevelProgressEntry { id = orderedIds[i], stars = levelStars[i] });
                }
                int lo = currentLevelIndex < 0 ? 0 : currentLevelIndex;
                if (lo > orderedIds.Count - 1) lo = orderedIds.Count - 1;
                furthestLevelId = orderedIds[lo];
            }

            currentLevelIndex = 0;
            levelStars = System.Array.Empty<int>();
        }

        /// <summary>Best stars for a level id, or 0 if absent.</summary>
        public int GetLevelStars(string id)
        {
            if (levelProgress == null || string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < levelProgress.Count; i++)
                if (levelProgress[i].id == id) return levelProgress[i].stars;
            return 0;
        }

        /// <summary>Sets (or inserts) the star rating for a level id, keeping the max.</summary>
        public void SetLevelStars(string id, int stars)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (levelProgress == null) levelProgress = new List<LevelProgressEntry>();
            for (int i = 0; i < levelProgress.Count; i++)
            {
                if (levelProgress[i].id == id)
                {
                    if (stars > levelProgress[i].stars) levelProgress[i].stars = stars;
                    return;
                }
            }
            levelProgress.Add(new LevelProgressEntry { id = id, stars = stars });
        }

        /// <summary>Gets the saved count for an id, or 0 if absent.</summary>
        public int GetPowerupCount(string id)
        {
            if (powerups == null || string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < powerups.Count; i++)
                if (powerups[i].id == id) return powerups[i].count;
            return 0;
        }

        /// <summary>Sets (or inserts) the count for an id.</summary>
        public void SetPowerupCount(string id, int count)
        {
            if (powerups == null) powerups = new List<PowerupSaveEntry>();
            for (int i = 0; i < powerups.Count; i++)
            {
                if (powerups[i].id == id) { powerups[i].count = count; return; }
            }
            powerups.Add(new PowerupSaveEntry { id = id, count = count });
        }
    }
}
