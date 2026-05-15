using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchThemAll.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;
        private EGameState gameState;

        // Cached once in Start() — avoids repeated FindObjectsByType allocations on every state change
        private IGameStateListener[] _cachedListeners;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else Destroy(gameObject);
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
            this.gameState = gameState;

            foreach (IGameStateListener listener in _cachedListeners)
                listener.GameStateChangedCallback(gameState);
        }

        public void StartGame() => SetGameState(EGameState.GAME);

        public void NextButtonCallback() => SceneManager.LoadScene(0);
        public void RetryButtonCallback() => SceneManager.LoadScene(0);

        public bool IsGame() => gameState == EGameState.GAME;
    }
}