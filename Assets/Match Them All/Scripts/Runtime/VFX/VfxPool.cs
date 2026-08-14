using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// General-purpose pooled ParticleSystem spawner. One pool per source prefab,
    /// keyed by the prefab so different prefabs never share instances.
    /// Generalizes the MergeManager particle-pool pattern to "any prefab, any position".
    ///
    /// ponytail: no pre-warm (powerups fire less often than merges; first-call
    /// Instantiate is acceptable). Add pre-warm if a first-use spike shows in the Profiler.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        public static VfxPool Instance { get; private set; }

        private readonly Dictionary<GameObject, IObjectPool<GameObject>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            // Stop pending return-to-pool coroutines so none fire against destroyed objects on scene unload.
            StopAllCoroutines();
        }

        /// <summary>
        /// Spawns prefab at pos (world space), sets layer, plays, returns to pool after lifetime.
        /// Null-safe: no prefab = no-op.
        /// The prefab reference may point at ANY ParticleSystem inside an effect prefab —
        /// the whole prefab root is pooled and every system in it plays (asset-pack
        /// VFX are commonly multi-system).
        /// </summary>
        public void Play(ParticleSystem prefab, Vector3 pos)
        {
            if (prefab == null) return;

            var pool = GetOrCreatePool(prefab.transform.root.gameObject);
            GameObject instance = pool.Get();
            instance.transform.position = pos;

            int layer = LayerMask.NameToLayer(InputManager.IsTutorialActive ? "Tutorial" : "Default");
            float lifetime = 0f;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                ps.Play();
                lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            }

            StartCoroutine(ReturnToPool(pool, instance, lifetime));
        }

        private IObjectPool<GameObject> GetOrCreatePool(GameObject prefabRoot)
        {
            if (_pools.TryGetValue(prefabRoot, out var pool)) return pool;

            pool = new ObjectPool<GameObject>(
                createFunc:      () => Instantiate(prefabRoot, transform),
                actionOnGet:     go => go.SetActive(true),
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go => Destroy(go),
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize:         16
            );
            _pools[prefabRoot] = pool;
            return pool;
        }

        private IEnumerator ReturnToPool(IObjectPool<GameObject> pool, GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            // Scene may have reloaded and destroyed this object while waiting.
            if (go) pool.Release(go);
        }
    }
}
