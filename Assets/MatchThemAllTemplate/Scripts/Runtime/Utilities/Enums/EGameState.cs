namespace MatchThemAll.Scripts
{
    public enum EGameState
    {
        MENU = 0,
        GAME = 1,
        LEVELCOMPLETE = 2,
        GAMEOVER = 3,
        PAUSED = 4,
        OUTOFTIME = 5,
        POWERUPOFFER = 6  // "Coins or ad?" popup for a power-up with 0 charges: timer + input paused, HUD stays
    }
}