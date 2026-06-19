using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Controls the Win/Level Complete panel.
    /// Calculates star rating based on time remaining, triggers the sequential
    /// PrimeTween star pop-in animation, saves progress via LevelManager, and
    /// provides navigation callbacks.
    ///
    /// Star thresholds (based on % of level time remaining when completing):
    ///   3 stars — > 60% remaining
    ///   2 stars — > 30% remaining
    ///   1 star  — any completion
    /// </summary>
    public class WinPanelManager : MonoBehaviour
    {
        [Header("Stars")]
        [Tooltip("Assign exactly 3 star GameObjects in order (star 1, 2, 3).")]
        [SerializeField] private GameObject[] starObjects;

        [Header("Animation")]
        [SerializeField] private float starPopDuration = 0.3f;
        [SerializeField] private float starPopDelay    = 0.25f;
        [SerializeField] private float starOvershoot   = 1.25f;

        private void OnEnable()
        {
            // Hide all stars immediately when panel opens (before animating)
            ResetStars();

            // Calculate stars from remaining time
            int stars = CalculateStars();

            // Reward coins: 10 per star
            int coinsEarned = stars * 10;
            SaveSystem.SaveManager.AddCoins(coinsEarned);

            // Save progress (advances level index if this was a new level)
            LevelManager.Instance?.SaveLevelComplete(stars);

            // Animate stars in after a short opening delay
            AnimateStars(stars);
        }

        // ── Star Rating ──────────────────────────────────────────────────────

        private static int CalculateStars()
        {
            if (LevelManager.Instance == null) return 1;

            int total     = LevelManager.Instance.TotalLevelDuration;
            int remaining = TimerManager.Instance ? TimerManager.Instance.CurrentTime : 0;

            if (total <= 0) return 1;

            float ratio = (float)remaining / total;
            if (ratio > 0.6f) return 3;
            if (ratio > 0.3f) return 2;
            return 1;
        }

        // ── Star Animation ───────────────────────────────────────────────────

        private void ResetStars()
        {
            foreach (var star in starObjects)
            {
                if (!star) continue;
                star.SetActive(true);
                star.transform.localScale = Vector3.zero;
            }
        }

        private void AnimateStars(int count)
        {
            for (var i = 0; i < starObjects.Length; i++)
            {
                GameObject star = starObjects[i];
                if (!star) continue;

                var earned = i < count;
                if (!earned)
                {
                    // Unearned stars stay small / greyed out — just show them at scale 0.6
                    float delay = starPopDelay * i + 0.2f;
                    Tween.Scale(star.transform, Vector3.one * 0.6f, starPopDuration * 0.5f, Ease.OutBack, startDelay: delay);
                    continue;
                }

                // Earned star: pop in with overshoot bounce
                var popDelay = starPopDelay * i + 0.2f;
                Tween.Scale(star.transform, Vector3.one * starOvershoot, starPopDuration, Ease.OutBack, startDelay: popDelay)
                         .OnComplete(() =>
                         {
                             Tween.Scale(star.transform, Vector3.one, starPopDuration * 0.4f, Ease.InOutSine);
                         });
            }
        }

        // ── Button Callbacks ─────────────────────────────────────────────────

        /// <summary>Called by the Next Level button.</summary>
        public void OnNextLevelClicked()
        {
            // -1 = use saved progress (which was already advanced by SaveLevelComplete)
            SceneLoader.LoadLevel(-1);
        }

        /// <summary>Called by the Level Select button.</summary>
        public void OnLevelSelectClicked() => SceneLoader.Load(SceneLoader.LevelSelect);
    }
}
