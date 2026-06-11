using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Manages the Settings panel UI. Reads saved settings on open and writes
    /// them back to disk whenever a slider or toggle changes.
    ///
    /// Wire up in Unity:
    ///   musicSlider.onValueChanged  -> OnMusicVolumeChanged
    ///   sfxSlider.onValueChanged    -> OnSfxVolumeChanged
    ///   hapticsToggle.onValueChanged -> OnHapticsChanged
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle hapticsToggle;

        private void Awake()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
        }

        private void OnEnable()
        {
            // Populate controls with saved values when the panel opens
            PlayerData data = SaveManager.Load();
            if (musicSlider  != null) musicSlider.value  = data.musicVolume;
            if (sfxSlider    != null) sfxSlider.value    = data.sfxVolume;
            if (hapticsToggle != null) hapticsToggle.isOn = data.hapticsEnabled;
        }

        // ── Callbacks (assign in Inspector via UnityEvent) ──────────────────

        public void OnMusicVolumeChanged(float value)
        {
            SoundManager.Instance?.SetMusicVolume(value);
            SaveSetting(data => data.musicVolume = value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            SoundManager.Instance?.SetSFXVolume(value);
            SaveSetting(data => data.sfxVolume = value);
        }

        public void OnHapticsChanged(bool enabled)
        {
            // Stub: wire to your haptics library when you add one
            SaveSetting(data => data.hapticsEnabled = enabled);
        }

        // ── Static helper ────────────────────────────────────────────────────

        /// <summary>
        /// Reads saved settings and applies them to SoundManager.
        /// Call this on scene load (e.g., from MainMenuManager.Start).
        /// </summary>
        public static void ApplyFromSave()
        {
            PlayerData data = SaveManager.Load();
            SoundManager.Instance?.SetMusicVolume(data.musicVolume);
            SoundManager.Instance?.SetSFXVolume(data.sfxVolume);
        }

        private static void SaveSetting(System.Action<PlayerData> mutate)
        {
            PlayerData data = SaveManager.Load();
            mutate(data);
            SaveManager.Save(data);
        }
    }
}
