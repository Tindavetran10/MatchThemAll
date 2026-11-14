using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Holds the current <see cref="EGameState"/> and broadcasts every change as a
    /// <see cref="GameStateChangedEvent"/>. The game scene starts in GAME; a full tray ends it.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public EGameState State { get; private set; }

        /// <summary>The state the game was in before the most recent SetGameState call.</summary>
        public EGameState PreviousState { get; private set; }

        public bool IsGame() => State == EGameState.GAME;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Mobile: 60 fps without vSync keeps battery use and thermal throttling down on mid-range phones.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Merges tween items to a scale they may already have; that is expected, not worth a warning.
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;

            EventBus.Subscribe<SpotFilledEvent>(OnTrayFull);
        }

        private void Start() => StartGame();

        private void OnDestroy() => EventBus.Unsubscribe<SpotFilledEvent>(OnTrayFull);

        private void OnTrayFull(SpotFilledEvent _) => SetGameState(EGameState.GAMEOVER);

        public void SetGameState(EGameState next)
        {
            (PreviousState, State) = (State, next);
            EventBus.Publish(new GameStateChangedEvent(next));
        }

        public void StartGame()  => SetGameState(EGameState.GAME);
        public void PauseGame()  => SetGameState(EGameState.PAUSED);
        public void ResumeGame() => SetGameState(EGameState.GAME);

        public void QuitToMenuCallback()
        {
            ResumeGame(); // leaving PAUSED lets the Pause panel close itself
            SceneLoader.Load(SceneLoader.Lobby);
        }

        // Button hooks kept for scenes that still reference them.
        public void NextButtonCallback()  => SceneLoader.LoadLevel(-1);
        public void RetryButtonCallback() => SceneLoader.LoadLevel(SceneLoader.RequestedLevelIndex);
    }
}
