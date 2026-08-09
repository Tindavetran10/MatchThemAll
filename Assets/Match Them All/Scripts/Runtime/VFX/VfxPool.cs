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

        private readonly Dictionary<ParticleSystem, IObjectPool<ParticleSystem>> _pools = new();

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
        /// </summary>
        public void Play(ParticleSystem prefab, Vector3 pos)
        {
            if (prefab == null) return;

            var pool = GetOrCreatePool(prefab);
            ParticleSystem ps = pool.Get();
            ps.transform.position = pos;
            ps.gameObject.layer = LayerMask.NameToLayer(InputManager.IsTutorialActive ? "Tutorial" : "Default");

            var main = ps.main;
            ps.Play();

            float lifetime = main.duration + main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(pool, ps, lifetime));
        }

        private IObjectPool<ParticleSystem> GetOrCreatePool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool)) return pool;

            pool = new ObjectPool<ParticleSystem>(
                createFunc:      () => Instantiate(prefab, transform),
                actionOnGet:     ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize:         16
            );
            _pools[prefab] = pool;
            return pool;
        }

        private IEnumerator ReturnToPool(IObjectPool<ParticleSystem> pool, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            // Scene may have reloaded and destroyed this object while waiting.
            if (ps) pool.Release(ps);
        }
    }
}
