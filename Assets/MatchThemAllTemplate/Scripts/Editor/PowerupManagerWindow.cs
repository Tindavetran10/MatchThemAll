#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using MatchThemAll.Scripts.Power_Ups;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Power-up Manager: create, edit, reorder and delete power-ups — icon, tray model, mechanic,
    /// unlock level, charges, price, VFX and sound — with a live 3D preview of the tray slot.
    /// The tray is spawned from this data at runtime (PowerupManager), so nothing here edits a scene.
    /// Same layout/palette as the Sound and Shop Managers.
    /// Open via: Match Them All → Power-up Manager
    /// </summary>
    public class PowerupManagerWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/MatchThemAllTemplate/Resources/Powerups/PowerupDatabase.asset";
        private const string AssetFolder = "Assets/MatchThemAllTemplate/Resources/Powerups";
        private const string SlotPrefabPath = "Assets/MatchThemAllTemplate/Prefabs/PowerUps/Slots/PowerupSlot.prefab";

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
        private const float MinColumnWidth = 240f;

        private PowerupDatabaseSO _db;
        private GameObject _genericSlot;
        private PowerupDataSO _selected;
        private SerializedObject _so;
        private readonly PowerupSlotPreview _preview = new();
        private Vector2 _listScroll, _detailScroll;
        private float _listWidth = -1f;
        private bool _draggingDivider, _isDirty;
        private string _newMechanicName = "";
        private bool _showNewMechanic;

        private bool _stylesInitialized;
        private readonly List<Texture2D> _ownedTextures = new();
        private GUIStyle _cardStyle, _headerStyle, _subHeaderStyle, _rowStyle, _selectedRowStyle, _emptyStyle, _descStyle;

        private static readonly Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        [MenuItem("Match Them All/Power-up Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<PowerupManagerWindow>("Power-up Manager");
            w.minSize = new Vector2(820, 560);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            Load();
        }

        private void OnDisable()
        {
            StopPreview();
            _preview.Dispose();
            _stylesInitialized = false;
            foreach (var t in _ownedTextures) if (t) DestroyImmediate(t);
            _ownedTextures.Clear();
        }

        private void Load()
        {
            _db = AssetDatabase.LoadAssetAtPath<PowerupDatabaseSO>(DatabasePath);
            _genericSlot = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            PowerupDataSO first = null;
            if (_db) foreach (var p in _db.Ordered) { first = p; break; }
            Select(_selected ? _selected : first);
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            var cardTex = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            cardTex.SetPixels(new[] { CardBg, CardBg, CardBg, CardBg });
            cardTex.Apply();
            _ownedTextures.Add(cardTex);

            _cardStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10), margin = new RectOffset(4, 4, 4, 4), normal = { background = cardTex } };
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, normal = { textColor = new Color(0.8f, 0.8f, 0.85f) } };
            _rowStyle = EditorWindowStyles.CardButton(false);
            _selectedRowStyle = EditorWindowStyles.CardButton(true);
            _rowStyle.richText = _selectedRowStyle.richText = true;
            _rowStyle.alignment = _selectedRowStyle.alignment = TextAnchor.MiddleLeft;
            _rowStyle.padding.left = _selectedRowStyle.padding.left = 48; // room for the icon
            _emptyStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = TextMuted } };
            _descStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, normal = { textColor = TextMuted } };
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Layout
        // ═════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            EnsureStyles();

            if (!_db) { DrawNoDatabase(); return; }

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);
            DrawToolbar();
            if (_showNewMechanic) DrawNewMechanicBar();

            if (_listWidth < 0f) _listWidth = Mathf.Max(280f, position.width * 0.32f);
            float left = Mathf.Clamp(_listWidth, MinColumnWidth, position.width - MinColumnWidth);
            float right = position.width - left - 2;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(left));
            DrawList();
            GUILayout.EndVertical();
            DrawDivider();
            GUILayout.BeginVertical(GUILayout.Width(right));
            DrawDetail(right);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
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
            GUILayout.Label("⚡ Power-up Manager", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            int problems = 0;
            foreach (var p in _db.powerups) if (p && Problem(p) != null) problems++;
            if (problems > 0)
            {
                GUI.color = AccentOrange;
                GUILayout.Label($"⚠ {problems} problem(s)", EditorStyles.boldLabel, GUILayout.Height(38));
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

            GUI.color = AccentGreen;
            if (GUILayout.Button("+ New Power-up", GUILayout.Height(28), GUILayout.Width(118))) { CreatePowerup(null); GUIUtility.ExitGUI(); }
            GUI.color = Color.white;
            GUI.enabled = _selected;
            if (GUILayout.Button("Duplicate", GUILayout.Height(28), GUILayout.Width(80))) { CreatePowerup(_selected); GUIUtility.ExitGUI(); }
            GUI.color = AccentRed;
            if (GUILayout.Button("Delete", GUILayout.Height(28), GUILayout.Width(64))) { DeletePowerup(_selected); GUIUtility.ExitGUI(); }
            GUI.color = Color.white;
            GUI.enabled = true;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button("New Mechanic…", GUILayout.Height(28), GUILayout.Width(110))) _showNewMechanic = !_showNewMechanic;
            GUI.color = Color.white;
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        private void DrawNewMechanicBar()
        {
            GUILayout.BeginHorizontal(_cardStyle);
            GUILayout.Label("New mechanic name", GUILayout.Width(130));
            _newMechanicName = EditorGUILayout.TextField(_newMechanicName);
            GUI.color = AccentGreen;
            if (GUILayout.Button("Create script", GUILayout.Width(100)))
            {
                string path = PowerupMechanicTemplate.Create(_newMechanicName);
                if (path == null)
                    EditorUtility.DisplayDialog("New Mechanic", "Use letters and digits (not starting with a digit), and a name that doesn't exist yet.", "OK");
                else
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                    EditorUtility.DisplayDialog("New Mechanic", $"Created {path}.\nFill in Activate(), then pick it under Mechanic once Unity has compiled.", "OK");
                    _newMechanicName = "";
                    _showNewMechanic = false;
                }
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            if (GUILayout.Button("Cancel", GUILayout.Width(60))) _showNewMechanic = false;
            GUILayout.EndHorizontal();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  List (left)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawList()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("TRAY ORDER  (left → right)", _subHeaderStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            _listScroll = GUILayout.BeginScrollView(_listScroll);

            PowerupDataSO pick = null;
            var ordered = new List<PowerupDataSO>(_db.Ordered);
            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];
                string unlock = p.unlockLevel == 0 ? "from start" : $"unlocks at level {p.unlockLevel + 1}";
                string problem = Problem(p);
                string sub = problem != null ? $"<color=#FFA633>⚠ {problem}</color>" : $"<color=#9999A6>{unlock}</color>";

                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                if (GUILayout.Button($"<b>{Name(p)}</b>\n<size=10>{sub}</size>", p == _selected ? _selectedRowStyle : _rowStyle, GUILayout.Height(44)))
                    pick = p;
                var row = GUILayoutUtility.GetLastRect();
                DrawIcon(new Rect(row.x + 6, row.y + 4, 36, 36), p);

                GUI.enabled = i > 0;
                if (GUILayout.Button(new GUIContent("▲", "Move left in the tray"), GUILayout.Width(24), GUILayout.Height(44))) { Move(p, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = i < ordered.Count - 1;
                if (GUILayout.Button(new GUIContent("▼", "Move right in the tray"), GUILayout.Width(24), GUILayout.Height(44))) { Move(p, +1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            if (pick) { Select(pick); GUIUtility.ExitGUI(); }

            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), DividerColor);
            GUI.color = TextMuted;
            GUILayout.Label($"  {ordered.Count} power-up(s) · the tray is built from this list at runtime", EditorStyles.miniLabel, GUILayout.Height(22));
            GUI.color = Color.white;
        }

        private static void DrawIcon(Rect r, PowerupDataSO p)
        {
            Texture tex = null;
            if (p.Icon) tex = p.Icon.texture;
            else if (p.TrayModel) tex = AssetPreview.GetAssetPreview(p.TrayModel);
            else if (p.UIPrefab) tex = AssetPreview.GetAssetPreview(p.UIPrefab);
            if (!tex) return;
            if (p.Icon)
            {
                var s = p.Icon.textureRect;
                GUI.DrawTextureWithTexCoords(r, tex, new Rect(s.x / tex.width, s.y / tex.height, s.width / tex.width, s.height / tex.height));
            }
            else GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Detail (right)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawDetail(float width)
        {
            if (!_selected || _so == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a power-up, or create one with + New Power-up.", _emptyStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            _so.Update();
            EditorGUIUtility.labelWidth = Mathf.Clamp(width * 0.28f, 110f, 170f);
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label(Name(_selected), _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping Asset", GUILayout.Width(90))) EditorGUIUtility.PingObject(_selected);
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            string problem = Problem(_selected);
            if (problem != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(12);
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
                GUILayout.Space(12);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(6);

            Section("Identity");
            EditorGUILayout.PropertyField(_so.FindProperty("displayName"), new GUIContent("Display name"));
            DrawIdField();
            EditorGUILayout.PropertyField(_so.FindProperty("icon"), new GUIContent("Icon", "Shown in the tray slot when there is no tray model."));
            EndSection();

            Section("Tray");
            EditorGUILayout.PropertyField(_so.FindProperty("uiPrefab"), new GUIContent("Slot prefab", "The slot spawned in the tray. PowerupSlot for custom power-ups; built-ins use their own Slot_* prefab."));
            var builtInProp = BuiltInProp(_selected.UIPrefab);
            if (builtInProp)
            {
                // Built-in slot: the prop (with its animation/points) is part of the slot prefab, so there's no Tray model to set.
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Model", "Built into the slot prefab together with its animation and points."),
                    new GUIContent($"{builtInProp.name}  (built into {_selected.UIPrefab.name})"));
                if (GUILayout.Button("Open Prefab", GUILayout.Width(90))) AssetDatabase.OpenAsset(_selected.UIPrefab);
                GUILayout.EndHorizontal();
                GUI.color = TextMuted;
                GUILayout.Label("Built-in slots carry their own 3D prop. To change it, open the slot prefab. Custom power-ups use PowerupSlot and pick a Tray model instead.", _descStyle);
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.PropertyField(_so.FindProperty("trayModel"), new GUIContent("Tray model", "Optional 3D model shown in the slot. Empty = the icon is shown."));
                GUI.color = TextMuted;
                GUILayout.Label("Empty tray model = the icon is shown.", _descStyle);
                GUI.color = Color.white;
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Preview", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("⌂ Reset Camera", GUILayout.Width(115))) _preview.ResetCamera();
            GUILayout.EndHorizontal();
            Rect pr = GUILayoutUtility.GetRect(10, 260, GUILayout.ExpandWidth(true));
            _preview.Draw(pr, _selected);
            if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.ScrollWheel) Repaint();
            EndSection();

            Section("Progression");
            var unlock = _so.FindProperty("unlockLevel");
            int shown = EditorGUILayout.IntField(new GUIContent("Unlocks at level", "The level number players see. 1 = available from the first level."), unlock.intValue + 1);
            unlock.intValue = Mathf.Max(1, shown) - 1;
            EditorGUILayout.PropertyField(_so.FindProperty("defaultAmount"), new GUIContent("Starting charges", "Charges a new player starts with."));
            EndSection();

            Section("Economy");
            EditorGUILayout.PropertyField(_so.FindProperty("buyCurrency"), new GUIContent("Currency"));
            EditorGUILayout.PropertyField(_so.FindProperty("buyCost"), new GUIContent("Price", "Offered when the player taps it with 0 charges."));
            EndSection();

            Section("Mechanic");
            DrawMechanic();
            EndSection();

            Section("Effects & Sound");
            EditorGUILayout.PropertyField(_so.FindProperty("activateVfx"), new GUIContent("Activate VFX"));
            EditorGUILayout.PropertyField(_so.FindProperty("endVfx"), new GUIContent("End VFX"));
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_so.FindProperty("activateSound"), new GUIContent("Activate sound"));
            GUI.enabled = _selected.ActivateSound;
            if (GUILayout.Button(new GUIContent("▶", "Preview"), GUILayout.Width(26))) PlayClip(_selected.ActivateSound);
            if (GUILayout.Button(new GUIContent("■", "Stop preview"), GUILayout.Width(26))) StopPreview();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            var vol = _so.FindProperty("activateVolume");
            vol.floatValue = EditorGUILayout.Slider("Volume", vol.floatValue, 0f, 1f);
            var pitch = _so.FindProperty("activatePitch");
            pitch.floatValue = EditorGUILayout.Slider("Pitch", pitch.floatValue, 0.5f, 2f);
            EndSection();

            GUILayout.Space(10);
            GUILayout.EndScrollView();

            if (_so.ApplyModifiedProperties()) { EditorUtility.SetDirty(_selected); _isDirty = true; }
        }

        private void DrawIdField()
        {
            var idProp = _so.FindProperty("id");
            string old = idProp.stringValue;
            string v = EditorGUILayout.DelayedTextField(new GUIContent("Id", "Unique key; the save file stores charges by it. Lowercase letters, digits and _ only."), old);
            if (v == old) return;
            v = Regex.Replace((v ?? "").ToLowerInvariant(), "[^a-z0-9_]", "");
            if (v.Length == 0 || _db.powerups.Exists(q => q && q != _selected && q.id == v))
            {
                EditorUtility.DisplayDialog("Id", v.Length == 0 ? "The id can't be empty." : $"Another power-up already uses '{v}'.", "OK");
                return;
            }
            if (!string.IsNullOrEmpty(old) && !EditorUtility.DisplayDialog("Change id?",
                    $"Players' saved charges are stored under '{old}'. After renaming to '{v}', existing players start this power-up from its starting charges.", "Rename", "Cancel"))
                return;
            idProp.stringValue = v;
        }

        private void DrawMechanic()
        {
            var types = new List<Type> { null };
            foreach (var t in TypeCache.GetTypesDerivedFrom<PowerupEffect>()) if (!t.IsAbstract) types.Add(t);
            var labels = new string[types.Count];
            labels[0] = "(none)";
            for (int i = 1; i < types.Count; i++) labels[i] = ObjectNames.NicifyVariableName(types[i].Name.Replace("Effect", ""));

            var current = _selected.Effect?.GetType(); // plain C# object, not a UnityEngine.Object
            int index = Mathf.Max(0, types.IndexOf(current));
            int next = EditorGUILayout.Popup(new GUIContent("Mechanic", "What the power-up does. Add your own with New Mechanic… in the toolbar."), index, labels);
            if (next != index && (current == null || EditorUtility.DisplayDialog("Switch mechanic?", "Its settings reset to the new mechanic's defaults.", "Switch", "Cancel")))
                _so.FindProperty("effect").managedReferenceValue = types[next] == null ? null : Activator.CreateInstance(types[next]);

            var ep = _so.FindProperty("effect");
            if (ep.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                var end = ep.GetEndProperty();
                var it = ep.Copy();
                for (bool enter = true; it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end); enter = false)
                    EditorGUILayout.PropertyField(it, true);
                EditorGUI.indentLevel--;
            }
            GUI.color = TextMuted;
            GUILayout.Label("New Mechanic… (toolbar) writes a script you fill in; it appears here after Unity compiles.", _descStyle);
            GUI.color = Color.white;
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

        private void DrawNoDatabase()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"No power-up database at {DatabasePath}.", _emptyStyle);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create Default Database", GUILayout.Width(200), GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Powerups/Create Default Database");
                Load();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Data
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// The prop of a built-in slot prefab (its Powerup has no model anchor, so the prop is baked in), or null
        /// for a PowerupSlot-style prefab that takes a Tray model.
        /// </summary>
        private static GameObject BuiltInProp(GameObject slotPrefab)
        {
            if (!slotPrefab) return null;
            var pu = slotPrefab.GetComponentInChildren<Powerup>(true);
            if (!pu) return null;
            return new SerializedObject(pu).FindProperty("modelAnchor").objectReferenceValue ? null : pu.gameObject;
        }

        private static string Name(PowerupDataSO p) =>
            !string.IsNullOrEmpty(p.DisplayName) ? p.DisplayName : !string.IsNullOrEmpty(p.id) ? p.id : p.name;

        /// <summary>First problem with this power-up, or null. Shown as ⚠ in the list and counted in the toolbar.</summary>
        private string Problem(PowerupDataSO p)
        {
            if (string.IsNullOrEmpty(p.id)) return "No id";
            int same = 0;
            foreach (var q in _db.powerups) if (q && q.id == p.id) same++;
            if (same > 1) return $"Id '{p.id}' is used {same} times";
            if (p.Effect == null) return "No mechanic";
            if (!p.UIPrefab) return "No slot prefab";
            if (p.UIPrefab == _genericSlot && !p.TrayModel && !p.Icon) return "No icon or tray model (the slot would be empty)";
            return null;
        }

        private void Select(PowerupDataSO p)
        {
            _selected = p;
            _so = p ? new SerializedObject(p) : null;
            GUI.FocusControl(null);
            Repaint();
        }

        /// <summary>Swaps this power-up with its neighbour in tray order, then renumbers 0..n-1.</summary>
        private void Move(PowerupDataSO p, int dir)
        {
            var list = new List<PowerupDataSO>(_db.Ordered);
            int i = list.IndexOf(p), j = i + dir;
            if (i < 0 || j < 0 || j >= list.Count) return;
            (list[i], list[j]) = (list[j], list[i]);
            for (int k = 0; k < list.Count; k++)
            {
                if (list[k].order == k) continue;
                Undo.RecordObject(list[k], "Reorder power-ups");
                list[k].order = k;
                EditorUtility.SetDirty(list[k]);
            }
            _isDirty = true;
            if (_so != null) _so.Update();
        }

        private void CreatePowerup(PowerupDataSO copyFrom)
        {
            string baseId = copyFrom ? copyFrom.id + "_copy" : "new_powerup";
            string id = baseId;
            for (int n = 2; _db.powerups.Exists(q => q && q.id == id); n++) id = baseId + n;

            var so = copyFrom ? Instantiate(copyFrom) : CreateInstance<PowerupDataSO>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{AssetFolder}/Powerup_{id}.asset");
            AssetDatabase.CreateAsset(so, path);
            var s = new SerializedObject(so);
            s.FindProperty("id").stringValue = id;
            s.FindProperty("order").intValue = _db.powerups.Count;
            if (copyFrom)
                s.FindProperty("displayName").stringValue = Name(copyFrom) + " Copy";
            else
            {
                s.FindProperty("displayName").stringValue = "New Power-up";
                s.FindProperty("defaultAmount").intValue = 3;
                s.FindProperty("unlockLevel").intValue = 0;
                s.FindProperty("uiPrefab").objectReferenceValue = _genericSlot;
            }
            s.ApplyModifiedPropertiesWithoutUndo();

            Undo.RecordObject(_db, "Add power-up");
            _db.powerups.Add(so);
            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            Select(so);
        }

        private void DeletePowerup(PowerupDataSO p)
        {
            if (!p) return;
            if (!EditorUtility.DisplayDialog("Delete power-up",
                    $"Delete '{Name(p)}'? The asset goes to the trash. Players' saved charges for '{p.id}' are left unused.", "Delete", "Cancel"))
                return;
            Undo.RecordObject(_db, "Delete power-up");
            _db.powerups.Remove(p);
            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(p));
            PowerupDataSO first = null;
            foreach (var q in _db.Ordered) { first = q; break; }
            Select(first);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Audio preview (same approach as the Sound Manager)
        // ═════════════════════════════════════════════════════════════════════

        private static void PlayClip(AudioClip clip)
        {
            StopPreview();
            AudioUtil?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopPreview() =>
            AudioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
    }
}
#endif
