using System;
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;

// Dev-only (not exported): synthesizes every SFX as a WAV we own, then assigns it in the SoundLibrary.
// Tweak a preset, rerun the menu item; same seed -> same file.
static class SfxSynth
{
    const int Rate = 44100;
    const string OutDir = "Assets/MatchThemAllTemplate/Audio/Audio Clip";
    const string LibraryPath = "Assets/MatchThemAllTemplate/Resources/Sound/SoundLibrary.asset";

    enum Wave { Sine, Square, Triangle, Saw, Noise }

    // One tone: pitch glides f0 -> f1 over dur. Noise uses lp (0..1) as a one-pole low-pass amount.
    struct Note
    {
        public float at, dur, f0, f1, vol, attack, vib, vibHz, lp;
        public Wave wave;
    }

    static Note N(float at, float dur, float f0, float f1 = -1, Wave w = Wave.Sine, float vol = 0.5f,
        float attack = 0.004f, float vib = 0, float vibHz = 0, float lp = 1) =>
        new() { at = at, dur = dur, f0 = f0, f1 = f1 < 0 ? f0 : f1, wave = w, vol = vol, attack = attack, vib = vib, vibHz = vibHz, lp = lp };

    static float Hz(string note) // "C5", "F#4"
    {
        int i = Array.IndexOf(new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" }, note[..^1]);
        int midi = (note[^1] - '0' + 1) * 12 + i;
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    static Note[] Arp(float step, float dur, Wave w, float vol, params string[] notes)
    {
        var list = new Note[notes.Length];
        for (int i = 0; i < notes.Length; i++) list[i] = N(i * step, dur, Hz(notes[i]), -1, w, vol);
        return list;
    }

    static Note[] Cat(params Note[][] parts) { var l = new List<Note>(); foreach (var p in parts) l.AddRange(p); return l.ToArray(); }

    static readonly (SoundEvent id, string file, Note[] notes)[] Presets =
    {
        (SoundEvent.ButtonClick, "UI Click", new[] { N(0, .06f, 900, 1300, Wave.Triangle, .6f) }),
        (SoundEvent.ButtonClose, "UI Close", new[] { N(0, .09f, 750, 380, Wave.Triangle, .6f) }),
        (SoundEvent.Error, "UI Error", new[] { N(0, .12f, 220, 200, Wave.Square, .25f), N(.14f, .18f, 185, 160, Wave.Square, .25f) }),
        (SoundEvent.PurchaseSuccess, "UI Purchase", Arp(.06f, .22f, Wave.Triangle, .45f, "C6", "E6", "G6", "C7")),
        (SoundEvent.RewardClaim, "UI Reward", Cat(Arp(.05f, .3f, Wave.Sine, .4f, "G5", "B5", "D6", "G6", "B6"),
            new[] { N(.25f, .5f, 3000, 3000, Wave.Noise, .06f, .05f, lp: .5f) })),

        (SoundEvent.ItemSelect, "Item Select", new[] { N(0, .07f, 480, 950, Wave.Sine, .7f) }),
        (SoundEvent.ItemPlace, "Item Place", new[] { N(0, .1f, 320, 110, Wave.Sine, .8f), N(0, .03f, 0, 0, Wave.Noise, .15f, lp: .3f) }),
        (SoundEvent.Merge, "Merge", Cat(new[] { N(0, .18f, 0, 0, Wave.Noise, .2f, .1f, lp: .15f), N(0, .18f, 300, 900, Wave.Sine, .4f, .05f) },
            Arp(.03f, .3f, Wave.Triangle, .35f, "C6", "G6", "C7"))),
        (SoundEvent.Combo, "Combo", Arp(.05f, .12f, Wave.Square, .2f, "E6", "G6", "B6")),
        (SoundEvent.GoalComplete, "Goal Complete", new[] { N(0, .5f, Hz("E6"), -1, Wave.Sine, .45f), N(.1f, .6f, Hz("B6"), -1, Wave.Sine, .4f) }),
        (SoundEvent.PowerupVacuum, "Powerup Vacuum", new[] { N(0, .55f, 0, 0, Wave.Noise, .3f, .08f, lp: .25f), N(0, .55f, 900, 180, Wave.Sine, .35f, .05f) }),
        (SoundEvent.PowerupSpring, "Powerup Spring", new[] { N(0, .45f, 220, 620, Wave.Sine, .55f, vib: .12f, vibHz: 14) }),
        (SoundEvent.PowerupFan, "Powerup Fan", new[] { N(0, .6f, 0, 0, Wave.Noise, .35f, .15f, lp: .12f), N(0, .6f, 140, 160, Wave.Triangle, .15f, .1f, vib: .05f, vibHz: 30) }),
        (SoundEvent.PowerupFreeze, "Powerup Freeze", Cat(Arp(.04f, .45f, Wave.Sine, .25f, "B6", "F#7", "D#7", "A#7"),
            new[] { N(0, .6f, 0, 0, Wave.Noise, .05f, .02f, lp: .9f) })),
        (SoundEvent.StarPop, "Star Pop", new[] { N(0, .05f, 1200, 1900, Wave.Sine, .6f) }),
        (SoundEvent.CoinReward, "Coin", new[] { N(0, .07f, Hz("B5"), -1, Wave.Square, .25f), N(.07f, .3f, Hz("E6"), -1, Wave.Square, .25f) }),

        (SoundEvent.LevelComplete, "Level Complete", Cat(Arp(.12f, .14f, Wave.Square, .2f, "C5", "E5", "G5"),
            new[] { N(.36f, .8f, Hz("C6"), -1, Wave.Triangle, .4f), N(.36f, .8f, Hz("E5"), -1, Wave.Triangle, .3f), N(.36f, .8f, Hz("G5"), -1, Wave.Triangle, .3f) })),
        (SoundEvent.GameOver, "Game Over", Cat(Arp(.22f, .24f, Wave.Triangle, .4f, "G4", "F#4", "F4"),
            new[] { N(.66f, .9f, Hz("E4"), Hz("D4"), Wave.Triangle, .4f, vib: .01f, vibHz: 6) })),
        (SoundEvent.OutOfTime, "Out Of Time", new[] { N(0, .03f, 2000, 2000, Wave.Square, .2f), N(.25f, .03f, 2000, 2000, Wave.Square, .2f),
            N(.5f, .5f, 140, 110, Wave.Saw, .3f) }),
    };

    [MenuItem("Tools/AssetGen/Generate SFX")]
    static void Generate()
    {
        Directory.CreateDirectory(OutDir);
        var lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>(LibraryPath);
        var paths = new List<(SoundEvent, string)>();
        foreach (var (id, file, notes) in Presets)
        {
            var path = $"{OutDir}/{file}.wav";
            WriteWav(path, Render(notes, (int)id));
            paths.Add((id, path));
        }
        AssetDatabase.Refresh();

        Undo.RecordObject(lib, "Assign generated SFX");
        foreach (var (id, path) in paths)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var entry = lib.entries.Find(e => e.id == id);
            if (entry == null) lib.entries.Add(entry = new SoundLibrarySO.Entry { id = id });
            entry.clips = new[] { clip };
        }
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SfxSynth] Generated and assigned {paths.Count} clips.");
    }

