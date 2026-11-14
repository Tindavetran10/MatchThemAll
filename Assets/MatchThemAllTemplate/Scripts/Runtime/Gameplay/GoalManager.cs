using System.Collections.Generic;
using ZLinq;
using MatchThemAll.Scripts.UI;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Tracks the level's collection goals and their cards. A goal counts down when an item of its
    /// kind is picked up (tap or Vacuum) and counts back up when a power-up returns one to the board.
    /// When every goal reaches zero the level is complete.
    /// </summary>
    public class GoalManager : MonoBehaviour
    {
        public static GoalManager Instance;

        private const int TimeBonusSeconds = 5;

        [Header("Cards")]
        [SerializeField] private Transform goalCardParent;
        [SerializeField] private GoalCard goalCardPrefab;

        /// <summary>Remaining amount per goal (a per-level copy; the level asset is never modified).</summary>
        public ItemLevelData[] Goals { get; private set; }

        private int[] _targets;
        private readonly List<GoalCard> _cards = new();

        #region Lifecycle

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            LevelManager.LevelSpawned += OnLevelSpawned;
            ItemSpotManager.ItemPickedUp += OnItemPickedUp;
        }

        private void Start()
        {
            if (!PowerupManager.Instance) return;
            PowerupManager.Instance.OnItemPickup += OnItemPickedUp;
            PowerupManager.Instance.OnItemBackToGame += OnItemBackToGame;
        }

        private void OnDestroy()
        {
            LevelManager.LevelSpawned -= OnLevelSpawned;
            ItemSpotManager.ItemPickedUp -= OnItemPickedUp;
            if (!PowerupManager.Instance) return;
            PowerupManager.Instance.OnItemPickup -= OnItemPickedUp;
            PowerupManager.Instance.OnItemBackToGame -= OnItemBackToGame;
        }

        #endregion

        private void OnLevelSpawned(Level level)
        {
            Goals = level.GetGoals();
            _targets = new int[Goals.Length];
            for (int i = 0; i < Goals.Length; i++) _targets[i] = Goals[i].amount;
            RebuildCards();
        }

        private void RebuildCards()
        {
            foreach (var card in _cards.AsValueEnumerable().Where(card => card)) Destroy(card.gameObject);
            _cards.Clear();

            foreach (var goal in Goals)
            {
                var card = Instantiate(goalCardPrefab, goalCardParent);
                card.Configure(goal.amount, goal.itemPrefab.Icon);
                _cards.Add(card);
            }
        }

        // Earliest unfinished goal of this kind takes the pickup.
        private void OnItemPickedUp(Item item)
        {
            int g = FindGoal(item.ItemNameKey, earliest: true, i => Goals[i].amount > 0);
            if (g < 0) return;

            int left = --Goals[g].amount;
            if (left > 0) _cards[g].UpdateAmount(left);
            else OnGoalReached(g);
        }

        // Latest goal of this kind that has room takes the item back (undoing the most recent progress).
        private void OnItemBackToGame(Item item)
        {
            int g = FindGoal(item.ItemNameKey, earliest: false, i => Goals[i].amount < _targets[i]);
            if (g < 0) return;

            if (Goals[g].amount == 0) _cards[g].Uncomplete();
            _cards[g].UpdateAmount(++Goals[g].amount);
        }

        private int FindGoal(EItemName kind, bool earliest, System.Predicate<int> accept)
        {
            if (Goals == null) return -1;
            int n = Goals.Length;
            for (int k = 0; k < n; k++)
            {
                int i = earliest ? k : n - 1 - k;
                if (Goals[i].itemPrefab.ItemNameKey == kind && accept(i)) return i;
            }
            return -1;
        }

        private void OnGoalReached(int g)
        {
            SoundManager.Sfx(SoundEvent.GoalComplete);
            _cards[g].Complete();

            if (TimerManager.Instance) TimerManager.Instance.AddTime(TimeBonusSeconds);
            if (FloatingTextSpawner.Instance && _cards[g])
                FloatingTextSpawner.Instance.Spawn($"+{TimeBonusSeconds}s", _cards[g].transform.position, Color.yellow);

            if (Goals.AsValueEnumerable().Any(goal => goal.amount > 0))
            {
                return;
            }
            GameManager.Instance.SetGameState(EGameState.LEVELCOMPLETE);
        }
    }
}
