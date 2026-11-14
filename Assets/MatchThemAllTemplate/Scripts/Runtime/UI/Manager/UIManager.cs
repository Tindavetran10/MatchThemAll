using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>Shows the panel that belongs to each game state; panels with a UIAnimator animate out.</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        private void Awake() => EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        private void OnDestroy() => EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var s = evt.NewState;
            // The HUD (goals, timer) stays up under the pause, out-of-time and power-up popups.
            Show(gamePanel, s is EGameState.GAME or EGameState.PAUSED or EGameState.OUTOFTIME or EGameState.POWERUPOFFER);
            Show(pausePanel, s == EGameState.PAUSED);
            Show(levelCompletePanel, s == EGameState.LEVELCOMPLETE);
            Show(gameOverPanel, s == EGameState.GAMEOVER);
        }

        private static void Show(GameObject panel, bool visible)
        {
            if (!panel || panel.activeSelf == visible) return;
            if (visible) { panel.SetActive(true); return; }

            if (panel.TryGetComponent<UI.UIAnimator>(out var animator)) animator.ClosePanel();
            else panel.SetActive(false);
        }
    }
}
