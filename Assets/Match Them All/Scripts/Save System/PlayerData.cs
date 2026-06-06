namespace MatchThemAll.Scripts.SaveSystem
{
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

        /// <summary>Best star rating (0-3) earned per level index.</summary>
        public int[] levelStars = System.Array.Empty<int>();

        // ── Powerup Charges ────────────────────────────────────────────────
        public int vacuumCount;
        public int springCount;
        public int fanCount;
        public int freezeCount;

        // ── Settings ───────────────────────────────────────────────────────
        public float musicVolume    = 1f;
        public float sfxVolume      = 1f;
        public bool  hapticsEnabled = true;

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
