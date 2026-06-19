using ZLinq;
using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        private EGameState _gameState;
        public EGameState State => _gameState;

        /// <summary>The state the game was in before the most recent SetGameState call.</summary>
        public EGameState PreviousState { get; private set; }

        // Cached once in Start() — avoids repeated FindObjectsByType allocations on every state change
        private IGameStateListener[] _cachedListeners;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else { Destroy(gameObject); return; }

            // Disable PrimeTween warning for tweening to current value (e.g., when merging items already at target scale)
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;

            // Mobile: cap framerate and disable vSync to reduce battery drain
            // and prevent GPU thermal throttling on mid-range devices.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            // Cache all listeners before the first state broadcast
            _cachedListeners = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .AsValueEnumerable()
                .OfType<IGameStateListener>()
                .ToArray();

            StartGame();
        }

        public void SetGameState(EGameState gameState)
        {
            PreviousState = _gameState;
            _gameState = gameState;

            foreach (IGameStateListener listener in _cachedListeners)
                listener.GameStateChangedCallback(gameState);
        }

        public void StartGame()  => SetGameState(EGameState.GAME);
        public void PauseGame()  => SetGameState(EGameState.PAUSED);
        public void ResumeGame() => SetGameState(EGameState.GAME);

        public void QuitToMenuCallback()
        {
            ResumeGame(); // Resumes game state which smoothly closes the Pause panel
            SceneLoader.Load(SceneLoader.MainMenu);
        }

        private static void RetryLevelCallback() =>
            // Just reload the current level from the Game Over or Pause menu
            SceneLoader.LoadLevel(SceneLoader.RequestedLevelIndex);

        // Keep these for backward compatibility if any old buttons use them
        public void NextButtonCallback()  => SceneLoader.LoadLevel(-1);
        public void RetryButtonCallback() => RetryLevelCallback();

        public bool IsGame() => _gameState == EGameState.GAME;
    }
}