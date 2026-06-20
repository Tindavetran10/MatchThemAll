using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    public class MergeManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float goUpDistance;
        [SerializeField] private float goUpDuration;
        [SerializeField] private Ease goUpEasing;

        [Header("Smash Settings")]
        [SerializeField] private float smashDuration;
        [SerializeField] private Ease smashEasing;

        [Header("Particles")]
        [SerializeField] private ParticleSystem mergeParticle;
        [SerializeField] private int particlePoolMaxSize = 10;

        // Reuses ParticleSystem instances instead of Instantiate/Destroy each merge
        private IObjectPool<ParticleSystem> _particlePool;

        private void Awake()
        {
            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);

            _particlePool = new ObjectPool<ParticleSystem>(
                createFunc:      CreateParticle,
                actionOnGet:     ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize:         particlePoolMaxSize
            );
        }

        private void Start()
        {
            // Pre-warm the pool to avoid Instantiate spikes during the first few merges
            var prewarm = new List<ParticleSystem>(4);
            for (int i = 0; i < 4; i++) prewarm.Add(_particlePool.Get());
            foreach (var ps in prewarm) _particlePool.Release(ps);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MergeStartedEvent>(OnMergeStarted);

            // Stop all pending return-to-pool coroutines so none can fire
            // against already-destroyed objects during scene teardown.
            // The scene unload destroys pooled GameObjects automatically —
            // no explicit pool.Clear() needed.
            StopAllCoroutines();
        }

        private ParticleSystem CreateParticle()
        {
            var ps = Instantiate(mergeParticle, transform);
            ps.gameObject.SetActive(false);
            return ps;
        }

        private void OnMergeStarted(MergeStartedEvent evt)
        {
            var items = evt.MergedItems;
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 targetPos = items[i].transform.position + items[i].transform.right * goUpDistance;

                Action callback = null;
                if (i == 0)
                    callback = () => SmashItem(items);

                var tween = Tween.Position(items[i].transform, targetPos, goUpDuration, goUpEasing);
                if (callback != null) tween.OnComplete(callback);
            }
        }

        private void SmashItem(List<Item> items)
        {
            items.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            float targetX = items[1].transform.position.x;

            Tween.PositionX(items[0].transform, targetX, smashDuration, smashEasing)
                .OnComplete(() => FinalizeMerge(items));

            Tween.PositionX(items[2].transform, targetX, smashDuration, smashEasing);

            Tween.PositionY(items[1].transform, items[1].transform.position.y + 0.1f, smashDuration * 0.5f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        private void FinalizeMerge(List<Item> items)
        {
            // Cache position before destroying items
            Vector3 mergePosition = items[1].transform.position;

            foreach (var item in items)
            {
                Tween.StopAll(item.transform);
                ItemPoolManager.Instance.ReleaseItem(item);
            }

            // Get a pooled particle instead of Instantiating a new one
            ParticleSystem ps = _particlePool.Get();
            ps.transform.position = mergePosition;
            ps.gameObject.layer = LayerMask.NameToLayer(InputManager.IsTutorialActive ? "Tutorial" : "Default");
            ps.Play();

            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnParticleToPool(ps, lifetime));
        }

        private IEnumerator ReturnParticleToPool(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);

            // The scene may have reloaded and destroyed this object while the coroutine was waiting.
            // Unity's == null check correctly returns true for destroyed UnityEngine.Objects.
            if (ps)
                _particlePool.Release(ps);
        }
    }
}