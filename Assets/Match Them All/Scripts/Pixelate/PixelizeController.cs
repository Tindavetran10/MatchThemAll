using MatchThemAll.Scripts;
using UnityEngine;

namespace Match_Them_All.Scripts.Pixelate
{
    /// <summary>
    /// Activates/deactivates the pixelate renderer feature based on game state.
    /// Attach this to any persistent GameObject (e.g., GameManager or a dedicated FX object).
    /// </summary>
    public class PixelizeController : MonoBehaviour, IGameStateListener
    {
        public void GameStateChangedCallback(EGameState newState)
        {
            PixelizeFeature.IsActive = (newState == EGameState.PAUSED);
        }
    }
}
