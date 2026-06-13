using System.IO;
using UnityEngine;

namespace MatchThemAll.Scripts.SaveSystem
{
    /// <summary>
    /// Static helper for reading and writing PlayerData to a JSON file on disk.
    ///
    /// Usage:
    ///   var data = SaveManager.Load();       // read (or get fresh defaults)
    ///   data.currentLevelIndex = 5;
    ///   SaveManager.Save(data);              // write to disk
    ///   SaveManager.Wipe();                  // delete save file (full reset)
    ///
    /// File location: Application.persistentDataPath/save.json
    ///   Android → /data/user/0/<package>/files/save.json
    ///   iOS     → <app>/Documents/save.json
    ///   Editor  → shown in console on first save
    /// </summary>
    public static class SaveManager
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        /// <summary>
        /// Loads PlayerData from disk. Returns a fresh default object if no save
        /// file exists yet (first launch) or if the file is corrupted.
        /// </summary>
        public static PlayerData Load()
        {
            if (!File.Exists(SavePath))
                return new PlayerData();

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
            }
            catch
            {
                Debug.LogWarning("[SaveManager] Save file corrupted — returning defaults.");
                return new PlayerData();
            }
        }

        /// <summary>
        /// Serializes PlayerData to JSON and writes it to disk.
        /// </summary>
        public static void Save(PlayerData data)
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
            Debug.Log($"[SaveManager] Saved to: {SavePath}");
#endif
        }

        /// <summary>
        /// Deletes the save file entirely. The next Load() call will return fresh defaults.
        /// </summary>
        public static void Wipe()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            Debug.Log("[SaveManager] Save file wiped.");
        }

        public static event System.Action<int> OnCoinsChanged;

        public static void AddCoins(int amount)
        {
            var data = Load();
            data.coins += amount;
            Save(data);
            OnCoinsChanged?.Invoke(data.coins);
        }

        public static bool SpendCoins(int amount)
        {
            var data = Load();
            if (data.coins >= amount)
            {
                data.coins -= amount;
                Save(data);
                OnCoinsChanged?.Invoke(data.coins);
                return true;
            }
            return false;
        }
    }
}
