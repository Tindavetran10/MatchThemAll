using System;
using System.Collections.Generic;
using UnityEngine;

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

        private void Awake() => ItemSpotManager.MergeStarted += OnMergeStarted;
        private void OnDestroy() => ItemSpotManager.MergeStarted -= OnMergeStarted;

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
            // Sort items from left to right
            items.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            float targetX = items[1].transform.position.x;

            // Move outer items to the center
            LeanTween.moveX(items[0].gameObject, targetX, smashDuration)
                .setEase(smashEasing)
                .setOnComplete(() => FinalizeMerge(items));

            LeanTween.moveX(items[2].gameObject, targetX, smashDuration)
                .setEase(smashEasing);

            // Subtle "bounce" on the middle item
            LeanTween.moveY(items[1].gameObject, items[1].transform.position.y + 0.1f, smashDuration * 0.5f)
                .setEase(LeanTweenType.easeOutQuad)
                .setLoopPingPong(1);
        }

        private void FinalizeMerge(List<Item> items)
        {
            // Cache position before destroying items to avoid accessing a destroyed transform
            Vector3 mergePosition = items[1].transform.position;

            foreach (var item in items)
                Destroy(item.gameObject);

            ParticleSystem particle = Instantiate(mergeParticle, mergePosition, Quaternion.identity, transform);
            particle.Play();

            // Auto-destroy the particle GameObject after it finishes playing
            float lifetime = particle.main.duration + particle.main.startLifetime.constantMax;
            Destroy(particle.gameObject, lifetime);
        }
    }
}