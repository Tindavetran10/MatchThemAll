using System.Collections.Generic;
using ZLinq;
using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts.Managers
{
    public class HintManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float idleThreshold = 5f;
        
        private float _idleTimer;
        private List<Item> _activeHintItems = new();
        private EItemName? _currentHintType;
        
        private void Awake()
        {
            EventBus.Subscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState != EGameState.GAME)
            {
                ResetHint();
            }
        }

        private void OnItemClickedEvent(ItemClickedEvent evt)
        {
            var item = evt.ClickedItem;
            if (_activeHintItems.Count > 0 && _currentHintType.HasValue)
            {
                if (item.ItemNameKey != _currentHintType.Value)
                {
                    // Player clicked a different item, reset the hint
                    ResetHint();
                }
                else
                {
                    // Player clicked the hinted item, just remove it from the list so we don't error out later
                    if (_activeHintItems.Contains(item))
                    {
                        if (!item.IsMovingToSpot)
                        {
                            Tween.StopAll(item.transform);
                            Tween.Scale(item.transform, Vector3.one, 0.1f);
                        }
                        _activeHintItems.Remove(item);
                    }
                }
            }
            
            // Reset idle timer on any click
            _idleTimer = 0f;
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGame()) 
                return;

            // Only track idle timer if we aren't currently hinting something
            if (_activeHintItems.Count == 0)
            {
                bool isActive = InputManager.Instance && InputManager.Instance.IsPointerActive;

                if (isActive)
                {
                    _idleTimer = 0f;
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= idleThreshold)
                    {
                        TriggerHint();
                    }
                }
            }
        }

        private void TriggerHint()
        {
            _activeHintItems = ItemSpotManager.Instance.GetBestHintItems(3);
            if (_activeHintItems == null || _activeHintItems.Count == 0) return;
            
            _currentHintType = _activeHintItems[0].ItemNameKey;

            foreach (var item in _activeHintItems.AsValueEnumerable().Where(item => item && item.gameObject.activeInHierarchy))
            {
                // Apply visual highlight
                if (InputManager.Instance && InputManager.Instance.OutlineMaterial)
                    item.Select(InputManager.Instance.OutlineMaterial);
                    
                // Apply a persistent pulsing animation using a fixed absolute scale to prevent compounding
                Tween.Scale(item.transform, Vector3.one * 1.3f, 0.4f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
            }
        }

        private void ResetHint()
        {
            _idleTimer = 0f;
            _currentHintType = null;
            
            if (_activeHintItems is { Count: > 0 })
            {
                foreach (var item in _activeHintItems.AsValueEnumerable()
                             .Where(item => item && item.gameObject.activeInHierarchy))
                {
                    if (!item.IsMovingToSpot)
                    {
                        Tween.StopAll(item.transform);
                        // Reset scale to normal (assuming local scale is 1, or roughly the original)
                        // It's safer to let the Select/Deselect handle materials, but scale needs to be reset
                        Tween.Scale(item.transform, Vector3.one, 0.1f);
                            
                        if (!item.Spot)
                        {
                            item.Deselect();
                        }
                    }
                }

                _activeHintItems.Clear();
            }
        }
    }
}
