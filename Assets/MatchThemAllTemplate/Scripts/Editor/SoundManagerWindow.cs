#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Sound Manager: assign, change and preview the clip(s) behind every <see cref="SoundEvent"/>.
    /// Same layout/palette as the Shop Manager and Template Editor.
    /// Open via: Match Them All → Sound Manager
    /// </summary>
    public class SoundManagerWindow : EditorWindow
    {
        private const string LibraryPath = "Assets/MatchThemAllTemplate/Resources/Sound/SoundLibrary.asset";
        private const string PrefabPath = "Assets/MatchThemAllTemplate/Resources/Sound/SoundManager.prefab";
        private const string MixerPath = "Assets/MatchThemAllTemplate/Audio/Mixer/GameAudioMixer.mixer";

        private static readonly Color PanelBg      = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg       = new(0.22f, 0.22f, 0.25f);
        private static readonly Color ToolbarBg    = new(0.14f, 0.14f, 0.16f);
        private static readonly Color AccentBlue   = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen  = new(0.26f, 0.83f, 0.53f);
        private static readonly Color AccentRed    = new(0.90f, 0.30f, 0.30f);
        private static readonly Color AccentOrange = new(1.00f, 0.65f, 0.20f);
        private static readonly Color DividerColor = new(0.12f, 0.12f, 0.14f);
        private static readonly Color TextMuted    = new(0.60f, 0.60f, 0.65f);

        private const float DividerGrabRange = 6f;
        private const float MinColumnWidth = 220f;

        private SoundLibrarySO _lib;
        private SerializedObject _so;
        private SoundEvent _selected = SoundEvent.None;
        private Vector2 _listScroll, _detailScroll;
        private bool _isDirty;
        private float _listWidth = -1f;
        private bool _draggingDivider;

        private bool _stylesInitialized;
        private readonly List<Texture2D> _ownedTextures = new();
        private GUIStyle _cardStyle, _headerStyle, _subHeaderStyle, _rowStyle, _selectedRowStyle, _emptyStyle, _descStyle;

        [MenuItem("Match Them All/Sound Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<SoundManagerWindow>("Sound Manager");
            w.minSize = new Vector2(780, 520);
            w.Load();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            Load();
        }

        private void OnDisable()
        {
            StopPreview();
            _stylesInitialized = false;
            foreach (var t in _ownedTextures) if (t) DestroyImmediate(t);
            _ownedTextures.Clear();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;
            foreach (var t in _ownedTextures) if (t) DestroyImmediate(t);
            _ownedTextures.Clear();

            var cardTex = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            cardTex.SetPixels(new[] { CardBg, CardBg, CardBg, CardBg });
            cardTex.Apply();
            _ownedTextures.Add(cardTex);

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = cardTex }
            };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, normal = { textColor = new Color(0.8f, 0.8f, 0.85f) } };
            _rowStyle = EditorWindowStyles.CardButton(false);
            _selectedRowStyle = EditorWindowStyles.CardButton(true);
            _rowStyle.richText = _selectedRowStyle.richText = true;
            _emptyStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = TextMuted } };
            _descStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, normal = { textColor = TextMuted } };
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Data
        // ═════════════════════════════════════════════════════════════════════

        private void Load()
        {
            StopPreview();
            _lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>(LibraryPath);
            _so = _lib ? new SerializedObject(_lib) : null;
            if (_lib && SyncEntries()) _isDirty = true;
            else _isDirty = false;
            if (_selected == SoundEvent.None) _selected = SoundEvent.ButtonClick;
            Repaint();
        }

        /// <summary>Adds an entry for every SoundEvent that doesn't have one (new enum values appear automatically).</summary>
        private bool SyncEntries()
        {
            bool added = false;
            foreach (SoundEvent e in System.Enum.GetValues(typeof(SoundEvent)))
            {
                if (e == SoundEvent.None || IndexOf(e) >= 0) continue;
                var entries = _so.FindProperty("entries");
                entries.arraySize++;
                var el = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                el.FindPropertyRelative("id").intValue = (int)e;
                el.FindPropertyRelative("clips").arraySize = 0;
                el.FindPropertyRelative("volume").floatValue = 1f;
                el.FindPropertyRelative("pitchMin").floatValue = 1f;
                el.FindPropertyRelative("pitchMax").floatValue = 1f;
                added = true;
            }
            if (added) { _so.ApplyModifiedProperties(); EditorUtility.SetDirty(_lib); }
            return added;
        }

        private int IndexOf(SoundEvent e)
        {
            var entries = _so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue == (int)e) return i;
            return -1;
        }

        private SerializedProperty Entry(SoundEvent e)
        {
            int i = IndexOf(e);
            return i < 0 ? null : _so.FindProperty("entries").GetArrayElementAtIndex(i);
        }

        private static string FirstClipName(SerializedProperty entry, out int count)
        {
            var clips = entry.FindPropertyRelative("clips");
            count = 0;
            string first = null;
            for (int i = 0; i < clips.arraySize; i++)
            {
                var c = clips.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!c) continue;
                count++;
                first ??= c.name;
            }
            return first;
        }

        private static string Pretty(SoundEvent e) => Regex.Replace(e.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

        private static string Category(SoundEvent e) => (int)e switch
        {
            < 10 => "UI",
            < 30 => "GAMEPLAY",
            < 40 => "RESULTS",
            _ => "MUSIC"
        };

        private static string Describe(SoundEvent e) => e switch
        {
            SoundEvent.ButtonClick => "Any button press (all buttons using UIButtonPunch).",
            SoundEvent.ButtonClose => "Close (X) buttons and the level-select back button.",
            SoundEvent.Error => "Locked level tapped, not enough coins, failed purchase.",
            SoundEvent.PurchaseSuccess => "A shop purchase completes.",
            SoundEvent.RewardClaim => "Claiming the daily reward.",
            SoundEvent.ItemSelect => "Tapping an item on the board.",
            SoundEvent.ItemPlace => "An item lands in a slot.",
            SoundEvent.Merge => "Three matching items merge.",
            SoundEvent.Combo => "A merge chains into a combo (x2 and up).",
            SoundEvent.GoalComplete => "A level goal is fully collected.",
            SoundEvent.PowerupVacuum => "Vacuum power-up activates.",
            SoundEvent.PowerupSpring => "Spring power-up activates.",
            SoundEvent.PowerupFan => "Fan power-up activates.",
            SoundEvent.PowerupFreeze => "Freeze power-up activates.",
            SoundEvent.StarPop => "Each star popping in on the Level Complete panel.",
            SoundEvent.CoinReward => "Coins awarded after a win.",
            SoundEvent.LevelComplete => "The level is won.",
            SoundEvent.GameOver => "The level is lost.",
            SoundEvent.OutOfTime => "The timer runs out (Continue panel).",
            SoundEvent.MusicMenu => "Loops in the Lobby and Level Select scenes.",
            SoundEvent.MusicGame => "Loops during gameplay. Use the same clip as menu music for an unbroken track.",
            _ => ""
        };

        // ═════════════════════════════════════════════════════════════════════
        //  Layout
        // ═════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            EnsureStyles();

            if (_lib == null) { DrawNoLibrary(); return; }

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);
            DrawToolbar();

            if (_listWidth < 0f) _listWidth = Mathf.Max(300f, position.width * 0.36f);
            float left = Mathf.Clamp(_listWidth, MinColumnWidth, position.width - MinColumnWidth);
            float right = position.width - left - 2;

            _so.Update();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(left));
            DrawList();
            GUILayout.EndVertical();
            DrawDivider();
            GUILayout.BeginVertical(GUILayout.Width(right));
            DrawDetail(right);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (_so.ApplyModifiedProperties()) { EditorUtility.SetDirty(_lib); _isDirty = true; }
        }

        private void DrawDivider()
        {
            Rect r = GUILayoutUtility.GetRect(2, 0, GUILayout.Width(2), GUILayout.ExpandHeight(true));
            var grab = new Rect(r.x - DividerGrabRange, r.y, DividerGrabRange * 2 + 2, r.height);
            bool hovering = grab.Contains(Event.current.mousePosition);
            EditorGUIUtility.AddCursorRect(grab, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(r, _draggingDivider || hovering ? AccentBlue : DividerColor);

            if (Event.current.type == EventType.MouseDown && hovering) { _draggingDivider = true; Event.current.Use(); }
            if (_draggingDivider && Event.current.type == EventType.MouseDrag)
            {
                _listWidth = Mathf.Clamp(Event.current.mousePosition.x, MinColumnWidth, position.width - MinColumnWidth);
                Event.current.Use();
                Repaint();
            }
            if (_draggingDivider && Event.current.type == EventType.MouseUp) { _draggingDivider = false; Event.current.Use(); }
        }

        private void DrawToolbar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), ToolbarBg);
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);
            GUI.color = AccentBlue;
            GUILayout.Label("🔊 Sound Manager", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            int missing = CountMissing();
            if (missing > 0)
            {
                GUI.color = AccentOrange;
                GUILayout.Label($"⚠ {missing} sound(s) without a clip", EditorStyles.boldLabel, GUILayout.Height(38));
                GUI.color = Color.white;
                GUILayout.Space(8);
            }

            if (_isDirty)
            {
                GUI.color = AccentOrange;
                GUILayout.Label("● Unsaved Changes", EditorStyles.boldLabel, GUILayout.Height(38));
                GUI.color = AccentGreen;
                if (GUILayout.Button("💾 Save", GUILayout.Height(28), GUILayout.Width(80)))
                {
                    AssetDatabase.SaveAssets();
                    _isDirty = false;
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
                GUILayout.Space(8);
            }

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button("↻ Reload", GUILayout.Height(28), GUILayout.Width(80))) { Load(); GUIUtility.ExitGUI(); }
            GUI.color = Color.white;
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        private int CountMissing()
        {
            int n = 0;
            foreach (SoundEvent e in System.Enum.GetValues(typeof(SoundEvent)))
            {
                var en = Entry(e);
                if (e == SoundEvent.None || en == null) continue;
                FirstClipName(en, out int c);
                if (c == 0) n++;
            }
            return n;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  List (left)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawList()
        {
            GUILayout.Space(10);
            _listScroll = GUILayout.BeginScrollView(_listScroll);

            string lastCategory = null;
            SoundEvent pick = SoundEvent.None;
            foreach (SoundEvent e in System.Enum.GetValues(typeof(SoundEvent)))
            {
                if (e == SoundEvent.None) continue;
                var en = Entry(e);
                if (en == null) continue;

                string cat = Category(e);
                if (cat != lastCategory)
                {
                    lastCategory = cat;
                    GUILayout.Space(6);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                    GUILayout.Label(cat, _subHeaderStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }

                string clipName = FirstClipName(en, out int count);
                string sub = count == 0 ? "<color=#E64D4D>No clip assigned</color>"
                           : count == 1 ? $"<color=#9999A6>{clipName}</color>"
                           : $"<color=#9999A6>{clipName}  +{count - 1} more</color>";

                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                if (GUILayout.Button($"{Pretty(e)}\n<size=10>{sub}</size>", e == _selected ? _selectedRowStyle : _rowStyle, GUILayout.Height(44)))
                    pick = e;

                GUI.enabled = count > 0;
                if (GUILayout.Button(new GUIContent("▶", "Preview"), GUILayout.Width(26), GUILayout.Height(44)))
                    Preview(en, e);
                GUI.enabled = true;
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            if (pick != SoundEvent.None)
            {
                _selected = pick;
                GUI.FocusControl(null);
                GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), DividerColor);
            GUI.color = TextMuted;
            GUILayout.Label($"  {System.Enum.GetValues(typeof(SoundEvent)).Length - 1} sound events", EditorStyles.miniLabel, GUILayout.Height(22));
            GUI.color = Color.white;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Detail (right)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawDetail(float width)
        {
            var en = Entry(_selected);
            EditorGUIUtility.labelWidth = Mathf.Clamp(width * 0.25f, 90f, 160f);

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Space(10);

            if (en != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(12);
                GUILayout.Label(Pretty(_selected), _headerStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(12);
                GUILayout.Label(Describe(_selected), _descStyle);
                GUILayout.Space(12);
                GUILayout.EndHorizontal();
                GUILayout.Space(6);

                Section("Clips");
                DrawClips(en);
                EndSection();

                Section("Playback");
                var vol = en.FindPropertyRelative("volume");
                vol.floatValue = EditorGUILayout.Slider("Volume", vol.floatValue, 0f, 1f);
                if (Category(_selected) != "MUSIC")
                {
                    var min = en.FindPropertyRelative("pitchMin");
                    var max = en.FindPropertyRelative("pitchMax");
                    float a = min.floatValue, b = max.floatValue;
                    EditorGUILayout.MinMaxSlider(new GUIContent("Pitch range"), ref a, ref b, 0.5f, 2f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth + 4);
                    a = EditorGUILayout.FloatField(Mathf.Round(a * 100f) / 100f, GUILayout.Width(60));
                    GUILayout.FlexibleSpace();
                    b = EditorGUILayout.FloatField(Mathf.Round(b * 100f) / 100f, GUILayout.Width(60));
                    GUILayout.EndHorizontal();
                    min.floatValue = Mathf.Clamp(Mathf.Min(a, b), 0.5f, 2f);
                    max.floatValue = Mathf.Clamp(Mathf.Max(a, b), 0.5f, 2f);
                    GUI.color = TextMuted;
                    GUILayout.Label("A random pitch in this range is used each play. Equal values = fixed pitch.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = TextMuted;
                    GUILayout.Label("Music loops. The first clip is used.", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                EndSection();
            }

            Section("Mixer Routing");
            EditorGUILayout.PropertyField(_so.FindProperty("sfxGroup"), new GUIContent("SFX group"));
            EditorGUILayout.PropertyField(_so.FindProperty("musicGroup"), new GUIContent("Music group"));
            EndSection();

            GUILayout.EndScrollView();
        }

        private void DrawClips(SerializedProperty en)
        {
            var clips = en.FindPropertyRelative("clips");
            int remove = -1;
            for (int i = 0; i < clips.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(clips.GetArrayElementAtIndex(i), GUIContent.none);
                var c = clips.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
                GUI.enabled = c;
                if (GUILayout.Button(new GUIContent("▶", "Preview this clip"), GUILayout.Width(26))) PlayClip(c, Category(_selected) == "MUSIC");
                GUI.enabled = true;
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(26))) remove = i;
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            if (remove >= 0)
            {
                // a non-null element must be nulled first, otherwise DeleteArrayElementAtIndex only clears it
                if (clips.GetArrayElementAtIndex(remove).objectReferenceValue) clips.GetArrayElementAtIndex(remove).objectReferenceValue = null;
                clips.DeleteArrayElementAtIndex(remove);
            }

            // Add by button or by dropping clips on the zone
            Rect drop = GUILayoutUtility.GetRect(0, 34, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(drop, new Color(0.16f, 0.16f, 0.19f));
            GUI.Label(drop, "＋ Drop audio clips here  (or click to add an empty slot)", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            var ev = Event.current;
            if (drop.Contains(ev.mousePosition))
            {
                if (ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform)
                {
                    bool ok = false;
                    foreach (var o in DragAndDrop.objectReferences) if (o is AudioClip) ok = true;
                    DragAndDrop.visualMode = ok ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    if (ev.type == EventType.DragPerform && ok)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var o in DragAndDrop.objectReferences)
                            if (o is AudioClip ac) { clips.arraySize++; clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = ac; }
                    }
                    ev.Use();
                }
                else if (ev.type == EventType.MouseDown)
                {
                    clips.arraySize++;
                    clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = null;
                    ev.Use();
                }
            }
        }

        private void Section(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(title.ToUpper(), _subHeaderStyle);
            GUILayout.Space(2);
        }

        private static void EndSection()
        {
            GUILayout.EndVertical();
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawNoLibrary()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("No SoundLibrary found.", _emptyStyle);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("Create default Sound Library", GUILayout.Width(220), GUILayout.Height(30)))
            {
                CreateDefaultLibrary();
                Load();
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Preview (UnityEditor.AudioUtil is internal, hence reflection)
        // ═════════════════════════════════════════════════════════════════════

        private static readonly System.Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        private static void Preview(SerializedProperty en, SoundEvent e)
        {
            var clips = en.FindPropertyRelative("clips");
            var list = new List<AudioClip>();
            for (int i = 0; i < clips.arraySize; i++)
                if (clips.GetArrayElementAtIndex(i).objectReferenceValue is AudioClip c) list.Add(c);
            if (list.Count > 0) PlayClip(list[Random.Range(0, list.Count)], Category(e) == "MUSIC");
        }

        private static void PlayClip(AudioClip clip, bool loop)
        {
            StopPreview();
            AudioUtil?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)?.Invoke(null, new object[] { clip, 0, loop });
        }

        private static void StopPreview() =>
            AudioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);

        // ═════════════════════════════════════════════════════════════════════
        //  First-time setup: library asset + SoundManager prefab with sensible defaults
        // ═════════════════════════════════════════════════════════════════════

        private const string Tpl = "Assets/MatchThemAllTemplate/Audio/Audio Clip/";
        private const string Music = "Assets/MatchThemAllTemplate/Audio/Music/";

        /// <summary>Creates Resources/Sound/SoundLibrary.asset (pre-filled from the bundled clips) and the SoundManager prefab.</summary>
        public static void CreateDefaultLibrary()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath)!);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

            var lib = AssetDatabase.LoadAssetAtPath<SoundLibrarySO>(LibraryPath);
            if (!lib)
            {
                lib = ScriptableObject.CreateInstance<SoundLibrarySO>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            if (mixer)
            {
                lib.sfxGroup = mixer.FindMatchingGroups("SFX").Length > 0 ? mixer.FindMatchingGroups("SFX")[0] : null;
                lib.musicGroup = mixer.FindMatchingGroups("Music").Length > 0 ? mixer.FindMatchingGroups("Music")[0] : null;
            }

            // event → (clip, volume, pitchMin, pitchMax)
            var defaults = new Dictionary<SoundEvent, (string clip, float vol, float p0, float p1)>
            {
                [SoundEvent.ButtonClick] = (Tpl + "UI Click.wav", 0.8f, 0.95f, 1.05f),
                [SoundEvent.ButtonClose] = (Tpl + "UI Close.wav", 0.8f, 1f, 1f),
                [SoundEvent.Error] = (Tpl + "UI Error.wav", 0.8f, 1f, 1f),
                [SoundEvent.PurchaseSuccess] = (Tpl + "UI Purchase.wav", 1f, 1f, 1f),
                [SoundEvent.RewardClaim] = (Tpl + "UI Reward.wav", 1f, 1f, 1f),
                [SoundEvent.ItemSelect] = (Tpl + "Item Select.wav", 0.7f, 0.95f, 1.1f),
                [SoundEvent.ItemPlace] = (Tpl + "Item Place.wav", 0.8f, 0.95f, 1.05f),
                [SoundEvent.Merge] = (Tpl + "Merge.wav", 1f, 1f, 1f),
                [SoundEvent.Combo] = (Tpl + "Combo.wav", 0.8f, 1.15f, 1.35f),
                [SoundEvent.GoalComplete] = (Tpl + "Goal Complete.wav", 1f, 1f, 1f),
                [SoundEvent.PowerupVacuum] = (Tpl + "Powerup Vacuum.wav", 0.9f, 0.9f, 0.9f),
                [SoundEvent.PowerupSpring] = (Tpl + "Powerup Spring.wav", 1f, 1.1f, 1.1f),
                [SoundEvent.PowerupFan] = (Tpl + "Powerup Fan.wav", 1f, 0.7f, 0.7f),
                [SoundEvent.PowerupFreeze] = (Tpl + "Powerup Freeze.wav", 1f, 0.75f, 0.75f),
                [SoundEvent.StarPop] = (Tpl + "Star Pop.wav", 0.9f, 1.1f, 1.4f),
                [SoundEvent.CoinReward] = (Tpl + "Coin.wav", 1f, 1f, 1f),
                [SoundEvent.LevelComplete] = (Tpl + "Level Complete.wav", 1f, 1f, 1f),
                [SoundEvent.GameOver] = (Tpl + "Game Over.wav", 1f, 1f, 1f),
                [SoundEvent.OutOfTime] = (Tpl + "Out Of Time.wav", 1f, 0.8f, 0.8f),
                [SoundEvent.MusicMenu] = (Music + "Music Menu - Flowerbed Fields.ogg", 0.5f, 1f, 1f),
                [SoundEvent.MusicGame] = (Music + "Music Game - Loop Town.ogg", 0.5f, 1f, 1f),
            };

            lib.entries ??= new List<SoundLibrarySO.Entry>();
            foreach (var kv in defaults)
            {
                if (lib.entries.Exists(x => x.id == kv.Key)) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(kv.Value.clip);
                lib.entries.Add(new SoundLibrarySO.Entry
                {
                    id = kv.Key,
                    clips = clip ? new[] { clip } : new AudioClip[0],
                    volume = kv.Value.vol,
                    pitchMin = kv.Value.p0,
                    pitchMax = kv.Value.p1,
                });
            }
            EditorUtility.SetDirty(lib);

            // Prefab spawned automatically in every scene (SoundManager.AutoCreate).
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            {
                var go = new GameObject("SoundManager");
                var sm = go.AddComponent<SoundManager>();
                var so = new SerializedObject(sm);
                so.FindProperty("audioMixer").objectReferenceValue = mixer;
                so.FindProperty("library").objectReferenceValue = lib;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
