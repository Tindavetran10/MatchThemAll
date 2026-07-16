#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Match_Them_All.Scripts.Power_Ups;

namespace Match_Them_All.Scripts.Editor
{
    /// <summary>
    /// One-shot setup: generates the 4 default PowerupDataSO assets + a PowerupDatabaseSO,
    /// with sensible default charges/tuning (per-effect field defaults). Icons/prefabs/Animator
    /// refs are left for the designer to assign in the Inspector.
    ///
    /// Uses SerializedObject to write private fields — the Editor folder compiles to a
    /// separate assembly (Assembly-CSharp-Editor), so internal/private access on the
    /// runtime PowerupDataSO would not be visible.
    ///
    /// Menu: Tools / Powerups / Create Default Database
    /// </summary>
    public static class PowerupDatabaseSetup
    {
        private const string OUTPUT_DIR = "Assets/Match Them All/Resources/Powerups";

        [MenuItem("Tools/Powerups/Create Default Database")]
        public static void CreateDatabase()
        {
            if (!Directory.Exists(OUTPUT_DIR))
                Directory.CreateDirectory(OUTPUT_DIR);

            // Tuning lives on the effect subclasses themselves (SpringEffect/FanEffect field defaults).
            const int defaultCharges = 3;

            var vacuum = CreateSO("vacuum",  "Vacuum",     order: 0, defaultAmount: defaultCharges, effect: new VacuumEffect());
            var spring = CreateSO("spring",  "Spring",     order: 1, defaultAmount: defaultCharges, effect: new SpringEffect());
            var fan    = CreateSO("fan",     "Fan",        order: 2, defaultAmount: defaultCharges, effect: new FanEffect());
            var freeze = CreateSO("freeze",  "Freeze Gun", order: 3, defaultAmount: defaultCharges, effect: new FreezeEffect());

            // Build the database.
            string dbPath = $"{OUTPUT_DIR}/PowerupDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<PowerupDatabaseSO>(dbPath);
            if (!db)
            {
                db = ScriptableObject.CreateInstance<PowerupDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.powerups = new System.Collections.Generic.List<PowerupDataSO> { vacuum, spring, fan, freeze };
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log("[PowerupDatabaseSetup] Created 4 PowerupDataSO assets + PowerupDatabaseSO under " + OUTPUT_DIR +
                      ". Assign icons/uiPrefab in each SO's Inspector.");
        }

        private static PowerupDataSO CreateSO(string id, string displayName, int order, int defaultAmount, PowerupEffect effect)
        {
            string path = $"{OUTPUT_DIR}/Powerup_{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<PowerupDataSO>(path);
            if (!so)
            {
                so = ScriptableObject.CreateInstance<PowerupDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("order").intValue = order;
            serialized.FindProperty("defaultAmount").intValue = defaultAmount;

            // [SerializeReference] field — managedReferenceValue accepts the instance directly.
            serialized.FindProperty("effect").managedReferenceValue = effect;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
            return so;
        }
    }
}
#endif
