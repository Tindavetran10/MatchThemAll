using System.Collections;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class TimerManager : MonoBehaviour
    {
        public static TimerManager Instance;
        
        [Header("Elements")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Freeze")]
        [Tooltip("Seconds the Freeze power-up stops the clock. Only counts down while playing (not paused / in popups).")]
        [SerializeField] private float freezeDuration = 10f;

        /// <summary>Remaining time in seconds. Read by WinPanelManager for star calculation.</summary>
        public int CurrentTime { get; private set; }

        private bool _isRunning;
        private bool _resumeAfterOffer; // was the timer counting when the power-up offer popup opened?
        private float _freezeLeft;

        // Cached to avoid allocating a new WaitForSeconds every tick
        private static readonly WaitForSeconds OneSecondWait = new(1f);

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);
            
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
            // Freeze countdown: only while actually playing, so pause / popups don't eat the freeze.
            if (!IsFrozen || !GameManager.Instance.IsGame()) return;
            _freezeLeft -= Time.deltaTime;
            if (_freezeLeft <= 0f) EndFreeze(restartTimer: true);
        }

        private void OnLevelSpawned(Level level)
        {
            if (IsFrozen) EndFreeze(restartTimer: false);
            CurrentTime = level.Duration;
            UpdateTimerText();
            StartTimer();
        }

        private void StartTimer()
        {
            StopTimer(); // cancel any already-running timer before starting a new one
            _isRunning = true;
            StartCoroutine(TimerCoroutine());
        }

        private IEnumerator TimerCoroutine()
        {
            while (CurrentTime > 0)
            {
                yield return OneSecondWait;
                if (!_isRunning) yield break;        // respect Stop/Pause

                CurrentTime--;
                UpdateTimerText();
            }

            TimerFinished();
        }

        private void UpdateTimerText()
        {
            // TMP's SetText(string, float, float) writes directly into the text buffer — zero string allocation
            timerText.SetText("{0:00}:{1:00}", CurrentTime / 60, CurrentTime % 60);
        }

        private void TimerFinished()
        {
            StopTimer();
            GameManager.Instance.SetGameState(EGameState.OUTOFTIME);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case EGameState.PAUSED:
                case EGameState.OUTOFTIME:
                    StopTimer();
                    break;
                case EGameState.POWERUPOFFER:
                    // Resume later only if it was counting now (not frozen, not tutorial-paused, level already started).
                    _resumeAfterOffer = _isRunning;
                    StopTimer();
                    break;
                case EGameState.GAME when GameManager.Instance.PreviousState == EGameState.PAUSED || GameManager.Instance.PreviousState == EGameState.OUTOFTIME:
                    if (!IsFrozen) StartTimer(); // resume from pause or out of time (a running freeze restarts it itself)
                    break;
                case EGameState.GAME when GameManager.Instance.PreviousState == EGameState.POWERUPOFFER:
                    if (_resumeAfterOffer) StartTimer();
                    break;
                case EGameState.LEVELCOMPLETE or EGameState.GAMEOVER:
                    StopTimer();
                    if (IsFrozen) EndFreeze(restartTimer: false);
                    break;
            }
        }

        public bool IsFrozen { get; private set; }

        /// <summary>Freeze time left, 1 → 0 (0 when not frozen). Drives the freeze bar under the clock.</summary>
        public float FreezeRemaining01 => IsFrozen && freezeDuration > 0f ? Mathf.Clamp01(_freezeLeft / freezeDuration) : 0f;

        private void StopTimer()
        {
            _isRunning = false;
            StopAllCoroutines();
        }

        public void FreezeTimer()
        {
            if (IsFrozen) return;
            IsFrozen = true;
            _freezeLeft = freezeDuration;
            StopTimer();
            EventBus.Publish(new TimerFrozenEvent(freezeDuration));
        }

        private void EndFreeze(bool restartTimer)
        {
            IsFrozen = false;
            _freezeLeft = 0f;
            EventBus.Publish(new TimerUnfrozenEvent());
            if (restartTimer && GameManager.Instance.IsGame()) StartTimer();
        }

        public void AddTime(int seconds)
        {
            CurrentTime += seconds;
            UpdateTimerText();
        }

        public void SetTutorialPause(bool isPaused)
        {
            if (isPaused)
            {
                StopTimer();
            }
            else
            {
                // Only start if we are in the GAME state (and not frozen — the freeze restarts it when it ends)
                if (GameManager.Instance.IsGame() && !IsFrozen)
                {
                    StartTimer();
                }
            }
        }

#if UNITY_EDITOR
        [NaughtyAttributes.Button("Test: Run Out of Time")]
        private void DebugRunOutOfTime()
        {
            if (!GameManager.Instance.IsGame()) return;
            CurrentTime = 0;
            UpdateTimerText();
            TimerFinished();
        }
#endif
    }
}