    static float[] Render(Note[] notes, int seed)
    {
        float end = 0;
        foreach (var n in notes) end = Mathf.Max(end, n.at + n.dur);
        var buf = new float[(int)((end + .02f) * Rate)];
        var rng = new System.Random(seed);

        foreach (var n in notes)
        {
            int start = (int)(n.at * Rate), len = (int)(n.dur * Rate);
            double phase = 0; float lpState = 0;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = i / (float)len, sec = i / (float)Rate;
                float f = Mathf.Lerp(n.f0, n.f1, t) * (1 + n.vib * Mathf.Sin(2 * Mathf.PI * n.vibHz * sec));
                phase = (phase + f / Rate) % 1.0;
                float p = (float)phase, s = n.wave switch
                {
                    Wave.Sine => Mathf.Sin(2 * Mathf.PI * p),
                    Wave.Square => p < .5f ? 1 : -1,
                    Wave.Triangle => 4 * Mathf.Abs(p - .5f) - 1,
                    Wave.Saw => 2 * p - 1,
                    _ => lpState += n.lp * ((float)rng.NextDouble() * 2 - 1 - lpState),
                };
                // Linear attack, exponential-ish decay to silence so nothing clicks.
                float env = Mathf.Min(1, sec / Mathf.Max(n.attack, 1e-4f)) * Mathf.Pow(1 - t, 2);
                buf[start + i] += s * env * n.vol;
            }
        }

        float peak = 0;
        foreach (var v in buf) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > .95f) for (int i = 0; i < buf.Length; i++) buf[i] *= .95f / peak;
        return buf;
    }

    static void WriteWav(string path, float[] samples)
    {
        using var w = new BinaryWriter(File.Create(path));
        int bytes = samples.Length * 2;
        w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
        w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
        w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data".ToCharArray()); w.Write(bytes);
        foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1, 1) * short.MaxValue));
    }
}
