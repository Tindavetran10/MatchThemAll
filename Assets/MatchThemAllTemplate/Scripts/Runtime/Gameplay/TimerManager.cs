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

        /// <summary>Remaining time in seconds. Read by WinPanelManager for star calculation.</summary>
        public int CurrentTime { get; private set; }

        private bool _isRunning;
        
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

        private void OnLevelSpawned(Level level)
        {
            CancelInvoke(nameof(UnfreezeTimer));
            IsFrozen = false;
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
                case EGameState.GAME when GameManager.Instance.PreviousState == EGameState.PAUSED || GameManager.Instance.PreviousState == EGameState.OUTOFTIME:
                    StartTimer(); // resume from pause or out of time
                    break;
                case EGameState.LEVELCOMPLETE or EGameState.GAMEOVER:
                    StopTimer();
                    break;
            }
        }

        public bool IsFrozen { get; private set; }

        private void StopTimer()
        {
            _isRunning = false;
            StopAllCoroutines();
        }

        public void FreezeTimer()
        {
            if (IsFrozen) return;
            IsFrozen = true;
            StopTimer();
            Invoke(nameof(UnfreezeTimer), 10f);
        }

        private void UnfreezeTimer()
        {
            IsFrozen = false;
            if (GameManager.Instance.IsGame())
            {
                StartTimer();
            }
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
                // Only start if we are in the GAME state
                if (GameManager.Instance.IsGame())
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
