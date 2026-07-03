using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Match_Them_All.Scripts.Editor
{
    public class ItemManagerWindow : EditorWindow
    {
        #region State & Fields
        private readonly List<GameObject> _itemPrefabs = new();
        private GameObject _selectedPrefab;
        private int _selectedIndex = -1;
        
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        
        // --- Modes ---
        private bool _isCreateMode;

        // --- Create Mode State ---
        private EItemName _newItemName;
        private Sprite _newItemIcon;
        private GameObject _newItemModelPrefab;
        private bool _isAddingNewItemType;
        private string _newItemTypeName = "";
        private bool _autoGenerateIcon = true;
        
        // Icon Settings State
        private Vector3 _iconRotation = new(0f, 0f, 90f);
        private float _iconPadding = 1.4f;
        private Vector3 _iconOffset = Vector3.zero;
        private Texture2D _previewIconTexture;
        private bool _previewDirty;
        private double _previewDirtyTime;
        private const double PreviewRenderThrottle = 0.08;

        // --- 3D Dock Preview State ---
        private PreviewRenderUtility _dockPreviewUtility;
        private GameObject _previewDockInstance;
        private GameObject _previewItemInstance;
        private bool _isDockPreviewInitialized;
        private GameObject _dockPrefab;

        // --- Orbital Camera State ---
        private float   _previewYaw   = 0f;
        private float   _previewPitch = 85f;   // start top-down like real gameplay
        private float   _previewDist  = 0.4f;
        private Vector3 _previewPanOffset = new(0f, 0f, 0f);
        // Auto-computed look-at focus (world-space center of the dock+item bounds) so the camera
        // frames the assembly instead of aiming at the dock base and clipping the item's head.
        // _previewPanOffset is a user delta on top of this.
        private Vector3 _previewFocus = Vector3.zero;

        // Default = top-down view matching the real game camera (90° pitch, FOV 50)
        private const float DefaultPreviewYaw   = 0f;
        private const float DefaultPreviewPitch = 85f;
        private const float DefaultPreviewDist  = 0.4f;
        private static readonly Vector3 DefaultPreviewPan = new(0f, 0f, 0f);

        // Delegates to ItemReferenceOps
        private const string ItemPrefabFolder = ItemReferenceOps.ItemPrefabFolder;

        // --- Styles ---
        private GUIStyle _cardStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _itemButtonStyle;
        private GUIStyle _selectedItemButtonStyle;
        private bool _stylesInitialized;

        private static readonly Color PanelBg = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.25f);
        private static readonly Color AccentBlue = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen = new(0.26f, 0.83f, 0.53f);
        private readonly List<Texture2D> _ownedTextures = new();
        #endregion

        #region Entry Point
        [MenuItem("Match Them All/Item Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemManagerWindow>("Item Manager");
            window.minSize = new Vector2(800, 500);
            window.LoadAll();
        }

        private void OnEnable()
        {
            LoadAll();
            _dockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Match Them All/Prefabs/Gameplay/Item Spot.prefab");
        }

        private void OnDisable()
        {
            _stylesInitialized = false;
            foreach (var t in _ownedTextures)
            {
                if (t != null) DestroyImmediate(t);
            }
            _ownedTextures.Clear();
            CleanupDockPreview();
        }

        private void OnDestroy()
        {
            CleanupDockPreview();
            if (_previewIconTexture != null) DestroyImmediate(_previewIconTexture);
        }
        #endregion

        #region Data Loading
        public void LoadAll()
        {
            _itemPrefabs.Clear();
            if (AssetDatabase.IsValidFolder(ItemPrefabFolder))
            {
                var pGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ItemPrefabFolder });
                foreach (var g in pGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    if (!path.Contains("/Trash/"))
                    {
                        var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (loaded != null && loaded.GetComponent<Item>() != null) 
                            _itemPrefabs.Add(loaded);
                    }
                }
            }

            if (_selectedPrefab)
            {
                _selectedIndex = _itemPrefabs.IndexOf(_selectedPrefab);
                if (_selectedIndex < 0) SelectItem(-1);
            }
        }

        private void SelectItem(int idx)
        {
            _selectedIndex = idx;
            _selectedPrefab = idx >= 0 && idx < _itemPrefabs.Count ? _itemPrefabs[idx] : null;
            _isCreateMode = false;
            
            // If selecting a prefab, we update the dock preview
            RefreshDockPreview();
        }
        #endregion

        #region Styles
        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = MakeTex(2, 2, CardBg) }
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
            };

            _itemButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 8, 8, 8),
                margin = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                normal =
                {
                    background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.28f)),
                    textColor = Color.white
                },
                hover = { background = MakeTex(2, 2, new Color(0.30f, 0.30f, 0.34f)) }
            };

            _selectedItemButtonStyle = new GUIStyle(_itemButtonStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f)) },
                fontStyle = FontStyle.Bold
            };
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (var i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            _ownedTextures.Add(t);
            return t;
        }

        private void BeginCard() => GUILayout.BeginVertical(_cardStyle);
        private static void EndCard() => GUILayout.EndVertical();
        #endregion

        #region Main Layout
        private void OnGUI()
        {
            EnsureStyles();

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);

            // Toolbar
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), new Color(0.14f, 0.14f, 0.16f));
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);
            GUI.color = AccentBlue;
            GUILayout.Label("💎 Item Manager", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↻ Reload", GUILayout.Height(28), GUILayout.Width(80)))
            {
                LoadAll();
                Repaint();
            }
            GUILayout.Space(12);
            GUILayout.EndHorizontal();

            // Two-column layout
            var leftWidth = Mathf.Max(200f, position.width * 0.27f);
            var rightWidth = position.width - leftWidth - 2;

            GUILayout.BeginHorizontal();

            // LEFT: Item list
            GUILayout.BeginVertical(GUILayout.Width(leftWidth));
            DrawItemList(leftWidth);
            GUILayout.EndVertical();

            // Divider
            EditorGUI.DrawRect(new Rect(leftWidth, 38, 2, position.height - 38), new Color(0.12f, 0.12f, 0.14f));
            GUILayout.Space(2);

            // RIGHT: Detail
            GUILayout.BeginVertical(GUILayout.Width(rightWidth));
            if (_isCreateMode)
                DrawCreateMode();
            else if (_selectedPrefab)
                DrawEditMode();
            else
                DrawEmptyState();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        #endregion

        #region Left Panel
        private void DrawItemList(float width)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("ITEMS", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("+ Create", GUILayout.Width(65), GUILayout.Height(22)))
            {
                _isCreateMode = true;
                _selectedPrefab = null;
                _selectedIndex = -1;
            }
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            _listScroll = GUILayout.BeginScrollView(_listScroll);
            for (var i = 0; i < _itemPrefabs.Count; i++)
            {
                var item = _itemPrefabs[i];
                var selected = i == _selectedIndex && !_isCreateMode;
                var style = selected ? _selectedItemButtonStyle : _itemButtonStyle;

                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                if (GUILayout.Button($"  {item.name}", style, GUILayout.Height(36)))
                {
                    SelectItem(i);
                }
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUI.color = new Color(0.4f, 0.4f, 0.45f);
            GUILayout.Label("Select an item on the left\nor click '+ Create' to make a new one.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 13 },
                GUILayout.Height(60));
            GUI.color = Color.white;
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }
        #endregion

        #region Edit Mode (3D Preview)
        private void DrawEditMode()
        {
            var item = _selectedPrefab?.GetComponent<Item>();
            if (item == null) return;

            // \u2500\u2500 Settings section (fixed height, scrollable) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label(_selectedPrefab.name, _headerStyle);
            GUILayout.FlexibleSpace();
            GUI.color = new Color(0.65f, 0.65f, 0.7f);
            if (GUILayout.Button("Ping Asset", GUILayout.Width(90), GUILayout.Height(26)))
                EditorGUIUtility.PingObject(_selectedPrefab);
            GUI.color = Color.white;
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            BeginCard();
            GUILayout.Label("Dock Placement Overrides", _subHeaderStyle);
            GUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            var so = new SerializedObject(item);
            var useRotProp    = so.FindProperty("useCustomDockRotation");
            var rotProp       = so.FindProperty("customDockRotation");
            var useOffsetProp = so.FindProperty("useCustomDockPositionOffset");
            var offsetProp    = so.FindProperty("customDockPositionOffset");

            bool useRotation = EditorGUILayout.Toggle("Use Custom Rotation", useRotProp.boolValue);
            Vector3 rot = rotProp.vector3Value;
            if (useRotation)
            {
                EditorGUI.indentLevel++;
                rot = EditorGUILayout.Vector3Field("Custom Dock Rotation", rotProp.vector3Value);
                EditorGUI.indentLevel--;
            }

            bool useOffset = EditorGUILayout.Toggle("Use Custom Offset", useOffsetProp.boolValue);
            Vector3 offset = offsetProp.vector3Value;
            if (useOffset)
            {
                EditorGUI.indentLevel++;
                offset = EditorGUILayout.Vector3Field("Position Offset", offsetProp.vector3Value);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                useRotProp.boolValue    = useRotation;
                rotProp.vector3Value    = rot;
                useOffsetProp.boolValue = useOffset;
                offsetProp.vector3Value = offset;
                so.ApplyModifiedProperties();
                UpdatePreviewItemTransform(item);
            }
            EndCard();
            GUILayout.Space(8);

            // \u2500\u2500 Preview section \u2014 fills all remaining vertical space \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            BeginCard();
            GUILayout.BeginHorizontal();
            GUILayout.Label("3D Dock Preview", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("\u2302 Reset Camera", GUILayout.Width(115), GUILayout.Height(20)))
                ResetPreviewCamera();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (_dockPrefab == null)
            {
                EditorGUILayout.HelpBox("Could not find 'Item Spot.prefab' to render the preview.", MessageType.Warning);
            }
            else
            {
                // ExpandHeight fills all remaining space in the right panel
                Rect previewRect = GUILayoutUtility.GetRect(
                    10, 10,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true));
                DrawDockPreview(previewRect, item);
            }
            EndCard();
        }
        
        private void InitDockPreview()
        {
            if (_isDockPreviewInitialized && _dockPreviewUtility != null) return;
            
            _dockPreviewUtility = new PreviewRenderUtility();
            _dockPreviewUtility.camera.fieldOfView = 50f;  // matches real game camera FOV
            _dockPreviewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            _dockPreviewUtility.camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 1f);
            _dockPreviewUtility.camera.nearClipPlane = 0.01f;

            // Lighting
            _dockPreviewUtility.lights[0].intensity = 1.5f;
            _dockPreviewUtility.lights[0].transform.eulerAngles = new Vector3(50f, 330f, 0f);
            _dockPreviewUtility.lights[1].intensity = 0.5f;

            // ── Camera setup ──────────────────────────────────────────────────
            // The dock prefab is a small flat object. In world space we place the
            // dock at the origin and the camera directly in front at +Z, looking back.
            //
            //   Camera  ----(looks at)---->  Dock @ origin
            //   Z = +0.4                    Z = 0
            //
            // Camera position is driven per-frame by the orbital controls in DrawDockPreview;
            // set a safe initial position here just in case.
            _dockPreviewUtility.camera.transform.position = new Vector3(0f, 0.3f, 0.5f);
            _dockPreviewUtility.camera.transform.rotation = Quaternion.Euler(20f, 180f, 0f);

            if (_dockPrefab)
            {
                _previewDockInstance = _dockPreviewUtility.InstantiatePrefabInScene(_dockPrefab);
                _previewDockInstance.transform.position = Vector3.zero;
                // Exact in-game instance rotation so items inherit the same orientation as gameplay
                _previewDockInstance.transform.rotation = new Quaternion(-0.61543345f, -0.6154355f, 0.3481978f, -0.34819424f);
                _previewDockInstance.transform.localScale = Vector3.one;
            }

            _previewFocus = ComputePreviewFocus();
            _isDockPreviewInitialized = true;
        }

        private void ResetPreviewCamera()
        {
            _previewYaw       = DefaultPreviewYaw;
            _previewPitch     = DefaultPreviewPitch;
            _previewDist      = DefaultPreviewDist;
            _previewPanOffset = DefaultPreviewPan;
            Repaint();
        }

        private void CleanupDockPreview()
        {
            if (_dockPreviewUtility != null)
            {
                _dockPreviewUtility.Cleanup();
                _dockPreviewUtility = null;
            }
            _isDockPreviewInitialized = false;
        }

        /// <summary>
        /// World-space center of the combined dock + item renderer bounds — the point the orbital
        /// camera should look at so the whole assembly is framed (head included) regardless of the
        /// item's height or pivot. Falls back to the dock origin if nothing has bounds yet.
        /// </summary>
        private Vector3 ComputePreviewFocus()
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;
            foreach (var root in new[] { _previewDockInstance, _previewItemInstance })
            {
                if (!root) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (!r) continue;
                    if (!any) { b = r.bounds; any = true; }
                    else b.Encapsulate(r.bounds);
                }
            }
            return any ? b.center : Vector3.zero;
        }

        private void RefreshDockPreview()
        {
            if (!_isDockPreviewInitialized) InitDockPreview();
            
            if (_previewItemInstance)
                DestroyImmediate(_previewItemInstance);

            if (_selectedPrefab && _dockPreviewUtility != null)
            {
                _previewItemInstance = _dockPreviewUtility.InstantiatePrefabInScene(_selectedPrefab);
                
                // Mirror gameplay hierarchy: item becomes child of dock's "Animation Parent"
                Transform itemParent = null;
                if (_previewDockInstance)
                    itemParent = _previewDockInstance.transform.Find("Animation Parent");
                if (itemParent == null && _previewDockInstance)
                    itemParent = _previewDockInstance.transform;
                
                if (itemParent)
                    _previewItemInstance.transform.SetParent(itemParent, false);

                var item = _selectedPrefab.GetComponent<Item>();
                UpdatePreviewItemTransform(item);
            }
        }

        private void UpdatePreviewItemTransform(Item configItem)
        {
            if (!_previewItemInstance || !configItem) return;

            // Exactly mirrors ItemSpotManager values from MainScene
            var targetPosition = new Vector3(0.095f, 0f, 0.095f);
            var targetScale    = new Vector3(0.15f, 0.15f, 0.15f);
            var targetEuler    = Vector3.zero;

            if (configItem.UseCustomDockPositionOffset)
                targetPosition += configItem.CustomDockPositionOffset;
            if (configItem.UseCustomDockRotation)
                targetEuler = configItem.CustomDockRotation;

            _previewItemInstance.transform.localScale    = targetScale;
            _previewItemInstance.transform.localPosition = targetPosition;
            _previewItemInstance.transform.localRotation = Quaternion.Euler(targetEuler);

            // Re-center the framing whenever the item's pose changes (spawn or offset/rotation tweak),
            // so the camera always targets the assembly's bounds center instead of the dock base.
            _previewFocus = ComputePreviewFocus();
        }

        private void DrawDockPreview(Rect rect, Item item)
        {
            if (!_isDockPreviewInitialized) InitDockPreview();
            if (!_previewItemInstance && _selectedPrefab) RefreshDockPreview();

            // ── Orbital camera input ──────────────────────────────────────────
            var evt = Event.current;
            if (rect.Contains(evt.mousePosition))
            {
                // Left mouse drag → orbit
                if (evt.type == EventType.MouseDrag && evt.button == 0)
                {
                    _previewYaw   += evt.delta.x * 0.5f;
                    _previewPitch -= evt.delta.y * 0.5f;
                    _previewPitch  = Mathf.Clamp(_previewPitch, -80f, 80f);
                    evt.Use();
                    Repaint();
                }
                // Middle mouse drag → pan
                if (evt.type == EventType.MouseDrag && evt.button == 2)
                {
                    // Build camera-space right and up vectors to pan in screen space
                    float pitchR = _previewPitch * Mathf.Deg2Rad;
                    float yawR   = _previewYaw   * Mathf.Deg2Rad;
                    var forward = new Vector3(
                        -Mathf.Cos(pitchR) * Mathf.Sin(yawR),
                        -Mathf.Sin(pitchR),
                        -Mathf.Cos(pitchR) * Mathf.Cos(yawR)
                    ).normalized;
                    var right = Vector3.Cross(Vector3.up, forward).normalized;
                    var up    = Vector3.Cross(forward, right).normalized;

                    float panSpeed = _previewDist * 0.001f;
                    _previewPanOffset -= right * (evt.delta.x * panSpeed);
                    _previewPanOffset += up    * (evt.delta.y * panSpeed);
                    evt.Use();
                    Repaint();
                }
                // Scroll wheel → zoom
                if (evt.type == EventType.ScrollWheel)
                {
                    _previewDist += evt.delta.y * 0.01f;
                    _previewDist  = Mathf.Clamp(_previewDist, 0.05f, 2f);
                    evt.Use();
                    Repaint();
                }
            }

            // ── Spherical → Cartesian camera position (always looks at target) ─
            float pitchRad = _previewPitch * Mathf.Deg2Rad;
            float yawRad   = _previewYaw   * Mathf.Deg2Rad;
            var camOffset = new Vector3(
                _previewDist * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
                _previewDist * Mathf.Sin(pitchRad),
                _previewDist * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
            );
            // Orbit around the auto-framed focus (bounds center) plus any user pan delta.
            Vector3 target = _previewFocus + _previewPanOffset;
            _dockPreviewUtility.camera.transform.position = target + camOffset;
            _dockPreviewUtility.camera.transform.LookAt(target, Vector3.up);

            // ── Render ───────────────────────────────────────────────────────
            _dockPreviewUtility.BeginPreview(rect, GUIStyle.none);
            _dockPreviewUtility.camera.Render();
            Texture previewTex = _dockPreviewUtility.EndPreview();
            GUI.DrawTexture(rect, previewTex, ScaleMode.ScaleToFit, false);

            // ── Usage hint ───────────────────────────────────────────────────
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1, 1, 1, 0.4f) },
                alignment = TextAnchor.LowerRight
            };
            GUI.Label(rect, "LMB drag: orbit  •  MMB drag: pan  •  Scroll: zoom ", hintStyle);
        }
        #endregion

        #region Create Mode
        private void DrawCreateMode()
        {
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("ITEM PREFAB GENERATOR", _headerStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            BeginCard();
            GUILayout.Label("Create New Item Prefab", _subHeaderStyle);
            GUILayout.Space(10);
            
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            _newItemName = (EItemName)EditorGUILayout.EnumPopup("Item Name Type", _newItemName);
            if (GUILayout.Button("New Type...", GUILayout.Width(100)))
            {
                _isAddingNewItemType = !_isAddingNewItemType;
                _newItemTypeName = "";
            }
            EditorGUILayout.EndHorizontal();

            if (_isAddingNewItemType)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                _newItemTypeName = EditorGUILayout.TextField(_newItemTypeName);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    AddNewItemType(_newItemTypeName);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            _autoGenerateIcon = EditorGUILayout.Toggle("Auto-Generate Icon", _autoGenerateIcon);

            if (_autoGenerateIcon)
            {
                GUILayout.Space(10);
                GUILayout.Label("Icon Camera Settings", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;
                _iconRotation = EditorGUILayout.Vector3Field("Model Rotation", _iconRotation);
                _iconPadding = EditorGUILayout.Slider("Zoom Padding", _iconPadding, 1f, 3f);
                _iconOffset = EditorGUILayout.Vector3Field("Camera Offset", _iconOffset);
                EditorGUI.indentLevel--;
                bool settingsChanged = EditorGUI.EndChangeCheck();
                
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                GUI.color = AccentBlue;
                if (GUILayout.Button("Manual Refresh", GUILayout.Height(30)))
                    settingsChanged = true;
                GUI.color = Color.white;
                
                if (_newItemModelPrefab == null)
                    EditorGUILayout.HelpBox("Assign a 3D Model Prefab to preview.", MessageType.Info);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
                TryConsumeIconPreview();
                if (_previewIconTexture != null)
                {
                    Rect outerRect = GUILayoutUtility.GetRect(84, 84, GUILayout.ExpandWidth(false));
                    Rect texRect = new Rect(outerRect.x + 2, outerRect.y + 2, 80, 80);
                    EditorGUI.DrawRect(outerRect, new Color(0.3f, 0.3f, 0.3f));
                    EditorGUI.DrawRect(texRect, new Color(0.15f, 0.15f, 0.15f));
                    GUI.DrawTexture(texRect, _previewIconTexture, ScaleMode.ScaleToFit);
                }
                
                EditorGUILayout.EndHorizontal();

                if (settingsChanged && _newItemModelPrefab != null)
                    ScheduleIconPreview();
            }
            else
            {
                GUILayout.Space(5);
                _newItemIcon = (Sprite)EditorGUILayout.ObjectField("UI Icon (Sprite)", _newItemIcon, typeof(Sprite), false);
            }
            
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            _newItemModelPrefab = (GameObject)EditorGUILayout.ObjectField("3D Model Prefab", _newItemModelPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && _newItemModelPrefab != null && _autoGenerateIcon)
                ScheduleIconPreview();

            GUILayout.Space(15);

            GUI.color = AccentGreen;
            bool canGenerate = _newItemModelPrefab && (_autoGenerateIcon || _newItemIcon);
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("🚀 Generate Item Prefab", GUILayout.Height(30)))
            {
                GenerateItemPrefab();
            }
            EditorGUI.EndDisabledGroup();
            GUI.color = Color.white;
            
            if (!canGenerate)
            {
                EditorGUILayout.HelpBox("Please assign a 3D Model Prefab (and an Icon if auto-generation is disabled).", MessageType.Warning);
            }

            EndCard();
            GUILayout.EndScrollView();
        }

        private void GenerateItemPrefab()
        {
            if (!_newItemModelPrefab || (!_autoGenerateIcon && !_newItemIcon)) return;

            string formattedName = "Item_" + _newItemName;

            if (!Directory.Exists(ItemPrefabFolder))
                Directory.CreateDirectory(ItemPrefabFolder);

            string path = $"{ItemPrefabFolder}/{formattedName}.prefab";

            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("Overwrite?",
                        $"A prefab named {formattedName} already exists. Overwrite?",
                        "Yes", "Cancel"))
                    return;
            }

            Sprite finalIcon = _newItemIcon;
            if (_autoGenerateIcon)
                finalIcon = CaptureItemIcon(_newItemModelPrefab, formattedName);

            GameObject root = new GameObject(formattedName);
            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(_newItemModelPrefab);
            visualInstance.transform.SetParent(root.transform);
            
            visualInstance.transform.localPosition = new Vector3(0.00f, 0.00f, -0.75f);
            visualInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            visualInstance.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            visualInstance.name = "Renderer";

            Collider col = visualInstance.GetComponentInChildren<Collider>();
            if (!col)
            {
                MeshFilter mf = visualInstance.GetComponentInChildren<MeshFilter>();
                if (mf)
                {
                    var meshCollider = mf.gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                    col = meshCollider;
                }
                else
                {
                    col = visualInstance.AddComponent<BoxCollider>();
                }
            }
            else if (col is MeshCollider mc)
            {
                mc.convex = true; 
            }

            Renderer rend = visualInstance.GetComponentInChildren<Renderer>();
            if (!rend) Debug.LogWarning("No Renderer found in the assigned 3D model.");

            Rigidbody rb = root.AddComponent<Rigidbody>();
            Item item = root.AddComponent<Item>();

            int matchLayer = LayerMask.NameToLayer("Match Stuff");
            if (matchLayer != -1) SetLayerRecursively(root, matchLayer);

            var so = new SerializedObject(item);
            so.FindProperty("itemNameKey").intValue = (int)_newItemName;
            so.FindProperty("icon").objectReferenceValue = finalIcon;
            if (rend) so.FindProperty("_renderer").objectReferenceValue = rend;
            so.ApplyModifiedProperties();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);

            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"[Item Manager] Successfully generated Item Prefab at {path}");
            
            LoadAll();
        }

        private void ScheduleIconPreview()
        {
            _previewDirty = true;
            _previewDirtyTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void TryConsumeIconPreview()
        {
            if (!_previewDirty || _newItemModelPrefab == null) return;
            if (_previewIconTexture == null || EditorApplication.timeSinceStartup - _previewDirtyTime >= PreviewRenderThrottle)
            {
                if (_previewIconTexture != null) DestroyImmediate(_previewIconTexture);
                _previewIconTexture = GetIconTexture2D(_newItemModelPrefab);
                _previewDirty = false;
            }
            else
            {
                Repaint();
            }
        }

        private Texture2D GetIconTexture2D(GameObject modelPrefab)
        {
            var previewUtility = new PreviewRenderUtility();
            try
            {
                previewUtility.camera.orthographic = true;
                previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewUtility.camera.backgroundColor = new Color(0, 0, 0, 0);

                previewUtility.lights[0].intensity = 2f;
                previewUtility.lights[0].color = Color.white;
                previewUtility.lights[0].transform.eulerAngles = new Vector3(53.516f, 349.741f, 49.274f);
                previewUtility.lights[1].intensity = 0f;

                previewUtility.BeginPreview(new Rect(0, 0, 512, 512), GUIStyle.none);

                var model = previewUtility.InstantiatePrefabInScene(modelPrefab);
                model.transform.rotation = Quaternion.Euler(_iconRotation);
                model.transform.localScale = Vector3.one;
                model.transform.position = Vector3.zero;

                Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else bounds.Encapsulate(renderer.bounds);
                }

                if (hasBounds)
                {
                    previewUtility.camera.transform.position = bounds.center - Vector3.forward * 5f + _iconOffset;
                    previewUtility.camera.transform.rotation = Quaternion.identity;
                    float maxExtent = Mathf.Max(bounds.extents.y, bounds.extents.x);
                    previewUtility.camera.orthographicSize = Mathf.Max(maxExtent * _iconPadding, 0.1f);
                }
                else
                {
                    previewUtility.camera.transform.position = new Vector3(0, 0, -5f) + _iconOffset;
                    previewUtility.camera.transform.rotation = Quaternion.identity;
                    previewUtility.camera.orthographicSize = 5f;
                }

                previewUtility.camera.Render();
                Texture rt = previewUtility.EndPreview();

                RenderTexture.active = (RenderTexture)rt;
                var tex2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex2D.Apply();
                return tex2D;
            }
            finally
            {
                RenderTexture.active = null;
                previewUtility.Cleanup();
            }
        }

        private Sprite CaptureItemIcon(GameObject modelPrefab, string formattedName)
        {
            var iconFolder = ItemReferenceOps.IconsFolder;
            if (!Directory.Exists(iconFolder)) Directory.CreateDirectory(iconFolder);

            string path = $"{iconFolder}/icon_{formattedName.ToLowerInvariant()}.png";

            var tex2D = GetIconTexture2D(modelPrefab);
            byte[] bytes = tex2D.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            DestroyImmediate(tex2D);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite s) return s;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private void AddNewItemType(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            newName = newName.Replace(" ", "");
            
            if (!Regex.IsMatch(newName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                EditorUtility.DisplayDialog("Invalid Name", "The type name must be a valid C# identifier.", "OK");
                return;
            }

            if (Enum.TryParse(typeof(EItemName), newName, out _))
            {
                EditorUtility.DisplayDialog("Already Exists", $"The type '{newName}' already exists.", "OK");
                return;
            }

            const string enumPath = "Assets/Match Them All/Scripts/Enums/EItemName.cs";
            if (!File.Exists(enumPath)) return;

            string fileContent = File.ReadAllText(enumPath);
            int enumStartIndex = fileContent.IndexOf("enum EItemName", StringComparison.Ordinal);
            if (enumStartIndex == -1) return;

            int firstBrace = fileContent.IndexOf("{", enumStartIndex, StringComparison.Ordinal);
            int closeBrace = fileContent.IndexOf("}", firstBrace, StringComparison.Ordinal);
            
            string enumBody = fileContent.Substring(firstBrace + 1, closeBrace - firstBrace - 1);
            int highestVal = Enum.GetValues(typeof(EItemName)).Cast<int>().Prepend(-1).Max();
            int nextVal = highestVal + 1;

            string newEnumBody = enumBody.TrimEnd();
            if (!newEnumBody.EndsWith(",")) newEnumBody += ",";
            newEnumBody += $"\n        {newName} = {nextVal}\n    ";

            string newFileContent = fileContent[..(firstBrace + 1)] + newEnumBody + fileContent[closeBrace..];
            File.WriteAllText(enumPath, newFileContent);
            
            _isAddingNewItemType = false;
            AssetDatabase.Refresh();
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (!obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
        }
        #endregion
    }
}
