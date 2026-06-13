using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZLinq;
using TMPro;
using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts.Tutorial;
using MatchThemAll.Scripts.UI;

namespace MatchThemAll.Scripts.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance;

        // ─────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────
        [Header("UI")]
        [SerializeField] private CanvasGroup tutorialCanvasGroup;       // TutorialCanvas (dark bg – ScreenSpaceCamera)
        [SerializeField] private CanvasGroup tutorialTextCanvasGroup;   // TutorialTextCanvas (text – ScreenSpaceOverlay)
        [SerializeField] private TextMeshProUGUI tutorialText;

        // ─────────────────────────────────────────────────────────────────
        // Runtime state
        // ─────────────────────────────────────────────────────────────────
        private int _currentStepIndex;
        private TutorialStep _currentStep;
        private List<TutorialStep> _currentSteps;

        // Objects currently on the Tutorial layer
        private readonly List<GameObject> _highlightedObjects = new();
        private int _originalLayer;
        private int _tutorialLayer;

        // Items kept for merge-detection (SpecificItem / AutoFind)
        private readonly List<Item> _targetItems = new();

        // Powerup kept for powerup-used detection
        private Powerup _targetPowerup;

        // Auto-timer handle
        private Coroutine _timerCoroutine;

        // ─────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
            _tutorialLayer = LayerMask.NameToLayer("Tutorial");
            if (_tutorialLayer == -1)
                Debug.LogError("[TutorialManager] 'Tutorial' layer does not exist! Please create it.");

            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.gameObject.SetActive(false);
            if (tutorialTextCanvasGroup != null)
            {
                tutorialTextCanvasGroup.alpha = 0f;
                tutorialTextCanvasGroup.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            LevelManager.LevelSpawned += OnLevelSpawned;
            InputManager.ItemClicked += OnItemClicked;
            InputManager.PowerupClicked += OnPowerupClicked;
            ItemSpotManager.MergeStarted += OnMergeStarted;
        }

        private void OnDisable()
        {
            LevelManager.LevelSpawned -= OnLevelSpawned;
            InputManager.ItemClicked -= OnItemClicked;
            InputManager.PowerupClicked -= OnPowerupClicked;
            ItemSpotManager.MergeStarted -= OnMergeStarted;
        }

        // ─────────────────────────────────────────────────────────────────
        // Step orchestration
        // ─────────────────────────────────────────────────────────────────
        private void OnLevelSpawned(Level level)
        {
            _currentSteps = level.GetTutorialSteps();
            
            if (_currentSteps == null || _currentSteps.Count == 0) return;

            _currentStepIndex = 0;
            StartCoroutine(RunStepDelayed(_currentStepIndex));
        }

        private IEnumerator RunStepDelayed(int index)
        {
            if (_currentSteps == null || index >= _currentSteps.Count) yield break;

            TutorialStep step = _currentSteps[index];
            if (step == null) yield break;

            yield return new WaitForSeconds(step.startDelay);

            List<GameObject> targets = ResolveTargets(step);
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"[TutorialManager] Step {index} found no valid targets – skipping.");
                yield break;
            }

            BeginStep(step, targets);
        }

        // ─────────────────────────────────────────────────────────────────
        // Target resolution
        // ─────────────────────────────────────────────────────────────────
        private List<GameObject> ResolveTargets(TutorialStep step)
        {
            var result = new List<GameObject>();

            switch (step.highlightTarget)
            {
                // ── Auto-find any 3 identical items ──────────────────────
                case EHighlightTarget.AutoFindItems:
                {
                    var allItems = LevelManager.Instance.Items;
                    if (allItems.Count < 3) return null;

                    var groups = new Dictionary<EItemName, List<Item>>();
                    foreach (var item in allItems)
                    {
                        if (!groups.ContainsKey(item.ItemNameKey)) groups[item.ItemNameKey] = new List<Item>();
                        groups[item.ItemNameKey].Add(item);
                    }

                    _targetItems.Clear();
                    foreach (var kvp in groups.AsValueEnumerable().Where(kvp => kvp.Value.Count >= 3))
                    {
                        var g = kvp.Value;
                        _targetItems.Add(g[0]); _targetItems.Add(g[1]); _targetItems.Add(g[2]);
                        break;
                    }

                    if (_targetItems.Count < 3) return null;
                    foreach (var i in _targetItems) result.Add(i.gameObject);
                    break;
                }

                // ── Specific item type ───────────────────────────────────
                case EHighlightTarget.SpecificItem:
                {
                    var allItems = LevelManager.Instance.Items;
                    _targetItems.Clear();
                    foreach (var item in allItems)
                    {
                        if (item.ItemNameKey != step.itemName) continue;
                        _targetItems.Add(item);
                        result.Add(item.gameObject);
                        if (_targetItems.Count >= 3) break;
                    }

                    if (_targetItems.Count < 3)
                    {
                        Debug.LogWarning($"[TutorialManager] SpecificItem: fewer than 3 '{step.itemName}' items found.");
                        return null;
                    }
                    break;
                }

                // ── Powerup slot ─────────────────────────────────────────
                case EHighlightTarget.Powerup:
                {
                    var allPowerups = FindObjectsByType<Powerup>(FindObjectsSortMode.None);
                    _targetPowerup = null;
                    foreach (var p in allPowerups)
                    {
                        if (p.Type != step.powerupType) continue;
                        _targetPowerup = p;
                        break;
                    }

                    if (_targetPowerup == null)
                    {
                        Debug.LogWarning($"[TutorialManager] Powerup: no powerup of type '{step.powerupType}' found.");
                        return null;
                    }
                    result.Add(_targetPowerup.gameObject);
                    break;
                }

                // ── Goal card ────────────────────────────────────────────
                case EHighlightTarget.GoalCard:
                {
                    var goalManager = GoalManager.Instance;
                    if (goalManager == null)
                    {
                        Debug.LogWarning("[TutorialManager] GoalCard: GoalManager.Instance is null.");
                        return null;
                    }

                    var goals = goalManager.Goals;
                    var allCards = FindObjectsByType<GoalCard>(FindObjectsSortMode.None);

                    int targetIndex = -1;
                    for (int i = 0; i < goals.Length; i++)
                    {
                        if (goals[i].itemPrefab.ItemNameKey == step.itemName) { targetIndex = i; break; }
                    }

                    if (targetIndex < 0 || targetIndex >= allCards.Length)
                    {
                        Debug.LogWarning($"[TutorialManager] GoalCard: no card found for '{step.itemName}'.");
                        return null;
                    }
                    result.Add(allCards[targetIndex].gameObject);
                    break;
                }

                // ── Manual list ──────────────────────────────────────────
                case EHighlightTarget.Manual:
                {
                    foreach (var go in step.manualTargets)
                        if (go != null) result.Add(go);

                    if (result.Count == 0)
                    {
                        Debug.LogWarning("[TutorialManager] Manual step has no targets assigned in the Inspector.");
                        return null;
                    }
                    break;
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Step begin
        // ─────────────────────────────────────────────────────────────────
        private void BeginStep(TutorialStep step, List<GameObject> targets)
        {
            _currentStep = step;

            // Move objects to Tutorial layer so the Overlay camera sees them
            _highlightedObjects.Clear();
            _originalLayer = targets[0].layer;
            foreach (var go in targets)
            {
                SetLayerRecursively(go, _tutorialLayer);
                _highlightedObjects.Add(go);
            }

            // Wire InputManager for item-based steps
            if (step.highlightTarget == EHighlightTarget.AutoFindItems ||
                step.highlightTarget == EHighlightTarget.SpecificItem)
            {
                InputManager.IsTutorialActive = true;
                InputManager.TutorialTargets = _targetItems.ToArray();
            }

            // Pause timer
            if (step.pauseTimer)
                TimerManager.Instance.SetTutorialPause(true);

            // Show UI
            tutorialText.text = step.message;
            tutorialCanvasGroup.gameObject.SetActive(true);
            LeanTween.alphaCanvas(tutorialCanvasGroup, 1f, 0.5f).setIgnoreTimeScale(true);
            if (tutorialTextCanvasGroup != null)
            {
                tutorialTextCanvasGroup.gameObject.SetActive(true);
                LeanTween.alphaCanvas(tutorialTextCanvasGroup, 1f, 0.5f).setIgnoreTimeScale(true);
            }

            // Auto-timer
            if (step.completionCondition == ECompletionCondition.OnTimer)
                _timerCoroutine = StartCoroutine(AutoCompleteAfterDelay(step.autoCompleteDelay));
        }

        private IEnumerator AutoCompleteAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            CompleteCurrentStep();
        }

        // ─────────────────────────────────────────────────────────────────
        // Event listeners
        // ─────────────────────────────────────────────────────────────────
        private void OnItemClicked(Item item) { /* merge detection handles completion */ }

        private void OnPowerupClicked(Powerup powerup)
        {
            if (_currentStep == null) return;
            if (_currentStep.completionCondition != ECompletionCondition.OnPowerupUsed) return;
            if (powerup == _targetPowerup)
                CompleteCurrentStep();
        }

        private void OnMergeStarted(List<Item> items)
        {
            if (_currentStep == null) return;
            if (_currentStep.completionCondition != ECompletionCondition.OnMerge) return;

            bool isTutorialMerge = items.AsValueEnumerable().Any(item => _targetItems.Contains(item));
            if (!isTutorialMerge) return;

            CompleteCurrentStep();
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Completes the current step and advances to the next.
        /// Call this when using <see cref="ECompletionCondition.Manual"/>.
        /// </summary>
        private void CompleteCurrentStep()
        {
            if (_currentStep == null) return;

            bool wasTimerPaused = _currentStep.pauseTimer;

            EndStep(wasTimerPaused);

            _currentStepIndex++;
            if (_currentSteps != null && _currentStepIndex < _currentSteps.Count)
                StartCoroutine(RunStepDelayed(_currentStepIndex));
        }

        // ─────────────────────────────────────────────────────────────────
        // Step teardown
        // ─────────────────────────────────────────────────────────────────
        private void EndStep(bool resumeTimer)
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            InputManager.IsTutorialActive = false;
            InputManager.TutorialTargets = null;
            _targetPowerup = null;
            _currentStep = null;

            if (resumeTimer)
                TimerManager.Instance.SetTutorialPause(false);

            // Fade out UI
            LeanTween.alphaCanvas(tutorialCanvasGroup, 0f, 0.5f)
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => tutorialCanvasGroup.gameObject.SetActive(false));

            if (tutorialTextCanvasGroup != null)
            {
                LeanTween.alphaCanvas(tutorialTextCanvasGroup, 0f, 0.5f)
                         .setIgnoreTimeScale(true)
                         .setOnComplete(() => tutorialTextCanvasGroup.gameObject.SetActive(false));
            }

            StartCoroutine(ResetLayersDelayed());
        }

        private IEnumerator ResetLayersDelayed()
        {
            yield return new WaitForSeconds(2f);
            foreach (var go in _highlightedObjects.AsValueEnumerable().Where(go => go != null))
                SetLayerRecursively(go, _originalLayer);
            _highlightedObjects.Clear();
            _targetItems.Clear();
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}