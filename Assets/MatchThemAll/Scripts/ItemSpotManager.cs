using UnityEngine;
using UnityEngine.Serialization;

namespace MatchThemAll.Scripts
{
    public class ItemSpotManager : MonoBehaviour
    {
        [Header("Elements")] 
        [SerializeField] private Transform itemSpots;
        
        [Header("Settings")]
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        [SerializeField] private Vector3 itemLocalScaleOnSpot;
        private void Awake() => InputManager.ItemClicked += OnItemClicked;

        private void OnDestroy() => InputManager.ItemClicked -= OnItemClicked;
        
        private void OnItemClicked(Item item)
        {
            Debug.Log("Item clicked");
            
            // Turn the Item as a child of the item spot
            item.transform.SetParent(itemSpots);
            
            // Scale the item down and set the local position to 0
            item.transform.localPosition = itemLocalPositionOnSpot;
            item.transform.localScale = itemLocalScaleOnSpot;
            
            // Disable its shadow
            item.DisableShadow();

            // Disable its collider / physics
            item.DisablePhysics();
        }
    }
}