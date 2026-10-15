using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Maps every <see cref="SoundEvent"/> to its clip(s) and playback settings.
    /// Edited through Match Them All → Sound Manager; loaded from Resources/Sound/SoundLibrary.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Match Them All/Sound Library")]
    public class SoundLibrarySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SoundEvent id;
            [Tooltip("One is picked at random each time, so several clips add variety.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Pitch is randomised between min and max on every play. Equal values = fixed pitch.")]
            [Range(0.5f, 2f)] public float pitchMin = 1f;
            [Range(0.5f, 2f)] public float pitchMax = 1f;

            public bool HasClip
            {
                get { foreach (var c in clips) if (c) return true; return false; }
            }

            public AudioClip PickClip()
            {
                if (clips == null || clips.Length == 0) return null;
                var c = clips[clips.Length == 1 ? 0 : UnityEngine.Random.Range(0, clips.Length)];
                return c ? c : Array.Find(clips, x => x);
            }
        }

        [Header("Mixer routing")]
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup musicGroup;

        public List<Entry> entries = new();

        private Dictionary<SoundEvent, Entry> _map;

        public bool TryGet(SoundEvent id, out Entry entry)
        {
            if (_map == null)
            {
                _map = new Dictionary<SoundEvent, Entry>();
                foreach (var e in entries) _map[e.id] = e;
            }
            return _map.TryGetValue(id, out entry);
        }

        private void OnValidate() => _map = null;
    }
}
