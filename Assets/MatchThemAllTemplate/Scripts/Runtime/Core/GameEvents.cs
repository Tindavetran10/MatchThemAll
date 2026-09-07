using System.Collections.Generic;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.Shop;

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
        public PowerupDataSO Powerup;
        public PowerupClickedEvent(PowerupDataSO powerup) => Powerup = powerup;
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

    // ==========================================
    // SHOP EVENTS
    // ==========================================

    public struct ShopPurchaseSucceededEvent
    {
        public ShopProductSO Product;
        public ShopPurchaseSucceededEvent(ShopProductSO product) => Product = product;
    }
}
