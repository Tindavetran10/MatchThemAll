namespace MatchThemAll.Scripts
{
    public interface IGameStateListener
    {
        void GameStateChangedCallback(EGameState gameState);
    }
}