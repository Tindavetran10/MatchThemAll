using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>One power-up's saved charge count, keyed by the PowerupDataSO.id.</summary>
    [System.Serializable]
    public class PowerupSaveEntry
    {
        public string id;
        public int count;
    }

    /// <summary>One level's saved star rating, keyed by the LevelDataSO.ID (stable identity).</summary>
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
        /// <summary>ID (LevelDataSO.ID) of the furthest unlocked level. Source of truth for progression.</summary>
        public string furthestLevelId = "";
        /// <summary>Best stars per level, keyed by LevelDataSO.ID.</summary>
        public List<LevelProgressEntry> levelProgress = new();

        // ── Player Engagement ──────────────────────────────────────────────
        public string lastPlayedDate = "";
        public int loginStreak = 0;

        // ── Economy ────────────────────────────────────────────────────────
        public int coins;
        /// <summary>Premium (hard) currency — bought with real money via IAP; spent on coin/power-up packs.</summary>
        public int gems;

        // ── Powerup Charges ────────────────────────────────────────────────
        public bool hasInitializedPowerups = false;
        public List<PowerupSaveEntry> powerups = new();   // id-keyed map

        // ── Shop (first-purchase bonus tracking) ───────────────────────────
        /// <summary>Product ids whose one-time first-purchase bonus has already been claimed.</summary>
        public List<string> claimedFirstBonusProductIds = new();

        // ── Settings ───────────────────────────────────────────────────────
        public float musicVolume    = 1f;
        public float sfxVolume      = 1f;
        public bool  hapticsEnabled = true;

        /// <summary>
        /// Ensures the powerup map exists. (Old per-type fields were removed in Stage 3a;
        /// this is a no-op aside from the null-guard. Kept as the load hook for future migrations.)
        /// </summary>
        public void MigrateLegacyPowerups() => powerups ??= new List<PowerupSaveEntry>();

        /// <summary>
        /// Ensures the level-progress list exists. (Legacy positional fields were removed; this is now
        /// a defensive init guard retained as a load hook for any future schema migration.)
        /// </summary>
        public void MigrateLevelsToKeyed(IReadOnlyList<string> orderedIds)
            => levelProgress ??= new List<LevelProgressEntry>();

        /// <summary>Best stars for a level id, or 0 if absent.</summary>
        public int GetLevelStars(string id)
        {
            if (levelProgress == null || string.IsNullOrEmpty(id)) return 0;
            
            return levelProgress.AsValueEnumerable()
                                .FirstOrDefault(t => t.id == id)?.stars ?? 0;
        }


        /// <summary>Sets (or inserts) the star rating for a level id, keeping the max.</summary>
        public void SetLevelStars(string id, int stars)
        {
            if (string.IsNullOrEmpty(id)) return;
            levelProgress ??= new List<LevelProgressEntry>();
            foreach (var t in levelProgress.AsValueEnumerable().Where(t => t.id == id))
            {
                if (stars > t.stars) t.stars = stars;
                return;
            }
            levelProgress.Add(new LevelProgressEntry { id = id, stars = stars });
        }

        /// <summary>Gets the saved count for an id, or 0 if absent.</summary>
        public int GetPowerupCount(string id)
        {
            if (powerups == null || string.IsNullOrEmpty(id)) return 0;
            return powerups.AsValueEnumerable().FirstOrDefault(t => t.id == id)?.count ?? 0;
        }

        /// <summary>Sets (or inserts) the count for an id.</summary>
        public void SetPowerupCount(string id, int count)
        {
            powerups ??= new List<PowerupSaveEntry>();
            foreach (var t in powerups.AsValueEnumerable().Where(t => t.id == id))
            {
                t.count = count; return;
            }
            powerups.Add(new PowerupSaveEntry { id = id, count = count });
        }
    }
}
