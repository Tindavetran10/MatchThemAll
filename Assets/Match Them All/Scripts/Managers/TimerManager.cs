using System.Collections;
using MatchThemAll.Scripts;
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

        private void Awake() => LevelManager.levelSpawned += OnLevelSpawned;
        private void OnDestroy() => LevelManager.levelSpawned -= OnLevelSpawned;

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
            timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        private void TimerFinished()
        {
            StopTimer();
            GameManager.instance.SetGameState(EGameState.GAMEOVER);
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            if (gameState is EGameState.LEVELCOMPLETE or EGameState.GAMEOVER)
                StopTimer();
        }

        private void StopTimer()
        {
            _isRunning = false;
            StopAllCoroutines();
        }
    }
}
