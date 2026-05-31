using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class UIManager : MonoBehaviour, IGameStateListener
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        public void GameStateChangedCallback(EGameState gameState)
        {
            menuPanel.SetActive(gameState == EGameState.MENU);

            // Keep the game HUD (goals, timer) visible while paused.
            gamePanel.SetActive(gameState is EGameState.GAME or EGameState.PAUSED);

            // Pause overlay sits on top of the game panel.
            pausePanel.SetActive(gameState == EGameState.PAUSED);

            levelCompletePanel.SetActive(gameState == EGameState.LEVELCOMPLETE);
            gameOverPanel.SetActive(gameState == EGameState.GAMEOVER);
        }
    }
}
