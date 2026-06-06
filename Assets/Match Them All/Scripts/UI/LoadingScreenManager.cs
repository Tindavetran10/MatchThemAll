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
            var target = SceneLoader.TargetScene;
            if (string.IsNullOrEmpty(target))
                target = SceneLoader.Game; // fallback

            // Begin async load but don't activate yet
            AsyncOperation op = SceneManager.LoadSceneAsync(target);
            if (op == null) yield break;
            op.allowSceneActivation = false;

            float elapsed = 0f;

            while (!op.isDone)
            {
                elapsed += Time.deltaTime;

                // AsyncOperation reports 0-0.9 while loading, then jumps to 1 on activation
                float loadProgress = Mathf.Clamp01(op.progress / 0.9f);
                float timeProgress = Mathf.Clamp01(elapsed / minimumLoadTime);
                float displayProgress = Mathf.Min(loadProgress, timeProgress);

                if (progressBar != null) progressBar.value = displayProgress;
                if (progressText != null) progressText.text = $"{Mathf.RoundToInt(displayProgress * 100)}%";

                // Activate scene only when both loading and minimum time are done
                if (op.progress >= 0.9f && elapsed >= minimumLoadTime && !op.allowSceneActivation)
                {
                    // Create a fader to transition out of the loading screen smoothly
                    var go = new GameObject("LoadingTransitionFader");
                    DontDestroyOnLoad(go);

                    var canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 999;

                    var img = go.AddComponent<Image>();
                    img.color = new Color(0, 0, 0, 0);

                    LeanTween.value(go, 0f, 1f, 0.25f)
                        .setIgnoreTimeScale(true)
                        .setOnUpdate(val => { img.color = new Color(0, 0, 0, val); })
                        .setOnComplete(() =>
                        {
                            // Activate the scene
                            op.allowSceneActivation = true;

                            // Fade back in
                            LeanTween.value(go, 1f, 0f, 0.35f)
                                .setIgnoreTimeScale(true)
                                .setDelay(0.1f)
                                .setOnUpdate(val =>
                                {
                                    if (img != null) img.color = new Color(0, 0, 0, val);
                                })
                                .setOnComplete(() => { Destroy(go); });
                        });

                    // Break out of the loop since we handle activation via LeanTween now
                    break;
                }
                yield return null;
            }
        }
    }
}
