using System;
using System.Collections.Generic;
using MatchThemAll.Scripts;
using UnityEngine;
using MatchThemAll.Scripts.Settings;

namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Dependencies handed to a <see cref="PowerupEffect"/> on activation.
    /// Effects reach game state ONLY through this struct — no global-singleton reach-inside —
    /// so they stay decoupled and testable.
    /// </summary>
    public struct PowerupContext
    {
        public List<Item> Items;              // LevelManager.Instance.Items
        public ItemLevelData[] Goals;         // GoalManager.Instance.Goals
        public ItemSpotManager ItemSpots;     // ItemSpotManager.Instance
        public TimerManager Timer;            // TimerManager.Instance
        public GameSettingsSO GameSettings;   // global, non-powerup tuning only
        public Transform VacuumSuckPosition;

        public Action<Item> OnItemPickup;
        public Action<Item> OnItemBackToGame;
        public Action<bool> SetBusy;          // replaces PowerupManager._isBusy mutations
    }
}
