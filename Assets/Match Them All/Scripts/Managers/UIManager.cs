using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class UIManager : MonoBehaviour, IGameStateListener
    {
        [Header("Panels")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        public void GameStateChangedCallback(EGameState gameState)
        {

            // Keep the game HUD (goals, timer) visible while paused.
            gamePanel.SetActive(gameState is EGameState.GAME or EGameState.PAUSED);

            // Pause overlay sits on top of the game panel.
            SetPanelActive(pausePanel, gameState == EGameState.PAUSED);

            SetPanelActive(levelCompletePanel, gameState == EGameState.LEVELCOMPLETE);
            SetPanelActive(gameOverPanel, gameState == EGameState.GAMEOVER);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel == null) return;

            if (active)
            {
                panel.SetActive(true);
            }
            else if (panel.activeSelf)
            {
                var anim = panel.GetComponent<MatchThemAll.Scripts.UI.UIAnimator>();
                if (anim != null) anim.ClosePanel();
                else panel.SetActive(false);
            }
        }
    }
}
