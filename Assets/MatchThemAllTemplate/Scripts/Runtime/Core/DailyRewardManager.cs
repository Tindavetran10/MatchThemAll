using System;
using UnityEngine;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.UI
{
    public class DailyRewardManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private DailyRewardPanel rewardPanelPrefab;
        [SerializeField] private Transform uiCanvas;

        private void Start()
        {
            int day = UnclaimedStreakDay();
            if (day > 0) ShowRewardPopup(day);
        }

        /// <summary>
        /// The streak day (1-7) the player can claim today, or 0 if today's reward was already claimed.
        /// The saved date is the last CLAIM date (written by GrantReward), so closing the popup keeps the reward.
        /// </summary>
        private static int UnclaimedStreakDay()
        {
            var (lastClaimStr, loginStreak) = SaveManager.GetDailyRewardData();
            if (string.IsNullOrEmpty(lastClaimStr) || !DateTime.TryParse(lastClaimStr, out DateTime lastClaim))
                return 1; // First time ever playing

            int daysDifference = (int)(DateTime.Now.Date - lastClaim.Date).TotalDays;
            if (daysDifference <= 0) return 0; // Already claimed today

            // Claimed exactly yesterday: continue the streak. Missed a day or more: start over.
            int day = daysDifference == 1 ? loginStreak + 1 : 1;
            return day > 7 ? 1 : day; // Loop back to 1 after day 7
        }

        public void ShowRewardPanelManual()
        {
            int day = UnclaimedStreakDay();
            if (day > 0) { ShowRewardPopup(day); return; }

            // Already claimed today: show the calendar with Claim disabled.
            var (_, loginStreak) = SaveManager.GetDailyRewardData();
            if (rewardPanelPrefab && uiCanvas)
                Instantiate(rewardPanelPrefab, uiCanvas).Initialize(Mathf.Clamp(loginStreak, 1, 7), null);
        }

        private void ShowRewardPopup(int streakDay)
        {
            if (rewardPanelPrefab && uiCanvas)
            {
                DailyRewardPanel panel = Instantiate(rewardPanelPrefab, uiCanvas);
                panel.Initialize(streakDay, () => GrantReward(streakDay));
            }
            else
            {
                Debug.LogWarning("[DailyReward] Prefab or Canvas not assigned! Granting reward silently.");
                GrantReward(streakDay);
            }
        }

        private static void GrantReward(int streakDay)
        {
            if (UnclaimedStreakDay() == 0) return; // already claimed today (e.g. two panels open)
            SaveManager.SaveDailyRewardData(streakDay, DateTime.Now.Date.ToString("O"));

            int coins = 0;
            switch (streakDay)
            {
                case 1:
                    coins = 10;
                    break;
                case 2:
                    coins = 25;
                    break;
                case 3:
                    coins = 50;
                    SaveManager.AddPowerupCharge("vacuum", 1);
                    break;
                case 4:
                    coins = 75;
                    break;
                case 5:
                    coins = 100;
                    SaveManager.AddPowerupCharge("spring", 1);
                    break;
                case 6:
                    coins = 150;
                    break;
                case 7:
                    coins = 300;
                    SaveManager.AddPowerupCharge("vacuum", 1);
                    SaveManager.AddPowerupCharge("spring", 1);
                    SaveManager.AddPowerupCharge("fan", 1);
                    SaveManager.AddPowerupCharge("freeze", 1);
                    break;
            }

            SaveManager.AddCoins(coins);
            SaveManager.Flush(); // Daily reward = natural save point
            EventBus.Publish(new CurrencyGrantedEvent(ECurrency.Coins, coins, CurrencyGrantedEvent.ScreenCenter));
            Debug.Log($"[DailyReward] Granted Day {streakDay} reward!");
        }
    }
}
