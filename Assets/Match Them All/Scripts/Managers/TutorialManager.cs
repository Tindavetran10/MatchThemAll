using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using MatchThemAll.Scripts;

namespace MatchThemAll.Scripts.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance;
        
        [Header("UI")]
        [SerializeField] private CanvasGroup tutorialCanvasGroup;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private string message = "Tap these 3 identical items!";

        private List<Item> _targetItems = new List<Item>();
        private int _itemsClicked = 0;
        private int _originalLayer;
        private int _tutorialLayer;

        private void Awake()
        {
            Instance = this;
            _tutorialLayer = LayerMask.NameToLayer("Tutorial");
            if (_tutorialLayer == -1)
            {
                Debug.LogError("Tutorial layer does not exist! Please create it.");
            }
            
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            LevelManager.LevelSpawned += OnLevelSpawned;
            InputManager.ItemClicked += OnItemClicked;
        }

        private void OnDisable()
        {
            LevelManager.LevelSpawned -= OnLevelSpawned;
            InputManager.ItemClicked -= OnItemClicked;
        }

        private void OnLevelSpawned(Level level)
        {
            // Only run on Level 1
            if (LevelManager.Instance.CurrentLevelIndex == 0)
            {
                StartCoroutine(SetupTutorialDelay());
            }
        }

        private IEnumerator SetupTutorialDelay()
        {
            // Wait for items to drop and settle
            yield return new WaitForSeconds(1.5f);

            var allItems = LevelManager.Instance.Items;
            if (allItems.Count < 3) yield break;

            // Find 3 identical items
            Dictionary<EItemName, List<Item>> groups = new Dictionary<EItemName, List<Item>>();
            foreach (var item in allItems)
            {
                if (!groups.ContainsKey(item.ItemNameKey)) groups[item.ItemNameKey] = new List<Item>();
                groups[item.ItemNameKey].Add(item);
            }

            _targetItems.Clear();
            foreach (var group in groups.Values)
            {
                if (group.Count >= 3)
                {
                    _targetItems.Add(group[0]);
                    _targetItems.Add(group[1]);
                    _targetItems.Add(group[2]);
                    break;
                }
            }

            if (_targetItems.Count < 3) yield break; // Fallback



            StartTutorial();
        }

        private void StartTutorial()
        {
            _itemsClicked = 0;
            InputManager.IsTutorialActive = true;
            InputManager.TutorialTargets = _targetItems.ToArray();
            
            TimerManager.Instance.SetTutorialPause(true);

            // Move targets to Tutorial layer so the Overlay camera sees them
            _originalLayer = _targetItems[0].gameObject.layer;
            foreach (var item in _targetItems)
            {
                SetLayerRecursively(item.gameObject, _tutorialLayer);
            }

            // Show UI
            tutorialText.text = message;
            tutorialCanvasGroup.gameObject.SetActive(true);
            LeanTween.alphaCanvas(tutorialCanvasGroup, 1f, 0.5f).setIgnoreTimeScale(true);
        }

        private void OnItemClicked(Item item)
        {
            if (!InputManager.IsTutorialActive) return;

            if (_targetItems.Contains(item))
            {
                _itemsClicked++;
                
                // Wait until all 3 are clicked before dismissing the tutorial
                if (_itemsClicked >= 3)
                {
                    EndTutorial();
                }
            }
        }

        private void EndTutorial()
        {
            InputManager.IsTutorialActive = false;
            InputManager.TutorialTargets = null;
            TimerManager.Instance.SetTutorialPause(false);

            LeanTween.alphaCanvas(tutorialCanvasGroup, 0f, 0.5f)
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => tutorialCanvasGroup.gameObject.SetActive(false));
                     
            // Wait a bit, then reset the layers in case they didn't get destroyed
            StartCoroutine(ResetLayersDelayed());
        }

        private IEnumerator ResetLayersDelayed()
        {
            yield return new WaitForSeconds(1f);
            foreach (var item in _targetItems)
            {
                if (item != null)
                {
                    SetLayerRecursively(item.gameObject, _originalLayer);
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}