using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
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
        private const string Loading     = "LoadingScene";
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
            SaveManager.Flush(); // Persist any dirty data before leaving
            TargetScene = sceneName;
            RequestedLevelIndex = -1;
            FadeAndLoad(Loading);
        }

        /// <summary>
        /// Loads the game scene with a specific level index.
        /// Pass -1 to continue from saved progress.
        /// </summary>
        public static void LoadLevel(int levelIndex = -1)
        {
            SaveManager.Flush(); // Persist any dirty data before leaving
            TargetScene = Game;
            RequestedLevelIndex = levelIndex;
            FadeAndLoad(Loading);
        }

        private static void FadeAndLoad(string nextScene)
        {
            // Create a persistent canvas for the fade
            var go = new UnityEngine.GameObject("SceneTransitionFader");
            UnityEngine.Object.DontDestroyOnLoad(go);

            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Render on top of everything

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0); // Start transparent

            // Fade to black over 0.4 seconds with OutQuad ease
            Tween.Custom(0f, 1f, 0.4f, onValueChange: val =>
                {
                    img.color = new Color(0, 0, 0, val);
                }, ease: Ease.OutQuad, useUnscaledTime: true)
                .OnComplete(() =>
                {
                    // Actually switch the scene once it's completely black
                    SceneManager.LoadScene(nextScene);

                    // Fade back out from black to transparent over 0.6 seconds with OutQuad ease
                    Tween.Custom(1f, 0f, 0.6f, onValueChange: val =>
                        {
                            if (img != null) img.color = new Color(0, 0, 0, val);
                        }, startDelay: 0.15f, ease: Ease.OutQuad, useUnscaledTime: true)
                        .OnComplete(() =>
                        {
                            // Clean up the fader when done
                            Object.Destroy(go);
                        });
                });
        }
    }
}
