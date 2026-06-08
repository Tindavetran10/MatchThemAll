using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MatchThemAll.Scripts
{
    public class MergeManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float goUpDistance;
        [SerializeField] private float goUpDuration;
        [SerializeField] private LeanTweenType goUpEasing;

        [Header("Smash Settings")]
        [SerializeField] private float smashDuration;
        [SerializeField] private LeanTweenType smashEasing;

        [Header("Particles")]
        [SerializeField] private ParticleSystem mergeParticle;
        [SerializeField] private int particlePoolMaxSize = 10;

        // Reuses ParticleSystem instances instead of Instantiate/Destroy each merge
        private IObjectPool<ParticleSystem> _particlePool;

        private void Awake()
        {
            ItemSpotManager.MergeStarted += OnMergeStarted;

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

        private void OnDestroy()
        {
            ItemSpotManager.MergeStarted -= OnMergeStarted;

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

        private void OnMergeStarted(List<Item> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 targetPos = items[i].transform.position + items[i].transform.right * goUpDistance;

                Action callback = null;
                if (i == 0)
                    callback = () => SmashItem(items);

                LeanTween.move(items[i].gameObject, targetPos, goUpDuration)
                    .setEase(goUpEasing)
                    .setOnComplete(callback);
            }
        }

        private void SmashItem(List<Item> items)
        {
            items.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            float targetX = items[1].transform.position.x;

            LeanTween.moveX(items[0].gameObject, targetX, smashDuration)
                .setEase(smashEasing)
                .setOnComplete(() => FinalizeMerge(items));

            LeanTween.moveX(items[2].gameObject, targetX, smashDuration)
                .setEase(smashEasing);

            LeanTween.moveY(items[1].gameObject, items[1].transform.position.y + 0.1f, smashDuration * 0.5f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1);
        }

        private void FinalizeMerge(List<Item> items)
        {
            // Cache position before destroying items
            Vector3 mergePosition = items[1].transform.position;

            foreach (var item in items)
                Destroy(item.gameObject);

            // Get a pooled particle instead of Instantiating a new one
            ParticleSystem ps = _particlePool.Get();
            ps.transform.position = mergePosition;
            if (InputManager.IsTutorialActive)
            {
                ps.gameObject.layer = LayerMask.NameToLayer("Tutorial");
            }
            else
            {
                ps.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            ps.Play();

            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnParticleToPool(ps, lifetime));
        }

        private IEnumerator ReturnParticleToPool(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);

            // The scene may have reloaded and destroyed this object while the coroutine was waiting.
            // Unity's == null check correctly returns true for destroyed UnityEngine.Objects.
            if (ps != null)
                _particlePool.Release(ps);
        }
    }
}