using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Scatters a level's items inside a box. The level's seed makes the scatter reproducible:
    /// each item consumes, in order, three Random.Range calls (x, y, z) and one Random.onUnitSphere.
    /// Keep that order — changing it reshuffles every existing level.
    /// </summary>
    public class ItemPlacer : MonoBehaviour
    {
        [Header("Spawn Area")]
        [SerializeField] private BoxCollider spawnZone;

        private const int ItemsPerFrame = 5;
        private readonly List<Item> _activeItems = new();

        public List<Item> GetItems() => _activeItems;

        /// <summary>Spawns the whole level in one frame.</summary>
        public void Initialize(LevelDataSO data)
        {
            if (!BeginSpawn(data)) return;
            foreach (var prefab in Expand(data)) Spawn(prefab);
        }

        /// <summary>Spawns the level a few items per frame to avoid a hitch.</summary>
        public async Task InitializeAsync(LevelDataSO data)
        {
            if (!BeginSpawn(data)) return;
            int inBatch = 0;
            foreach (var prefab in Expand(data))
            {
                Spawn(prefab);
                if (++inBatch < ItemsPerFrame) continue;
                inBatch = 0;
                await Task.Delay(10); // Task.Yield() completes synchronously in Unity; a short delay reaches the next frame
            }
        }

        private bool BeginSpawn(LevelDataSO data)
        {
            if (!data) { Debug.LogError("ItemPlacer: null LevelDataSO received."); return false; }
            Despawn();
            Random.InitState(data.seed);
            return true;
        }

        /// <summary>One entry per item to spawn (amount × multiplier copies of each prefab).</summary>
        private static IEnumerable<Item> Expand(LevelDataSO data)
        {
            foreach (var entry in data.itemData)
            {
                int copies = entry.amount * Mathf.Max(1, entry.multiplier);
                for (int c = 0; c < copies; c++) yield return entry.itemPrefab;
            }
        }

        private void Spawn(Item prefab)
        {
            var item = CreateInstance(prefab);
            var t = item.transform;
            t.SetParent(transform);
            t.position = RandomPointInZone();
            t.rotation = Quaternion.Euler(Random.onUnitSphere * 360f);
            _activeItems.Add(item);
        }

        private static Item CreateInstance(Item prefab)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject)).GetComponent<Item>();
#endif
            return ItemPoolManager.Instance.GetItem(prefab);
        }

        private void Despawn()
        {
            foreach (var item in _activeItems)
            {
                if (!item) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) { DestroyImmediate(item.gameObject); continue; }
#endif
                ItemPoolManager.Instance.ReleaseItem(item);
            }
            _activeItems.Clear();
        }

        private Vector3 RandomPointInZone()
        {
            Vector3 half = spawnZone.size * 0.5f;
            float x = Random.Range(-half.x, half.x);
            float y = Random.Range(-half.y, half.y);
            float z = Random.Range(-half.z, half.z);
            return transform.TransformPoint(spawnZone.center + new Vector3(x, y, z));
        }

#if UNITY_EDITOR
        [Header("Editor Preview")]
        [Tooltip("Assign a LevelDataSO here to preview how items will spawn in the editor.")]
        [SerializeField] private LevelDataSO previewData;

        [Button("Preview Spawn")]
        private void PreviewSpawn()
        {
            if (previewData) Initialize(previewData);
            else Debug.LogWarning("ItemPlacer: Assign a LevelDataSO to Preview Data first.");
        }

        /// <summary>Used by LevelEditorWindow; takes the data directly so the prefab's previewData stays untouched.</summary>
        public void PreviewSpawnFromEditor(LevelDataSO data) => Initialize(data);
#endif
    }
}
