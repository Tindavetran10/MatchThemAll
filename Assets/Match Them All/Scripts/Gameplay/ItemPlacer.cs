using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Spawns items at runtime from a LevelDataSO.
    /// No longer stores item data itself — that lives in the ScriptableObject.
    /// </summary>
    public class ItemPlacer : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private BoxCollider spawnZone;

        /// <summary>
        /// Called by Level.Initialize() at runtime.
        /// Uses the SO's seed and item list to scatter items inside the spawn zone.
        /// </summary>
        public void Initialize(LevelDataSO data)
        {
            Random.InitState(data.seed);

            foreach (var entry in data.itemData)
            {
                for (int i = 0; i < entry.amount; i++)
                {
                    Item item = Instantiate(entry.itemPrefab, transform);
                    item.transform.position = GetSpawnPosition();
                    item.transform.rotation = Quaternion.Euler(Random.onUnitSphere * 360f);
                }
            }
        }

        private Vector3 GetSpawnPosition()
        {
            float x = Random.Range(-spawnZone.size.x / 2f, spawnZone.size.x / 2f);
            float y = Random.Range(-spawnZone.size.y / 2f, spawnZone.size.y / 2f);
            float z = Random.Range(-spawnZone.size.z / 2f, spawnZone.size.z / 2f);

            Vector3 localPos = spawnZone.center + new Vector3(x, y, z);
            return transform.TransformPoint(localPos);
        }

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [Tooltip("Assign a LevelDataSO here to preview how items will spawn in the editor.")]
        [SerializeField] private LevelDataSO previewData;

        [Button("Preview Spawn")]
        private void PreviewSpawn()
        {
            // Clear any previously spawned items
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                child.parent = null;
                DestroyImmediate(child.gameObject);
            }

            if (previewData == null)
            {
                Debug.LogWarning("ItemPlacer: Assign a LevelDataSO to Preview Data first.");
                return;
            }

            Initialize(previewData);
        }

        private void OnValidate()
        {
            // Validation now handled inside LevelDataSO / ItemLevelData
        }
#endif
    }
}
