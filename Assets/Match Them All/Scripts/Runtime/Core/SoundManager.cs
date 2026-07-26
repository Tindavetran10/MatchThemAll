using UnityEngine;
using UnityEngine.Audio;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Global singleton that plays all game audio.
    /// Persists across scene loads (DontDestroyOnLoad).
    ///
    /// Playing a sound from any script:
    ///   SoundManager.Instance.Play(mySoundDataSO);
    ///
    /// Playing background music:
    ///   SoundManager.Instance.PlayMusic(myMusicDataSO);
    ///
    /// Adjusting volume (e.g. from a Settings screen):
    ///   SoundManager.Instance.SetSFXVolume(0.8f);   // 0-1 normalized
    ///   SoundManager.Instance.SetMusicVolume(0.5f);
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Mixer")]
        [Tooltip("Assign your project's AudioMixer asset here.")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Pool")]
        [Tooltip("How many SFX can play simultaneously. 8 is sufficient for most mobile games.")]
        [SerializeField, Range(1, 16)] private int sfxPoolSize = 8;

        // Exposed parameter names — must match what you expose in the AudioMixer asset.
        private const string MasterVolumeParam = "MasterVolume";
        private const string SfxVolumeParam    = "SFXVolume";
        private const string MusicVolumeParam   = "MusicVolume";

        private AudioSource[] _sfxPool;
        private AudioSource   _musicSource;
        private int           _poolCursor; // round-robin index for stealing busy sources

        // ------------------------------------------------------------------ Lifecycle

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                transform.SetParent(null); // Detach from parent to ensure it's a root GameObject for DontDestroyOnLoad
                DontDestroyOnLoad(gameObject);
                BuildPool();
            }
            else Destroy(gameObject);
        }

        /// <summary>Creates the AudioSource components that act as the SFX pool.</summary>
        private void BuildPool()
        {
            _sfxPool = new AudioSource[sfxPoolSize];
            for (var i = 0; i < sfxPoolSize; i++)
            {
                _sfxPool[i]            = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }

            _musicSource            = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop        = true;
        }

        // ------------------------------------------------------------------ SFX

        /// <summary>
        /// Plays a one-shot (or looping) sound effect.
        /// Finds a free AudioSource from the pool; if all are busy, steals the oldest.
        /// </summary>
        public void Play(SoundDataSO data)
        {
            if (!data || !data.clip)
            {
                Debug.LogWarning("SoundManager.Play: SoundDataSO or its clip is null.");
                return;
            }

            var source                = GetFreeSource();
            source.clip                       = data.clip;
            source.volume                     = data.volume;
            source.pitch                      = data.pitch;
            source.loop                       = data.loop;
            source.outputAudioMixerGroup      = data.mixerGroup;
            source.Play();
        }

        /// <summary>
        /// Returns the first idle AudioSource in the pool.
        /// Falls back to round-robin stealing when all sources are busy.
        /// </summary>
        private AudioSource GetFreeSource()
        {
            foreach (var sfx in _sfxPool)
                if (!sfx.isPlaying)
                    return sfx;

            // All busy — steal the next source in order and restart it
            var stolen = _sfxPool[_poolCursor % _sfxPool.Length];
            _poolCursor++;
            return stolen;
        }

        // ------------------------------------------------------------------ Music

        /// <summary>Starts background music. Ignored if the same clip is already playing.</summary>
        public void PlayMusic(SoundDataSO data)
        {
            if (!data || !data.clip) return;

            // Avoid restarting the same track if it is already running
            if (_musicSource.clip == data.clip && _musicSource.isPlaying) return;

            _musicSource.clip                  = data.clip;
            _musicSource.volume                = data.volume;
            _musicSource.pitch                 = data.pitch;
            _musicSource.outputAudioMixerGroup = data.mixerGroup;
            _musicSource.Play();
        }

        /// <summary>Stops background music immediately.</summary>
        public void StopMusic() => _musicSource.Stop();

        // ------------------------------------------------------------------ Volume control

        /// <summary>Sets master volume. normalizedValue is 0 (mute) to 1 (full).</summary>
        public void SetMasterVolume(float normalizedValue) =>
            SetMixerVolume(MasterVolumeParam, normalizedValue);

        /// <summary>Sets SFX bus volume. normalizedValue is 0 to 1.</summary>
        public void SetSFXVolume(float normalizedValue) =>
            SetMixerVolume(SfxVolumeParam, normalizedValue);

        /// <summary>Sets music bus volume. normalizedValue is 0 to 1.</summary>
        public void SetMusicVolume(float normalizedValue) =>
            SetMixerVolume(MusicVolumeParam, normalizedValue);

        /// <summary>
        /// Converts a 0-1 linear slider value to dB and applies it to the AudioMixer.
        /// AudioMixer works in dB; using log10 gives a perceptually linear volume curve.
        /// </summary>
        private void SetMixerVolume(string parameter, float normalizedValue)
        {
            if (!audioMixer) return;

            // Clamp to avoid log10(0) = -Infinity
            var db = Mathf.Log10(Mathf.Max(0.0001f, normalizedValue)) * 20f;
            audioMixer.SetFloat(parameter, db);
        }
    }
}
