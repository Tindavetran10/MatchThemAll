using System.Collections.Generic;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.Shop;
using UnityEngine;

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

    /// <summary>A power-up passed its checks, spent a charge and started. Unlike PowerupClickedEvent, ignored taps don't send it.</summary>
    public struct PowerupActivatedEvent
    {
        public PowerupDataSO Powerup;
        public PowerupActivatedEvent(PowerupDataSO powerup) => Powerup = powerup;
    }

    /// <summary>The Freeze power-up stopped the clock for Duration seconds of play time.</summary>
    public struct TimerFrozenEvent
    {
        public float Duration;
        public TimerFrozenEvent(float duration) => Duration = duration;
    }

    /// <summary>The freeze ended (ran out, or the level ended/restarted).</summary>
    public struct TimerUnfrozenEvent { }

    /// <summary>A power-up with 0 charges was tapped: ask the player to pay coins or watch an ad for one.</summary>
    public struct PowerupOfferRequestedEvent
    {
        public PowerupDataSO Powerup;
        public PowerupOfferRequestedEvent(PowerupDataSO powerup) => Powerup = powerup;
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

    // ==========================================
    // CURRENCY EVENTS
    // ==========================================

    /// <summary>
    /// Raised right after coins/gems are added through SaveManager, so the pill can play the fly-to-counter
    /// effect. ScreenPosition is where the burst starts; Delay holds the burst back (the counter waits too).
    /// </summary>
    public struct CurrencyGrantedEvent
    {
        public ECurrency Currency;
        public int Amount;
        public Vector2 ScreenPosition;
        public float Delay;

        public CurrencyGrantedEvent(ECurrency currency, int amount, Vector2 screenPosition, float delay = 0f)
        {
            Currency = currency; Amount = amount; ScreenPosition = screenPosition; Delay = delay;
        }

        /// <summary>Burst from the middle of the screen (grants with no on-screen source, e.g. store purchases).</summary>
        public static Vector2 ScreenCenter => new(Screen.width * 0.5f, Screen.height * 0.5f);
    }
}
