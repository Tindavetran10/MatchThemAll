using System.Collections.Generic;
using Match_Them_All.Scripts.Power_Ups;

namespace MatchThemAll.Scripts
{
    // ==========================================
    // GAME STATE EVENTS
    // ==========================================

    public struct GameStateChangedEvent
    {
        public EGameState NewState;
        public GameStateChangedEvent(EGameState newState) => NewState = newState;
    }

    public struct LevelSpawnedEvent { }

    public struct SpotFilledEvent { }

    // ==========================================
    // ITEM EVENTS
    // ==========================================

    public struct ItemClickedEvent
    {
        public Item ClickedItem;
        public ItemClickedEvent(Item item) => ClickedItem = item;
    }

    public struct ItemPickedUpEvent
    {
        public Item PickedItem;
        public ItemPickedUpEvent(Item item) => PickedItem = item;
    }

    public struct ItemReachedSpotEvent
    {
        public Item Item;
        public ItemReachedSpotEvent(Item item) => Item = item;
    }

    public struct MergeStartedEvent
    {
        public List<Item> MergedItems;
        public MergeStartedEvent(List<Item> items) => MergedItems = items;
    }

    // ==========================================
    // POWERUP EVENTS
    // ==========================================

    public struct PowerupClickedEvent
    {
        public Powerup ClickedPowerup;
        public PowerupClickedEvent(Powerup powerup) => ClickedPowerup = powerup;
    }

    public struct PowerupItemPickedUpEvent
    {
        public Item PickedItem;
        public PowerupItemPickedUpEvent(Item item) => PickedItem = item;
    }

    public struct PowerupItemBackToGameEvent
    {
        public Item ReturnedItem;
        public PowerupItemBackToGameEvent(Item item) => ReturnedItem = item;
    }
}
