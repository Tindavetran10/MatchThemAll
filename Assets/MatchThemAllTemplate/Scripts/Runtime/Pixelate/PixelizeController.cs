using UnityEngine;

namespace MatchThemAll.Scripts.Pixelate
{
    /// <summary>
    /// Optional: add to any scene object to pixelate only while the game is paused.
    /// Without it, <see cref="PixelizeFeature.IsActive"/> keeps its default (always on).
    /// </summary>
    public class PixelizeController : MonoBehaviour
    {
        private void Awake() => EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        private void OnDestroy() => EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);

        private static void OnStateChanged(GameStateChangedEvent evt) =>
            PixelizeFeature.IsActive = evt.NewState == EGameState.PAUSED;
    }
}
