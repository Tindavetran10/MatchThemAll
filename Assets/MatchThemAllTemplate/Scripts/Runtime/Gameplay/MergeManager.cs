using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Plays the merge for three matched items: they lift out of the tray, collapse into their
    /// common centre while shrinking, then return to the pool with a particle burst.
    /// </summary>
    public class MergeManager : MonoBehaviour
    {
        [Header("Lift")]
        [Tooltip("How far the items rise out of the tray (along their local X, which faces the camera in the tray).")]
        [SerializeField] private float goUpDistance;
        [SerializeField] private float goUpDuration;
        [SerializeField] private Ease goUpEasing;

        [Header("Collapse")]
        [SerializeField] private float smashDuration;
        [SerializeField] private Ease smashEasing;
        [Tooltip("Scale the items shrink to as they meet.")]
        [SerializeField] private float collapseScale = 0.6f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem mergeParticle;
        [SerializeField] private int particlePoolMaxSize = 10;

        private const int PrewarmCount = 4;
        private IObjectPool<ParticleSystem> _bursts;

        private void Awake()
        {
            _bursts = new ObjectPool<ParticleSystem>(
                () => { var ps = Instantiate(mergeParticle, transform); ps.gameObject.SetActive(false); return ps; },
                ps => ps.gameObject.SetActive(true),
                ps => ps.gameObject.SetActive(false),
                ps => Destroy(ps.gameObject),
                collectionCheck: false, defaultCapacity: PrewarmCount, maxSize: particlePoolMaxSize);

            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);
        }

        private void Start()
        {
            // Instantiate the first few bursts now instead of on the first merges.
            var warm = new ParticleSystem[PrewarmCount];
            for (int i = 0; i < warm.Length; i++) warm[i] = _bursts.Get();
            foreach (var ps in warm) _bursts.Release(ps);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MergeStartedEvent>(OnMergeStarted);
            StopAllCoroutines(); // no pool returns after scene teardown
        }

        private void OnMergeStarted(MergeStartedEvent evt)
        {
            var items = evt.MergedItems;
            for (int i = 0; i < items.Count; i++)
            {
                var t = items[i].transform;
                var lift = Tween.Position(t, t.position + t.right * goUpDistance, goUpDuration, goUpEasing);
                if (i == 0) lift.OnComplete(() => Collapse(items)); // all lifts share a duration; one callback is enough
            }
        }

        private void Collapse(List<Item> items)
        {
            Vector3 centre = Vector3.zero;
            foreach (var item in items) centre += item.transform.position;
            centre /= items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                var t = items[i].transform;
                Tween.Scale(t, t.localScale * collapseScale, smashDuration, smashEasing);
                var move = Tween.Position(t, centre, smashDuration, smashEasing);
                if (i == 0) move.OnComplete(() => Finish(items, centre));
            }
        }

        private void Finish(List<Item> items, Vector3 at)
        {
            foreach (var item in items)
            {
                Tween.StopAll(item.transform);
                ItemPoolManager.Instance.ReleaseItem(item);
            }

            var burst = _bursts.Get();
            burst.transform.position = at;
            burst.gameObject.layer = LayerMask.NameToLayer(InputManager.IsTutorialActive ? "Tutorial" : "Default");
            burst.Play();

            var main = burst.main;
            StartCoroutine(ReleaseAfter(burst, main.duration + main.startLifetime.constantMax));
        }

        private IEnumerator ReleaseAfter(ParticleSystem burst, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (burst) _bursts.Release(burst); // may be gone if the scene reloaded meanwhile
        }
    }
}
