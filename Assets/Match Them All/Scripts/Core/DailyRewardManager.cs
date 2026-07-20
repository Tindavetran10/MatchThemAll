using System;
using UnityEngine;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.UI
{
    public class DailyRewardManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private DailyRewardPanel rewardPanelPrefab;
        [SerializeField] private Transform uiCanvas;

        private void Start() => CheckDailyReward();

        private void CheckDailyReward()
        {
            var (lastPlayedStr, loginStreak) = SaveManager.GetDailyRewardData();
            DateTime currentDate = DateTime.Now.Date;

            if (string.IsNullOrEmpty(lastPlayedStr))
            {
                // First time ever playing
                SaveManager.SaveDailyRewardData(1, currentDate.ToString("O"));
                ShowRewardPopup(1);
                return;
            }

            if (DateTime.TryParse(lastPlayedStr, out DateTime lastPlayedDate))
            {
                int daysDifference = (int)(currentDate - lastPlayedDate.Date).TotalDays;

                switch (daysDifference)
                {
                    case 0:
                        // Already played today, no reward.
                        return;
                    case 1:
                        // Played exactly yesterday, increment streak
                        loginStreak++;
                        break;
                    case > 1:
                        // Missed a day or more, reset streak
                        loginStreak = 1;
                        break;
                }
                
                // Cap streak logic (loop back or cap at 7)
                if (loginStreak > 7)
                {
                    loginStreak = 1; // Loop back to 1
                }

                SaveManager.SaveDailyRewardData(loginStreak, currentDate.ToString("O"));
                ShowRewardPopup(loginStreak);
            }
        }

        public void ShowRewardPanelManual()
        {
            var (_, loginStreak) = SaveManager.GetDailyRewardData();
            ShowRewardPopup(Mathf.Clamp(loginStreak, 1, 7));
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
            switch (streakDay)
            {
                case 1:
                    SaveManager.AddCoins(10);
                    break;
                case 2:
                    SaveManager.AddCoins(25);
                    break;
                case 3:
                    SaveManager.AddCoins(50);
                    SaveManager.AddPowerupCharge("vacuum", 1);
                    break;
                case 4:
                    SaveManager.AddCoins(75);
                    break;
                case 5:
                    SaveManager.AddCoins(100);
                    SaveManager.AddPowerupCharge("spring", 1);
                    break;
                case 6:
                    SaveManager.AddCoins(150);
                    break;
                case 7:
                    SaveManager.AddCoins(300);
                    SaveManager.AddPowerupCharge("vacuum", 1);
                    SaveManager.AddPowerupCharge("spring", 1);
                    SaveManager.AddPowerupCharge("fan", 1);
                    SaveManager.AddPowerupCharge("freeze", 1);
                    break;
            }

            SaveManager.Flush(); // Daily reward = natural save point
            Debug.Log($"[DailyReward] Granted Day {streakDay} reward!");
        }
    }
}
