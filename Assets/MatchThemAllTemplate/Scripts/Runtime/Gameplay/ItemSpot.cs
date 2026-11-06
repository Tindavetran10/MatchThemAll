using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>One slot of the tray. Holds at most one item and plays a small bump when something lands.</summary>
    public class ItemSpot : MonoBehaviour
    {
        private static readonly int BumpState = Animator.StringToHash("Bump");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _itemParent;

        public Item Item { get; private set; }

        public bool IsEmpty() => Item == null;

        /// <summary>Takes ownership of <paramref name="item"/> and parents it under this slot.</summary>
        public void Populate(Item item)
        {
            Item = item;
            item.AssignSpot(this);
            item.transform.SetParent(_itemParent);
        }

        /// <summary>Forgets the current occupant without touching its GameObject.</summary>
        public void Clear()
        {
            var previous = Item;
            Item = null;
            if (previous && previous.Spot == this) previous.UnassignSpot();
        }

        public void BumpDown() => _animator.Play(BumpState, 0, 0f);
    }
}
