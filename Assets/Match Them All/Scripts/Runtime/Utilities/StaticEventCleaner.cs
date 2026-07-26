using UnityEngine;
using MatchThemAll.Scripts;

public static class StaticEventCleaner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Clear()
    {
        // Reset all static Unity events
        LevelManager.LevelSpawned = null;
        ItemSpotManager.ItemPickedUp = null;
        InputManager.IsTutorialActive = false;
        InputManager.TutorialTargets = null;
        EventBus.ClearAll();
    }
}