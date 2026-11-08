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
        public const string Lobby       = "Lobby";
        public const string LevelSelect = "LevelSelect";
        private const string Loading    = "LoadingScene";
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

        // ── Transition fade (shared with LoadingScreenManager so both hops look the same) ──
        private const float FadeDuration = 0.5f;   // to black and back, same length both ways
        private const float HoldOnBlack  = 0.1f;
        private const Ease  FadeEase     = Ease.InOutSine;

        private static async void FadeAndLoad(string nextScene)
        {
            try
            {
                var fader = CreateFader("SceneTransitionFader");
                await FadeToBlack(fader);
                SceneManager.LoadScene(nextScene); // switch once the screen is fully black
                await FadeFromBlack(fader);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Full-screen black overlay that survives the scene change, starting transparent.</summary>
        public static UnityEngine.UI.Image CreateFader(string name)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Render on top of everything

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0);
            return img;
        }

        public static Tween FadeToBlack(UnityEngine.UI.Image fader) =>
            Tween.Custom(0f, 1f, FadeDuration, val => fader.color = new Color(0, 0, 0, val), ease: FadeEase, useUnscaledTime: true);

        /// <summary>Fades the overlay back to transparent, then destroys it.</summary>
        public static async Awaitable FadeFromBlack(UnityEngine.UI.Image fader)
        {
            // A new scene's first frames can take hundreds of ms (load, Awake/Start, level map build).
            // Unscaled tweens count that time, so a fade started now would be mostly over before it is
            // ever drawn. Stay on black until 3 normal frames in a row (or 1.5 s), then fade.
            // Several frames, not one: SceneManager.LoadScene loads a frame late, so the frame right
            // after the call is still fast and the slow load frame only comes after it.
            float waitStart = Time.realtimeSinceStartup;
            int calmFrames = 0;
            while (calmFrames < 3 && Time.realtimeSinceStartup - waitStart < 1.5f)
            {
                await Awaitable.NextFrameAsync();
                calmFrames = Time.unscaledDeltaTime <= 0.1f ? calmFrames + 1 : 0;
            }

            await Tween.Custom(1f, 0f, FadeDuration, val => { if (fader) fader.color = new Color(0, 0, 0, val); },
                startDelay: HoldOnBlack, ease: FadeEase, useUnscaledTime: true);
            if (fader) Object.Destroy(fader.gameObject);
        }
    }
}
