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
        PowerupVacuum = 15,
        PowerupSpring = 16,
        PowerupFan = 17,
        PowerupFreeze = 18,
        StarPop = 19,
        CoinReward = 20,

        // Results
        LevelComplete = 30,
        GameOver = 31,
        OutOfTime = 32,

        // Music (loops)
        MusicMenu = 40,
        MusicGame = 41,
    }
}
