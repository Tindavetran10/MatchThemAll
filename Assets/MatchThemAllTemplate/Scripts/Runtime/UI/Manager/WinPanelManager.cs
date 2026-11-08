using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Controls the Win/Level Complete panel.
    /// Calculates star rating based on time remaining (configurable per-level via LevelDataSO),
    /// triggers the sequential PrimeTween star pop-in animation, saves progress via LevelManager,
    /// and provides navigation callbacks.
    ///
    /// Star thresholds are read from LevelDataSO (timeFor3Stars / timeFor2Stars).
    /// Template users can modify CalculateStars() to evaluate by Score, Moves, etc.
    /// </summary>
    public class WinPanelManager : MonoBehaviour
    {
        [Header("Stars")]
        [Tooltip("Assign exactly 3 star GameObjects in order (star 1, 2, 3).")]
        [SerializeField] private GameObject[] starObjects;
        [Tooltip("Optional. Sprite swapped onto stars that were not earned (e.g. a gray star).")]
        [SerializeField] private Sprite unearnedStarSprite;

        [Header("Rewards")]
        [Tooltip("Shows the coins granted for this level (\"+60\"). The coin burst starts here.")]
        [SerializeField] private TextMeshProUGUI coinRewardText;

        [Header("Animation")]
        [SerializeField] private float starPopDuration = 0.3f;
        [SerializeField] private float starPopDelay    = 0.25f;
        [SerializeField] private float starOvershoot   = 1.25f;

        private Sprite _earnedStarSprite;
        private CurrencyGrantedEvent? _pendingCoinEvent;
        private int _nextLevelIndex = -1; // the level after the one just won; -1 = saved progress

        private void LateUpdate()
        {
            if (_pendingCoinEvent is not { } evt) return;
            _pendingCoinEvent = null;
            EventBus.Publish(evt);
        }

        private void OnEnable()
        {
            // Hide all stars immediately when panel opens (before animating)
            ResetStars();

            // Calculate stars from remaining time
            int stars = CalculateStars();

            // Reward coins: Base coins from LevelData + Bonus per star
            var levelData = LevelManager.Instance ? LevelManager.Instance.CurrentLevelData : null;
            int coinsEarned = 50;

            if (levelData)
            {
                if (levelData.rewardMode == LevelDataSO.RewardCalculationMode.FixedValue)
                {
                    coinsEarned = levelData.baseCoinReward;
                }
                else
                {
                    coinsEarned = levelData.baseCoinReward + (stars * levelData.coinsPerStar);
                }
            }
            else
            {
                // Fallback
                coinsEarned = 50 + (stars * 10);
            }
            
            SaveManager.AddCoins(coinsEarned);
            float coinDelay = starPopDelay * stars + 0.6f; // coins burst after the stars; CurrencyFlyEffect plays the clinks

            // Show the amount that was just granted, and burst the coins from it to the counter.
            Vector2 burstFrom = CurrencyGrantedEvent.ScreenCenter;
            if (coinRewardText)
            {
                coinRewardText.text = $"+{coinsEarned:N0}";
                var canvas = coinRewardText.canvas;
                Camera cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                burstFrom = RectTransformUtility.WorldToScreenPoint(cam, coinRewardText.transform.position);
            }
            // Published in LateUpdate: the panel's own CoinPill (a child) is enabled after this OnEnable and
            // would miss the event. LateUpdate is still before this frame renders, so the counter never flickers.
            _pendingCoinEvent = new CurrencyGrantedEvent(ECurrency.Coins, coinsEarned, burstFrom, coinDelay);

            // Next Level plays the level after this one (also when replaying an old level).
            // Read before SaveLevelComplete, which moves CurrentLevelIndex. Last level → saved progress.
            var lm = LevelManager.Instance;
            _nextLevelIndex = lm && lm.CurrentLevelIndex + 1 < lm.TotalLevelCount ? lm.CurrentLevelIndex + 1 : -1;

            // Save progress (advances level index if this was a new level)
            LevelManager.Instance?.SaveLevelComplete(stars);

            // Animate stars in after a short opening delay
            AnimateStars(stars);
        }

        // ── Star Rating ──────────────────────────────────────────────────────

        private static int CalculateStars()
        {
            if (!LevelManager.Instance) return 1;

            var levelData = LevelManager.Instance.CurrentLevelData;
            int remaining = TimerManager.Instance ? TimerManager.Instance.CurrentTime : 0;

            if (levelData)
            {
                // Note for Template Users:
                // You can modify this method to evaluate stars based on Score, Moves, etc.
                // Currently, it evaluates based on time remaining specified in LevelDataSO.
                if (remaining >= levelData.timeFor3Stars) return 3;
                if (remaining >= levelData.timeFor2Stars) return 2;
                return 1;
            }

            // Fallback
            int total = LevelManager.Instance.TotalLevelDuration;
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

                // Restore the earned look first; AnimateStars swaps unearned stars afterward.
                if (unearnedStarSprite && star.TryGetComponent(out Image img))
                {
                    _earnedStarSprite ??= img.sprite;
                    img.sprite = _earnedStarSprite;
                }
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
                    if (unearnedStarSprite && star.TryGetComponent(out Image unearnedImg))
                        unearnedImg.sprite = unearnedStarSprite;

                    // Unearned stars stay small / greyed out — just show them at scale 0.6
                    float delay = starPopDelay * i + 0.2f;
                    Tween.Scale(star.transform,
                        Vector3.one * 0.6f,
                        starPopDuration * 0.5f,
                        Ease.OutBack,
                        startDelay: delay);
                    continue;
                }

                // Earned star: pop in with overshoot bounce
                var popDelay = starPopDelay * i + 0.2f;
                Tween.Delay(popDelay, () => SoundManager.Sfx(SoundEvent.StarPop));
                Tween.Scale(star.transform,
                        Vector3.one * starOvershoot,
                        starPopDuration,
                        Ease.OutBack,
                        startDelay: popDelay)
                         .OnComplete(() =>
                         {
                             Tween.Scale(star.transform,
                                 Vector3.one,
                                 starPopDuration * 0.4f,
                                 Ease.InOutSine);
                         });
            }
        }

        // ── Button Callbacks ─────────────────────────────────────────────────

        /// <summary>Called by the Next Level button.</summary>
        public void OnNextLevelClicked()
        {
            int next = _nextLevelIndex;
            if (Utilities.AdManager.Instance) Utilities.AdManager.Instance.ShowInterstitialThen(() => SceneLoader.LoadLevel(next));
            else SceneLoader.LoadLevel(next);
        }

        /// <summary>Called by the Level Select button.</summary>
        public void OnLevelSelectClicked() => SceneLoader.Load(SceneLoader.LevelSelect);
    }
}
