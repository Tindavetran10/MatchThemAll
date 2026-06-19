using System;
using System.IO;
using System.Linq;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;

namespace Match_Them_All.Scripts.Editor
{
    public class ItemCreatorWindow : EditorWindow
    {
        private GameObject _modelPrefab;
        private Sprite _icon;
        private EItemName _itemName;

        private bool _isAddingNewType;
        private string _newItemTypeName = "";

        [MenuItem("Match Them All/Item Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemCreatorWindow>("Item Setup Wizard");
            window.minSize = new Vector2(350, 200);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("1. Define Data", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            _itemName = (EItemName)EditorGUILayout.EnumPopup("Item Type", _itemName);
            if (GUILayout.Button(_isAddingNewType ? "Cancel" : "New Type", GUILayout.Width(80)))
            {
                _isAddingNewType = !_isAddingNewType;
                _newItemTypeName = "";
            }
            GUILayout.EndHorizontal();

            if (_isAddingNewType)
            {
                GUILayout.BeginHorizontal();
                _newItemTypeName = EditorGUILayout.TextField("-> New Type Name", _newItemTypeName);
                if (GUILayout.Button("Add & Compile", GUILayout.Width(120)))
                {
                    AddNewItemType(_newItemTypeName);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Adding a new type will trigger a script compile. Do this before assigning your 3D model below.", MessageType.Warning);
            }

            _icon = (Sprite)EditorGUILayout.ObjectField("UI Icon Sprite", _icon, typeof(Sprite), false);

            GUILayout.Space(10);
            GUILayout.Label("2. Define 3D Visuals", EditorStyles.boldLabel);
            _modelPrefab = (GameObject)EditorGUILayout.ObjectField("3D Model (FBX/Prefab)", _modelPrefab, typeof(GameObject), false);

            GUILayout.Space(20);
            GUI.color = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Generate Item Prefab", GUILayout.Height(40)))
            {
                GeneratePrefab();
            }
            GUI.color = Color.white;
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This tool automatically builds the correct hierarchy, adds the Item and Rigidbody components, " +
                "makes colliders convex, and wires up all references in the Item script.", MessageType.Info);
        }

        private void GeneratePrefab()
        {
            if (!_modelPrefab)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a 3D model (FBX or Prefab).", "OK");
                return;
            }

            // Define save path
            const string folderPath = "Assets/Match Them All/Prefabs/Items";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            // Format name nicely: e.g. "BluePotion" -> "Blue Potion"
            string formattedName = System.Text.RegularExpressions.Regex.Replace(_itemName.ToString(), "([A-Z])", " $1").Trim();
            string path = $"{folderPath}/{formattedName}.prefab";

            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("Overwrite?", $"A prefab already exists at {path}. Overwrite it?", "Yes", "Cancel"))
                {
                    return;
                }
            }

            // 1. Create hierarchy in memory
            GameObject root = new GameObject(formattedName);
            
            // 2. Instantiate visual child
            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(_modelPrefab);
            visualInstance.transform.SetParent(root.transform);
            
            // Apply standard transforms based on existing prefabs
            visualInstance.transform.localPosition = new Vector3(0.00f, 0.04f, -0.75f);
            visualInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            visualInstance.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            
            visualInstance.name = "Renderer";

            // 3. Setup physics on visual child
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
                // Must be convex to work properly with non-kinematic Rigidbody
                mc.convex = true; 
            }

            Renderer rend = visualInstance.GetComponentInChildren<Renderer>();
            if (rend == null)
            {
                Debug.LogWarning("No Renderer found in the assigned 3D model.");
            }

            // 4. Setup root components
            Rigidbody rb = root.AddComponent<Rigidbody>();
            Item item = root.AddComponent<Item>();

            // Set Layer
            int matchLayer = LayerMask.NameToLayer("Match Stuff");
            if (matchLayer != -1)
            {
                SetLayerRecursively(root, matchLayer);
            }
            else
            {
                Debug.LogWarning("[Item Setup Wizard] Layer 'Match Stuff' not found! Skipping layer assignment.");
            }

            // 5. Wire up serialized fields on Item.cs
            var so = new SerializedObject(item);
            so.FindProperty("itemNameKey").intValue = (int)_itemName;
            so.FindProperty("icon").objectReferenceValue = _icon;
            
            if (rend) 
                so.FindProperty("_renderer").objectReferenceValue = rend;
                
            so.ApplyModifiedProperties();

            // 6. Save prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);

            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"[Item Setup Wizard] Successfully generated Item Prefab at {path}");
        }

        private void AddNewItemType(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            newName = newName.Replace(" ", ""); // Remove spaces
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                EditorUtility.DisplayDialog("Invalid Name", "The type name must be a valid C# identifier (no spaces, special characters).", "OK");
                return;
            }

            if (Enum.TryParse(typeof(EItemName), newName, out _))
            {
                EditorUtility.DisplayDialog("Already Exists", $"The type '{newName}' already exists in EItemName.", "OK");
                return;
            }

            const string enumPath = "Assets/Match Them All/Scripts/Enums/EItemName.cs";
            if (!File.Exists(enumPath))
            {
                EditorUtility.DisplayDialog("Error", "Could not find EItemName.cs!", "OK");
                return;
            }

            string fileContent = File.ReadAllText(enumPath);
            
            int enumStartIndex = fileContent.IndexOf("enum EItemName", StringComparison.Ordinal);
            if (enumStartIndex == -1) return;

            int firstBrace = fileContent.IndexOf("{", enumStartIndex, StringComparison.Ordinal);
            int closeBrace = fileContent.IndexOf("}", firstBrace, StringComparison.Ordinal);
            
            string enumBody = fileContent.Substring(firstBrace + 1, closeBrace - firstBrace - 1);
            
            int highestVal = Enum.GetValues(typeof(EItemName)).Cast<int>().Prepend(-1).Max();
            int nextVal = highestVal + 1;

            string newEnumBody = enumBody.TrimEnd();
            if (!newEnumBody.EndsWith(","))
            {
                newEnumBody += ",";
            }
            newEnumBody += $"\n        {newName} = {nextVal}\n    ";

            string newFileContent = fileContent.Substring(0, firstBrace + 1) + newEnumBody + fileContent.Substring(closeBrace);
            
            File.WriteAllText(enumPath, newFileContent);
            
            _isAddingNewType = false;
            AssetDatabase.Refresh();
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (!obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}
