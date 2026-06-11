using System.Collections.Generic;
using UnityEngine;
using Match_Them_All.Scripts.Power_Ups;

namespace MatchThemAll.Scripts.Tutorial
{
    /// <summary>
    /// How the tutorial manager decides which objects to highlight.
    /// </summary>
    public enum EHighlightTarget
    {
        [Tooltip("Auto-detect any 3 identical items in the scene.")]
        AutoFindItems,
        
        [Tooltip("Highlight exactly 3 items that match a specific EItemName.")]
        SpecificItem,
        
        [Tooltip("Highlight the powerup slot that matches a specific EPowerupType.")]
        Powerup,
        
        [Tooltip("Highlight the goal card that matches a specific EItemName.")]
        GoalCard,
        
        [Tooltip("Highlight an arbitrary list of GameObjects you drag in.")]
        Manual
    }

    /// <summary>
    /// What event ends this tutorial step.
    /// </summary>
    public enum ECompletionCondition
    {
        [Tooltip("Step ends when the highlighted items merge.")]
        OnMerge,
        
        [Tooltip("Step ends when the highlighted powerup fires its Started event.")]
        OnPowerupUsed,
        
        [Tooltip("Step ends automatically after the autoCompleteDelay timer finishes.")]
        OnTimer,
        
        [Tooltip("Step never ends on its own — call TutorialManager.Instance.CompleteCurrentStep().")]
        Manual
    }

    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Match Them All/Tutorial Step")]
    public class TutorialStepSO : ScriptableObject
    {
        [Header("Message")]
        [Tooltip("Text shown in the tutorial overlay while this step is active.")]
        public string message = "Tap these 3 identical items!";

        [Header("Highlight Target")]
        [Tooltip("Determines which objects will be highlighted / moved to the Tutorial layer.")]
        public EHighlightTarget highlightTarget = EHighlightTarget.AutoFindItems;

        [Tooltip("(SpecificItem / GoalCard) The item type to find and highlight.")]
        public EItemName itemName;

        [Tooltip("(Powerup) The powerup slot to highlight.")]
        public EPowerupType powerupType;

        [Tooltip("(Manual) Drag in any GameObjects to highlight.")]
        public List<GameObject> manualTargets = new List<GameObject>();

        [Header("Completion")]
        [Tooltip("What event causes this step to end and advance to the next.")]
        public ECompletionCondition completionCondition = ECompletionCondition.OnMerge;

        [Tooltip("(OnTimer) Seconds to wait before auto-completing this step.")]
        [Min(0f)]
        public float autoCompleteDelay = 3f;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the level spawns before this step begins. Useful to let items settle.")]
        [Min(0f)]
        public float startDelay = 1.5f;

        [Tooltip("If true, the game timer is paused while this step is active.")]
        public bool pauseTimer = true;
    }
}
