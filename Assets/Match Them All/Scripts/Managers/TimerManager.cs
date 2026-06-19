using System.Collections;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class TimerManager : MonoBehaviour, IGameStateListener
    {
        public static TimerManager Instance;
        
        [Header("Elements")]
        [SerializeField] private TextMeshProUGUI timerText;

        /// <summary>Remaining time in seconds. Read by WinPanelManager for star calculation.</summary>
        public int CurrentTime { get; private set; }

        private bool _isRunning;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);
            
            LevelManager.LevelSpawned += OnLevelSpawned;
        }

        private void OnDestroy() => LevelManager.LevelSpawned -= OnLevelSpawned;

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
                yield return new WaitForSeconds(1f); // wait a full second before decrementing
                if (!_isRunning) yield break;        // respect Stop/Pause

                CurrentTime--;
                UpdateTimerText();
            }

            TimerFinished();
        }

        private void UpdateTimerText()
        {
            // Direct integer math — no TimeSpan, no Substring, no extra allocations
            int minutes = CurrentTime / 60;
            int seconds = CurrentTime % 60;
            timerText.text = $"{minutes:D2}:{seconds:D2}";
        }

        private void TimerFinished()
        {
            StopTimer();
            GameManager.Instance.SetGameState(EGameState.OUTOFTIME);
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            switch (gameState)
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
