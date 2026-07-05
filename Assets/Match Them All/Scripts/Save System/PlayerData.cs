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
    /// <summary>
    /// Plain data container for all persistent player progress.
    /// Serialized to JSON by SaveManager — no MonoBehaviour, no Unity lifecycle.
    /// Add new fields here as the game grows; SaveManager.Load() will default
    /// missing fields to 0/false automatically on older save files.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        // ── Level Progress ─────────────────────────────────────────────────
        public int currentLevelIndex;

        // ── Player Engagement ──────────────────────────────────────────────
        public string lastPlayedDate = "";
        public int loginStreak = 0;

        // ── Economy ────────────────────────────────────────────────────────
        public int coins;

        /// <summary>Best star rating (0-3) earned per level index.</summary>
        public int[] levelStars = System.Array.Empty<int>();

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

        /// <summary>Records stars for a level, keeping the best score.</summary>
        public void SetLevelStars(int levelIndex, int stars)
        {
            if (levelIndex < 0) return;
            if (levelStars == null || levelStars.Length <= levelIndex)
            {
                var expanded = new int[levelIndex + 1];
                if (levelStars != null)
                    System.Array.Copy(levelStars, expanded, levelStars.Length);
                levelStars = expanded;
            }
            if (stars > levelStars[levelIndex])
                levelStars[levelIndex] = stars;
        }

        /// <summary>Returns best stars earned for a level, or 0 if never played.</summary>
        public int GetLevelStars(int levelIndex)
        {
            if (levelStars == null || levelIndex >= levelStars.Length) return 0;
            return levelStars[levelIndex];
        }
    }
}
