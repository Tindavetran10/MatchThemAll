using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using MatchThemAll.Scripts.Shop;

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

        [Header("Sound Library")]
        [Tooltip("Event → clip map edited in Match Them All → Sound Manager. Loaded from Resources/Sound/SoundLibrary if empty.")]
        [SerializeField] private SoundLibrarySO library;

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

                if (!library) library = Resources.Load<SoundLibrarySO>("Sound/SoundLibrary");
                SubscribeToGameEvents();
                UI.SettingsManager.ApplyFromSave();          // saved volumes, whichever scene we start in
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else Destroy(gameObject);
        }

        // Starting music inside Awake is unreliable (the source may not be ready yet), so wait for the first frame.
        private void Start() { if (Instance == this) PlaySceneMusic(SceneManager.GetActiveScene().name); }

        /// <summary>Makes sure a SoundManager exists in every scene (Lobby, Main, ...) by spawning the Resources prefab.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance) return;
            var prefab = Resources.Load<GameObject>("Sound/SoundManager");
            if (prefab) Instantiate(prefab);
            else new GameObject("[SoundManager]").AddComponent<SoundManager>();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus.Unsubscribe<ItemClickedEvent>(OnItemClicked);
            EventBus.Unsubscribe<ItemReachedSpotEvent>(OnItemReachedSpot);
            EventBus.Unsubscribe<MergeStartedEvent>(OnMergeStarted);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<ShopPurchaseSucceededEvent>(OnPurchase);
            ComboManager.OnComboUpdated -= OnComboUpdated;
        }

        // ------------------------------------------------------------------ Game event → sound

        private void SubscribeToGameEvents()
        {
            EventBus.Subscribe<ItemClickedEvent>(OnItemClicked);
            EventBus.Subscribe<ItemReachedSpotEvent>(OnItemReachedSpot);
            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<ShopPurchaseSucceededEvent>(OnPurchase);
            ComboManager.OnComboUpdated += OnComboUpdated;
        }

        private void OnItemClicked(ItemClickedEvent _) => Sfx(SoundEvent.ItemSelect);
        private void OnItemReachedSpot(ItemReachedSpotEvent _) => Sfx(SoundEvent.ItemPlace);
        private void OnMergeStarted(MergeStartedEvent _) => Sfx(SoundEvent.Merge);
        private void OnPurchase(ShopPurchaseSucceededEvent _) => Sfx(SoundEvent.PurchaseSuccess);
        private void OnComboUpdated(int combo) { if (combo > 1) Sfx(SoundEvent.Combo); }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case EGameState.LEVELCOMPLETE: Sfx(SoundEvent.LevelComplete); break;
                case EGameState.GAMEOVER:      Sfx(SoundEvent.GameOver); break;
                case EGameState.OUTOFTIME:     Sfx(SoundEvent.OutOfTime); break;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _) => PlaySceneMusic(scene.name);

        /// <summary>Gameplay music in the game scene, menu music in the menus. Loading/intro scenes keep whatever is playing.</summary>
        private void PlaySceneMusic(string sceneName)
        {
            if (sceneName == SceneLoader.Game) PlayMusic(SoundEvent.MusicGame);
            else if (sceneName == SceneLoader.Lobby || sceneName == SceneLoader.LevelSelect) PlayMusic(SoundEvent.MusicMenu);
        }

        // ------------------------------------------------------------------ Event API

        private readonly System.Collections.Generic.Dictionary<SoundEvent, float> _lastPlayed = new();
        private const float MinRepeatInterval = 0.04f; // collapses same-frame duplicates (e.g. a 3-item merge)

        /// <summary>Plays the sound assigned to <paramref name="id"/> in the SoundLibrary. Safe to call when no manager/clip exists.</summary>
        public static void Sfx(SoundEvent id)
        {
            if (Instance) Instance.PlayEvent(id);
        }

        private void PlayEvent(SoundEvent id)
        {
            if (!library || !library.TryGet(id, out var entry)) return;
            var clip = entry.PickClip();
            if (!clip) return;

            if (_lastPlayed.TryGetValue(id, out var last) && Time.unscaledTime - last < MinRepeatInterval) return;
            _lastPlayed[id] = Time.unscaledTime;

            var source = GetFreeSource();
            source.clip = clip;
            source.volume = entry.volume;
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            source.loop = false;
            source.outputAudioMixerGroup = library.sfxGroup;
            source.Play();
        }

        /// <summary>Starts a looping music event (MusicMenu / MusicGame). Keeps playing if the clip is unchanged.</summary>
        public void PlayMusic(SoundEvent id)
        {
            if (!library || !library.TryGet(id, out var entry)) return;
            var clip = entry.PickClip();
            if (!clip || (_musicSource.clip == clip && _musicSource.isPlaying)) return;

            _musicSource.clip = clip;
            _musicSource.volume = entry.volume;
            _musicSource.pitch = 1f;
            _musicSource.outputAudioMixerGroup = library.musicGroup;
            _musicSource.Play();
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
