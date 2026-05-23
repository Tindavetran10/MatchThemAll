using UnityEngine;
using MatchThemAll.Scripts;

public static class StaticEventCleaner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Clear()
    {
        // Reset all static Unity events
        LevelManager.LevelSpawned = null;
        InputManager.ItemClicked = null;
        ItemSpotManager.MergeStarted = null;
        ItemSpotManager.ItemPickedUp = null;
    }
}