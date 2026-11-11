using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Plays every game sound, driven by <see cref="SoundEvent"/> ids mapped to clips in the
    /// SoundLibrary (edit it in Match Them All ▸ Sound Manager). One instance lives across scenes.
    ///
    ///   SoundManager.Sfx(SoundEvent.Merge);                 // one-shot, safe even with no manager
    ///   SoundManager.Instance.PlayMusic(SoundEvent.MusicGame);
    ///   SoundManager.Instance.SetSFXVolume(0.8f);           // 0..1, used by the Settings screen
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Mixer")]
        [Tooltip("AudioMixer exposing the SFXVolume and MusicVolume parameters.")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Sound Library")]
        [Tooltip("Event → clip map edited in Match Them All → Sound Manager. Loaded from Resources/Sound/SoundLibrary if empty.")]
        [SerializeField] private SoundLibrarySO library;

        [Header("Voices")]
        [Tooltip("How many one-shots can overlap. When all are busy the longest-running one is cut.")]
        [SerializeField, Range(1, 16)] private int sfxPoolSize = 8;

        private const string SfxVolumeParam = "SFXVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const float MinRepeatInterval = 0.04f; // collapses same-frame duplicates (e.g. a 3-item merge)
        private const float SilentDb = -80f;

        private AudioSource[] _voices;
        private float[] _voiceStartedAt;
        private AudioSource _music;
        private SoundEvent _musicId;
        private readonly Dictionary<SoundEvent, float> _lastPlayed = new();

        #region Lifecycle

        /// <summary>Makes sure a SoundManager exists in every scene (Lobby, Main, ...) by spawning the Resources prefab.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance) return;
            var prefab = Resources.Load<GameObject>("Sound/SoundManager");
            if (prefab) Instantiate(prefab);
            else new GameObject("[SoundManager]").AddComponent<SoundManager>();
        }

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null); // DontDestroyOnLoad needs a root object
            DontDestroyOnLoad(gameObject);

            if (!library) library = Resources.Load<SoundLibrarySO>("Sound/SoundLibrary");
            CreateSources();

            EventBus.Subscribe<ItemClickedEvent>(OnItemClicked);
            EventBus.Subscribe<ItemReachedSpotEvent>(OnItemReachedSpot);
            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<ShopPurchaseSucceededEvent>(OnPurchase);
            ComboManager.OnComboUpdated += OnComboUpdated;
            SceneManager.sceneLoaded += OnSceneLoaded;

            UI.SettingsManager.ApplyFromSave(); // saved volumes, whichever scene we start in
        }

        // Starting music inside Awake is unreliable (the source may not be ready yet), so wait for the first frame.
        private void Start() { if (Instance == this) PlaySceneMusic(SceneManager.GetActiveScene().name); }

        // Keeps the playing track at its library volume, so Sound Manager edits are heard live in Play mode.
        private void Update()
        {
            if (_music && _music.isPlaying && library && library.TryGet(_musicId, out var entry)) _music.volume = entry.volume;
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

        private void CreateSources()
        {
            _voices = new AudioSource[sfxPoolSize];
            _voiceStartedAt = new float[sfxPoolSize];
            for (var i = 0; i < _voices.Length; i++) _voices[i] = NewSource(loop: false);
            _music = NewSource(loop: true);
        }

        private AudioSource NewSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }

        #endregion

        #region Game events → sounds

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

        #endregion

        #region Playback

        /// <summary>Plays the sound assigned to <paramref name="id"/> in the SoundLibrary. Safe to call when no manager/clip exists.</summary>
        public static void Sfx(SoundEvent id)
        {
            if (Instance) Instance.PlayEvent(id);
        }

        /// <summary>Plays <paramref name="clip"/> on the SFX group (power-ups carry their own clip). Safe when no manager/clip exists.</summary>
        public static void Sfx(AudioClip clip, float volume, float pitch)
        {
            if (Instance && clip) Instance.PlayClip(clip, volume, pitch);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            int v = PickVoice();
            var source = _voices[v];
            _voiceStartedAt[v] = Time.unscaledTime;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.outputAudioMixerGroup = library ? library.sfxGroup : null;
            source.Play();
        }

        private void PlayEvent(SoundEvent id)
        {
            if (!library || !library.TryGet(id, out var entry)) return;
            var clip = entry.PickClip();
            if (!clip) return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out var last) && now - last < MinRepeatInterval) return;
            _lastPlayed[id] = now;

            int v = PickVoice();
            var source = _voices[v];
            _voiceStartedAt[v] = now;
            source.clip = clip;
            source.volume = entry.volume;
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            source.outputAudioMixerGroup = library.sfxGroup;
            source.Play();
        }

        /// <summary>An idle voice if there is one, otherwise the one that started longest ago.</summary>
        private int PickVoice()
        {
            int oldest = 0;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) return i;
                if (_voiceStartedAt[i] < _voiceStartedAt[oldest]) oldest = i;
            }
            return oldest;
        }

        /// <summary>Starts a looping music event (MusicMenu / MusicGame). Keeps playing if the clip is unchanged.</summary>
        public void PlayMusic(SoundEvent id)
        {
            if (!library || !library.TryGet(id, out var entry)) return;
            var clip = entry.PickClip();
            if (!clip || (_music.clip == clip && _music.isPlaying)) return;

            _musicId = id;
            _music.clip = clip;
            _music.volume = entry.volume;
            _music.pitch = 1f;
            _music.outputAudioMixerGroup = library.musicGroup;
            _music.Play();
        }

        #endregion

        #region Volume

        /// <summary>SFX bus volume, 0 (mute) to 1 (full).</summary>
        public void SetSFXVolume(float volume01) => SetBus(SfxVolumeParam, volume01);

        /// <summary>Music bus volume, 0 (mute) to 1 (full).</summary>
        public void SetMusicVolume(float volume01) => SetBus(MusicVolumeParam, volume01);

        // The mixer works in decibels; 20·log10(v) makes the slider feel even. 0 maps to the mixer's floor.
        private void SetBus(string parameter, float volume01)
        {
            if (!audioMixer) return;
            float db = volume01 > 0.0001f ? 20f * Mathf.Log10(volume01) : SilentDb;
            audioMixer.SetFloat(parameter, db);
        }

        #endregion
    }
}
