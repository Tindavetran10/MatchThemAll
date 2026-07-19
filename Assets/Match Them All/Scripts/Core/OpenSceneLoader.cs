using System.Collections;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Attached to the OpenScene. Shows the splash/open screen for a brief moment,
    /// then loads the Lobby scene through the standard loading pipeline.
    /// </summary>
    public class OpenSceneLoader : MonoBehaviour
    {
        [Tooltip("How long (seconds) to display the open/splash screen before transitioning to the Lobby.")]
        [SerializeField] private float displayDuration = 2f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(displayDuration);
            SceneLoader.Load(SceneLoader.Lobby);
        }
    }
}
