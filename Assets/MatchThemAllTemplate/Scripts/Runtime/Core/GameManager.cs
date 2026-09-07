using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public EGameState State { get; private set; }

        /// <summary>The state the game was in before the most recent SetGameState call.</summary>
        public EGameState PreviousState { get; private set; }

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else { Destroy(gameObject); return; }

            EventBus.Subscribe<SpotFilledEvent>(OnSpotFilled);

            // Disable PrimeTween warning for tweening to current value (e.g., when merging items already at target scale)
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;

            // Mobile: cap framerate and disable vSync to reduce battery drain
            // and prevent GPU thermal throttling on mid-range devices.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void OnDestroy() => EventBus.Unsubscribe<SpotFilledEvent>(OnSpotFilled);

        private void OnSpotFilled(SpotFilledEvent evt) => SetGameState(EGameState.GAMEOVER);

        private void Start() => StartGame();

        public void SetGameState(EGameState gameState)
        {
            PreviousState = State;
            State = gameState;

            EventBus.Publish(new GameStateChangedEvent(gameState));
        }

        public void StartGame()  => SetGameState(EGameState.GAME);
        public void PauseGame()  => SetGameState(EGameState.PAUSED);
        public void ResumeGame() => SetGameState(EGameState.GAME);

        public void QuitToMenuCallback()
        {
            ResumeGame(); // Resumes game state which smoothly closes the Pause panel
            SceneLoader.Load(SceneLoader.Lobby);
        }

        private static void RetryLevelCallback() =>
            // Just reload the current level from the Game Over or Pause menu
            SceneLoader.LoadLevel(SceneLoader.RequestedLevelIndex);

        // Keep these for backward compatibility if any old buttons use them
        public void NextButtonCallback()  => SceneLoader.LoadLevel(-1);
        public void RetryButtonCallback() => RetryLevelCallback();

        public bool IsGame() => State == EGameState.GAME;
    }
}