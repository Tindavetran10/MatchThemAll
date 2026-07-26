using UnityEngine;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>
    /// Tiny auto-created MonoBehaviour that hooks Unity lifecycle events
    /// to flush save data to disk. Created automatically via [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public class SaveManagerBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            // Initialize the cache early
            SaveManager.Initialize();

            // Create a persistent GameObject to receive lifecycle callbacks
            var go = new GameObject("[SaveManager]");
            go.AddComponent<SaveManagerBootstrapper>();
            DontDestroyOnLoad(go);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveManager.Flush();
        }

        private void OnApplicationQuit() => SaveManager.Flush();
    }
}
