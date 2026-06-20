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
            if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (hapticsToggle) hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
        }

        private void OnEnable()
        {
            // Populate controls with saved values when the panel opens
            var (musicVolume, sfxVolume, hapticsEnabled) = SaveManager.GetSettings();
            if (musicSlider) musicSlider.value  = musicVolume;
            if (sfxSlider) sfxSlider.value    = sfxVolume;
            if (hapticsToggle) hapticsToggle.isOn = hapticsEnabled;
        }

        // ── Callbacks (assign in Inspector via UnityEvent) ──────────────────

        public void OnMusicVolumeChanged(float value)
        {
            SoundManager.Instance?.SetMusicVolume(value);
            SaveManager.SaveMusicVolume(value);
        }

        private static void OnSfxVolumeChanged(float value)
        {
            SoundManager.Instance?.SetSFXVolume(value);
            SaveManager.SaveSfxVolume(value);
        }

        public void OnHapticsChanged(bool enabled)
        {
            // Stub: wire to your haptics library when you add one
            SaveManager.SaveHaptics(enabled);
        }

        // ── Static helper ────────────────────────────────────────────────────

        /// <summary>
        /// Reads saved settings and applies them to SoundManager.
        /// Call this on scene load (e.g., from MainMenuManager.Start).
        /// </summary>
        public static void ApplyFromSave()
        {
            var (musicVolume, sfxVolume, _) = SaveManager.GetSettings();
            SoundManager.Instance?.SetMusicVolume(musicVolume);
            SoundManager.Instance?.SetSFXVolume(sfxVolume);
        }
    }
}
