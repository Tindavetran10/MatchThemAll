using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Attached to the LoadingScene. Reads SceneLoader.TargetScene, asynchronously
    /// loads it in the background, and updates the progress bar UI until done.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI tipText;

        [Header("Settings")]
        [Tooltip("Minimum time (seconds) to show the loading screen so it doesn't flash.")]
        [SerializeField] private float minimumLoadTime = 0.8f;

        private static readonly string[] Tips =
        {
            "Tip: Use the Vacuum powerup on your biggest goal!",
            "Tip: The Fan powerup scatters all items — use it to break clusters!",
            "Tip: Freeze the timer when you're almost out of time!",
            "Tip: The Spring powerup releases a random item back to play!",
        };

        private void Start() => StartCoroutine(LoadAsync());

        private IEnumerator LoadAsync()
        {
            // Show a random tip
            if (tipText != null)
                tipText.text = Tips[Random.Range(0, Tips.Length)];

            // Determine target scene
            string target = SceneLoader.TargetScene;
            if (string.IsNullOrEmpty(target))
                target = SceneLoader.Game; // fallback

            // Begin async load but don't activate yet
            AsyncOperation op = SceneManager.LoadSceneAsync(target);
            op.allowSceneActivation = false;

            float elapsed = 0f;

            while (!op.isDone)
            {
                elapsed += Time.deltaTime;

                // AsyncOperation reports 0-0.9 while loading, then jumps to 1 on activation
                float loadProgress  = Mathf.Clamp01(op.progress / 0.9f);
                float timeProgress  = Mathf.Clamp01(elapsed / minimumLoadTime);
                float displayProgress = Mathf.Min(loadProgress, timeProgress);

                if (progressBar != null) progressBar.value = displayProgress;
                if (progressText != null) progressText.text = $"{Mathf.RoundToInt(displayProgress * 100)}%";

                // Activate scene only when both loading and minimum time are done
                if (op.progress >= 0.9f && elapsed >= minimumLoadTime)
                    op.allowSceneActivation = true;

                yield return null;
            }
        }
    }
}
