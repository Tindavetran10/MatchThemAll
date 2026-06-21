using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using ZLinq;

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
        
        [Header("Data")]
        private readonly System.Collections.Generic.List<Item> _activeItems = new();

        /// <summary>
        /// Called by Level.Initialize() at runtime.
        /// Uses the SO's seed and item list to scatter items inside the spawn zone.
        /// </summary>
        public void Initialize(LevelDataSO data)
        {
            ClearItems();
            Random.InitState(data.seed);

            foreach (var entry in data.itemData)
            {
                int totalAmount = entry.amount * Mathf.Max(1, entry.multiplier);
                for (int i = 0; i < totalAmount; i++)
                {
                    SpawnItem(entry.itemPrefab);
                }
            }
            CompactItems();
        }

        public async Task InitializeAsync(LevelDataSO data)
        {
            ClearItems();
            Random.InitState(data.seed);

            int itemsSpawnedThisFrame = 0;
            const int itemsPerFrame = 5; // Spawn 5 items per frame to avoid spikes

            foreach (var entry in data.itemData)
            {
                int totalAmount = entry.amount * Mathf.Max(1, entry.multiplier);
                for (int i = 0; i < totalAmount; i++)
                {
                    SpawnItem(entry.itemPrefab);
                    
                    itemsSpawnedThisFrame++;
                    if (itemsSpawnedThisFrame >= itemsPerFrame)
                    {
                        itemsSpawnedThisFrame = 0;
                        await Task.Delay(10); // Task.Yield() runs synchronously in Unity. Delay(10) forces it to wait for the next frame.
                    }
                }
            }
            CompactItems();
        }

        private void ClearItems()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Destroy tracked preview items so they don't leak in the scene
                foreach (var item in _activeItems.AsValueEnumerable().Where(item => item)) 
                    DestroyImmediate(item.gameObject);
                _activeItems.Clear();
                return;
            }
#endif
            foreach (var item in _activeItems.AsValueEnumerable().Where(item => item)) 
                ItemPoolManager.Instance.ReleaseItem(item);

            _activeItems.Clear();
        }

        /// <summary>
        /// Removes null entries from the active items list.
        /// Called after spawning completes — NOT on every read.
        /// </summary>
        private void CompactItems()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                if (_activeItems[i] == null)
                    _activeItems.RemoveAt(i);
            }
        }

        private void SpawnItem(Item prefab)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
                Item item = go.GetComponent<Item>();
                item.transform.SetParent(transform);
                item.transform.position = GetSpawnPosition();
                item.transform.rotation = Quaternion.Euler(Random.onUnitSphere * 360f);
                _activeItems.Add(item);
                return;
            }
#endif
            Item runtimeItem = ItemPoolManager.Instance.GetItem(prefab);
            runtimeItem.transform.SetParent(transform);
            runtimeItem.transform.position = GetSpawnPosition();
            runtimeItem.transform.rotation = Quaternion.Euler(Random.onUnitSphere * 360f);
            _activeItems.Add(runtimeItem);
        }

        private Vector3 GetSpawnPosition()
        {
            float x = Random.Range(-spawnZone.size.x / 2f, spawnZone.size.x / 2f);
            float y = Random.Range(-spawnZone.size.y / 2f, spawnZone.size.y / 2f);
            float z = Random.Range(-spawnZone.size.z / 2f, spawnZone.size.z / 2f);

            Vector3 localPos = spawnZone.center + new Vector3(x, y, z);
            return transform.TransformPoint(localPos);
        }

        public System.Collections.Generic.List<Item> GetItems() => _activeItems;

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [Tooltip("Assign a LevelDataSO here to preview how items will spawn in the editor.")]
        [SerializeField] private LevelDataSO previewData;

        [Button("Preview Spawn")]
        private void PreviewSpawn()
        {
            if (previewData == null)
            {
                Debug.LogWarning("ItemPlacer: Assign a LevelDataSO to Preview Data first.");
                return;
            }

            PreviewSpawnWithData(previewData);
        }

        /// <summary>
        /// Called by LevelEditorWindow. Passes data directly so the serialized
        /// previewData field is never written to, keeping the prefab asset clean.
        /// </summary>
        public void PreviewSpawnFromEditor(LevelDataSO data) => PreviewSpawnWithData(data);

        private void PreviewSpawnWithData(LevelDataSO data) =>
            // ClearItems handles destroying previously spawned edit-mode objects
            Initialize(data);

        private void OnValidate()
        {
            // Validation now handled inside LevelDataSO / ItemLevelData
        }
#endif
    }
}
