using System.Collections;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class TimerManager : MonoBehaviour, IGameStateListener
    {
        [Header("Elements")]
        [SerializeField] private TextMeshProUGUI timerText;

        private int  _currentTime;
        private bool _isRunning;

        private void Awake() => LevelManager.LevelSpawned += OnLevelSpawned;
        private void OnDestroy() => LevelManager.LevelSpawned -= OnLevelSpawned;

        private void OnLevelSpawned(Level level)
        {
            _currentTime = level.Duration;
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
            while (_currentTime > 0)
            {
                yield return new WaitForSeconds(1f); // wait a full second before decrementing
                if (!_isRunning) yield break;        // respect Stop/Pause

                _currentTime--;
                UpdateTimerText();
            }

            TimerFinished();
        }

        private void UpdateTimerText()
        {
            // Direct integer math — no TimeSpan, no Substring, no extra allocations
            int minutes = _currentTime / 60;
            int seconds = _currentTime % 60;
            timerText.text = $"{minutes:D2}:{seconds:D2}";
        }

        private void TimerFinished()
        {
            StopTimer();
            GameManager.Instance.SetGameState(EGameState.GAMEOVER);
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            switch (gameState)
            {
                case EGameState.PAUSED:
                    StopTimer();
                    break;
                case EGameState.GAME when GameManager.Instance.PreviousState == EGameState.PAUSED:
                    StartTimer(); // resume from pause — don't reset _currentTime
                    break;
                case EGameState.LEVELCOMPLETE or EGameState.GAMEOVER:
                    StopTimer();
                    break;
            }
        }

        private void StopTimer()
        {
            _isRunning = false;
            StopAllCoroutines();
        }
    }
}
