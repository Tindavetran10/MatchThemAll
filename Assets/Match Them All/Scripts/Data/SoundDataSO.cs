using UnityEngine;
using UnityEngine.Audio;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Data asset for a single sound. Holds the clip and all playback settings.
    /// Create via: Right-click in Project → Create → Match Them All → Sound Data
    ///
    /// Usage from any script:
    ///   [SerializeField] private SoundDataSO mergeSound;
    ///   SoundManager.Instance.Play(mergeSound);
    /// </summary>
    [CreateAssetMenu(fileName = "SoundData", menuName = "Match Them All/Sound Data")]
    public class SoundDataSO : ScriptableObject
    {
        [Header("Audio")]
        [SerializeField] public AudioClip clip;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)]   public float volume = 1f;
        [SerializeField, Range(0.5f, 2f)] public float pitch  = 1f;
        [SerializeField]                  public bool  loop   = false;

        [Header("Mixer")]
        [Tooltip("Route to a specific AudioMixer group (SFX, Music, UI). Leave empty to bypass the mixer.")]
        [SerializeField] public AudioMixerGroup mixerGroup;
    }
}
