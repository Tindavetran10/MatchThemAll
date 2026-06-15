using System;
using UnityEngine;
using MatchThemAll.Scripts.SaveSystem;

// For EPowerupType

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
            PlayerData data = SaveManager.Load();
            string lastPlayedStr = data.lastPlayedDate;
            DateTime currentDate = DateTime.Now.Date;

            if (string.IsNullOrEmpty(lastPlayedStr))
            {
                // First time ever playing
                data.loginStreak = 1;
                data.lastPlayedDate = currentDate.ToString("O");
                SaveManager.Save(data);
                ShowRewardPopup(data.loginStreak);
                return;
            }

            if (DateTime.TryParse(lastPlayedStr, out DateTime lastPlayedDate))
            {
                int daysDifference = (int)(currentDate - lastPlayedDate.Date).TotalDays;

                if (daysDifference == 0)
                {
                    // Already played today, no reward.
                    return;
                }
                else if (daysDifference == 1)
                {
                    // Played exactly yesterday, increment streak
                    data.loginStreak++;
                }
                else if (daysDifference > 1)
                {
                    // Missed a day or more, reset streak
                    data.loginStreak = 1;
                }
                
                // Cap streak logic (loop back or cap at 7)
                if (data.loginStreak > 7)
                {
                    data.loginStreak = 1; // Loop back to 1
                }

                data.lastPlayedDate = currentDate.ToString("O");
                SaveManager.Save(data);

                ShowRewardPopup(data.loginStreak);
            }
        }

        public void ShowRewardPanelManual()
        {
            PlayerData data = SaveManager.Load();
            ShowRewardPopup(Mathf.Clamp(data.loginStreak, 1, 7));
        }

        private void ShowRewardPopup(int streakDay)
        {
            if (rewardPanelPrefab != null && uiCanvas != null)
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
            PlayerData data = SaveManager.Load();
            
            switch (streakDay)
            {
                case 1:
                    data.coins += 10;
                    break;
                case 2:
                    data.coins += 25;
                    break;
                case 3:
                    data.coins += 50;
                    data.vacuumCount += 1; // Assuming Vacuum is Hint/First powerup
                    break;
                case 4:
                    data.coins += 75;
                    break;
                case 5:
                    data.coins += 100;
                    data.springCount += 1; // Assuming Spring is Shuffle
                    break;
                case 6:
                    data.coins += 150;
                    break;
                case 7:
                    data.coins += 300;
                    data.vacuumCount += 1;
                    data.springCount += 1;
                    data.fanCount += 1;
                    data.freezeCount += 1;
                    break;
            }

            SaveManager.Save(data);
            Debug.Log($"[DailyReward] Granted Day {streakDay} reward!");
        }
    }
}
