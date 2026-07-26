using MatchThemAll.Scripts;
using UnityEngine;

namespace MatchThemAll.Scripts.Pixelate
{
    /// <summary>
    /// Activates/deactivates the pixelate renderer feature based on game state.
    /// Attach this to any persistent GameObject (e.g., GameManager or a dedicated FX object).
    /// </summary>
    public class PixelizeController : MonoBehaviour
    {
        private void Awake() => EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        private void OnDestroy() => EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            PixelizeFeature.IsActive = evt.NewState == EGameState.PAUSED;
        }
    }
}
