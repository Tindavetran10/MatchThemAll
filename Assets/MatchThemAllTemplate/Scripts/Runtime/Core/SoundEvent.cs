namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Every sound the game triggers. Clips for each one are assigned in the Sound Manager window
    /// (Match Them All → Sound Manager). Play from code with <c>SoundManager.Sfx(SoundEvent.X)</c>.
    ///
    /// Values are explicit because they are serialized in the SoundLibrary — never renumber; append new ones.
    /// A new value automatically shows up in the editor window.
    /// </summary>
    public enum SoundEvent
    {
        None = 0,

        // UI
        ButtonClick = 1,
        ButtonClose = 2,
        Error = 3,
        PurchaseSuccess = 4,
        RewardClaim = 5,

        // Gameplay
        ItemSelect = 10,
        ItemPlace = 11,
        Merge = 12,
        Combo = 13,
        GoalComplete = 14,
        // 15–18 retired (were the power-up sounds; each PowerupDataSO now carries its own clip) — don't reuse.
        StarPop = 19,
        CoinReward = 20,
        SpringLand = 21,

        // Results
        LevelComplete = 30,
        GameOver = 31,
        OutOfTime = 32,

        // Music (loops)
        MusicMenu = 40,
        MusicGame = 41,
    }
}
