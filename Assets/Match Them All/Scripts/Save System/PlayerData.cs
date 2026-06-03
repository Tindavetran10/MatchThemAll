using System;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>
    /// Plain data container for all persistent player progress.
    /// Serialized to JSON by SaveManager — no MonoBehaviour, no Unity lifecycle.
    /// Add new fields here as the game grows; SaveManager.Load() will default
    /// missing fields to 0/false automatically on older save files.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // ── Level Progress ─────────────────────────────────────────────────
        public int currentLevelIndex;

        // ── Powerup Charges ────────────────────────────────────────────────
        public int vacuumCount;
        public int springCount;
        public int fanCount;
        public int freezeCount;
    }
}
