using UnityEngine.SceneManagement;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Static helper for all scene transitions. Always routes through the Loading
    /// scene so every transition gets a consistent loading screen.
    ///
    /// Usage:
    ///   SceneLoader.Load("MainMenu");
    ///   SceneLoader.LoadLevel(index);  // sets requested index, then loads MainScene
    /// </summary>
    public static class SceneLoader
    {
        // Scene name constants — update these if you rename your scenes
        public const string MainMenu    = "MainMenu";
        public const string LevelSelect = "LevelSelect";
        public const string Loading     = "LoadingScene";
        public const string Game        = "MainScene";

        /// <summary>
        /// The scene the LoadingScreen will load after its progress bar completes.
        /// Set automatically by Load() before switching to the loading scene.
        /// </summary>
        public static string TargetScene { get; private set; }

        /// <summary>
        /// The level index to play. -1 means "use saved progress" (normal progression).
        /// Set by LoadLevel() when replaying a specific level from the Level Select screen.
        /// </summary>
        public static int RequestedLevelIndex { get; private set; } = -1;

        /// <summary>Loads any scene by name, routing through the loading screen.</summary>
        public static void Load(string sceneName)
        {
            TargetScene = sceneName;
            RequestedLevelIndex = -1;
            SceneManager.LoadScene(Loading);
        }

        /// <summary>
        /// Loads the game scene with a specific level index.
        /// Pass -1 to continue from saved progress.
        /// </summary>
        public static void LoadLevel(int levelIndex = -1)
        {
            TargetScene = Game;
            RequestedLevelIndex = levelIndex;
            SceneManager.LoadScene(Loading);
        }
    }
}
