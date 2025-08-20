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
        
        private void Awake() => ItemSpotManager.mergeStarted += OnMergeStarted;
        private void OnDestroy() => ItemSpotManager.mergeStarted -= OnMergeStarted;

        private void OnMergeStarted(List<Item> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 targetPos = items[i].transform.position + items[i].transform.right * goUpDistance;

                Action callback = null;
                
                if(i == 0) 
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
            
            LeanTween.moveX(items[0].gameObject, targetX, smashDuration)
                .setEase(smashEasing)
                .setOnComplete(() => FinalizeMerge(items));

            LeanTween.moveX(items[2].gameObject, targetX, smashDuration)
                .setEase(smashEasing);
        }

        private void FinalizeMerge(List<Item> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                Destroy(item.gameObject);
            }
        }
    }
}