using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchThemAll.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        private EGameState _gameState;
        private EGameState _previousGameState;

        /// <summary>The state the game was in before the most recent SetGameState call.</summary>
        public EGameState PreviousState => _previousGameState;

        // Cached once in Start() — avoids repeated FindObjectsByType allocations on every state change
        private IGameStateListener[] _cachedListeners;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else { Destroy(gameObject); return; }

            // Mobile: cap framerate and disable vSync to reduce battery drain
            // and prevent GPU thermal throttling on mid-range devices.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            // Cache all listeners before the first state broadcast
            _cachedListeners = FindObjectsByType<MonoBehaviour>()
                .OfType<IGameStateListener>()
                .ToArray();

            SetGameState(EGameState.MENU);
        }

        public void SetGameState(EGameState gameState)
        {
            _previousGameState = _gameState;
            _gameState = gameState;

            foreach (IGameStateListener listener in _cachedListeners)
                listener.GameStateChangedCallback(gameState);
        }

        public void StartGame()  => SetGameState(EGameState.GAME);
        public void PauseGame()  => SetGameState(EGameState.PAUSED);
        public void ResumeGame() => SetGameState(EGameState.GAME);

        public void NextButtonCallback()  => SceneManager.LoadScene(0);
        public void RetryButtonCallback() => SceneManager.LoadScene(0);

        public bool IsGame() => _gameState == EGameState.GAME;
    }
}