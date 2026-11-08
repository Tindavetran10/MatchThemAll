using System;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// The level clock. Counts whole seconds down while running, stops for pause / popups / tutorial
    /// steps, and can be frozen by the Freeze power-up (the freeze itself only drains during play).
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        public static TimerManager Instance;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Freeze")]
        [Tooltip("Seconds the Freeze power-up stops the clock. Only counts down while playing (not paused / in popups).")]
        [SerializeField] private float freezeDuration = 10f;

        /// <summary>Remaining time in seconds. Read by WinPanelManager for star calculation.</summary>
        public int CurrentTime { get; private set; }

        public bool IsFrozen { get; private set; }

        /// <summary>Freeze time left, 1 → 0 (0 when not frozen). Drives the freeze bar under the clock.</summary>
        public float FreezeRemaining01 => IsFrozen && freezeDuration > 0f ? Mathf.Clamp01(_freezeLeft / freezeDuration) : 0f;

        private bool _ticking;
        private float _sinceLastTick;   // scaled seconds since the last whole-second step
        private bool _resumeAfterOffer; // was the clock ticking when the power-up offer popup opened?
        private float _freezeLeft;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            LevelManager.LevelSpawned += OnLevelSpawned;
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            LevelManager.LevelSpawned -= OnLevelSpawned;
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Update()
        {
            if (_ticking) Tick(Time.deltaTime);

            // Freeze drains only while actually playing, so pause / popups don't eat it.
            if (IsFrozen && GameManager.Instance.IsGame())
            {
                _freezeLeft -= Time.deltaTime;
                if (_freezeLeft <= 0f) EndFreeze(restartClock: true);
            }
        }

        private void Tick(float dt)
        {
            _sinceLastTick += dt;
            while (_ticking && _sinceLastTick >= 1f && CurrentTime > 0)
            {
                _sinceLastTick -= 1f;
                CurrentTime--;
                RefreshText();
            }

            if (_ticking && CurrentTime <= 0)
            {
                Halt();
                GameManager.Instance.SetGameState(EGameState.OUTOFTIME);
            }
        }

        private void Run()
        {
            _ticking = true;
            _sinceLastTick = 0f; // every (re)start waits a full second before the first step
        }

        private void Halt() => _ticking = false;

        private void RefreshText() =>
            timerText.SetText("{0:00}:{1:00}", CurrentTime / 60, CurrentTime % 60); // SetText(format, floats) doesn't allocate

        private void OnLevelSpawned(Level level)
        {
            if (IsFrozen) EndFreeze(restartClock: false);
            CurrentTime = level.Duration;
            RefreshText();
            Run();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var previous = GameManager.Instance.PreviousState;
            switch (evt.NewState)
            {
                case EGameState.PAUSED:
                case EGameState.OUTOFTIME:
                    Halt();
                    break;

                case EGameState.POWERUPOFFER:
                    // Resume later only if it is ticking now (not frozen, not tutorial-paused, level started).
                    _resumeAfterOffer = _ticking;
                    Halt();
                    break;

                case EGameState.GAME when previous is EGameState.PAUSED or EGameState.OUTOFTIME:
                    if (!IsFrozen) Run(); // a running freeze restarts the clock itself when it ends
                    break;

                case EGameState.GAME when previous == EGameState.POWERUPOFFER:
                    if (_resumeAfterOffer) Run();
                    break;

                case EGameState.LEVELCOMPLETE:
                case EGameState.GAMEOVER:
                    Halt();
                    if (IsFrozen) EndFreeze(restartClock: false);
                    break;
                case EGameState.MENU:
                case EGameState.GAME: // fresh start (MENU/LEVELCOMPLETE → GAME): the clock starts from LevelSpawned, not here
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void FreezeTimer()
        {
            if (IsFrozen) return;
            IsFrozen = true;
            _freezeLeft = freezeDuration;
            Halt();
            EventBus.Publish(new TimerFrozenEvent(freezeDuration));
        }

        private void EndFreeze(bool restartClock)
        {
            IsFrozen = false;
            _freezeLeft = 0f;
            EventBus.Publish(new TimerUnfrozenEvent());
            if (restartClock && GameManager.Instance.IsGame()) Run();
        }

        public void AddTime(int seconds)
        {
            CurrentTime += seconds;
            RefreshText();
        }

        public void SetTutorialPause(bool isPaused)
        {
            if (isPaused) Halt();
            else if (GameManager.Instance.IsGame() && !IsFrozen) Run(); // a freeze restarts the clock when it ends
        }

#if UNITY_EDITOR
        [NaughtyAttributes.Button("Test: Run Out of Time")]
        private void DebugRunOutOfTime()
        {
            if (!GameManager.Instance.IsGame()) return;
            CurrentTime = 0;
            RefreshText();
            Run();
            Tick(0f);
        }
#endif
    }
}
