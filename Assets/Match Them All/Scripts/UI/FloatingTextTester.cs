using MatchThemAll.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Testing
{
    /// <summary>
    /// Attach this to a Button to quickly test the FloatingTextSpawner.
    /// Safe to leave in the project; has no effect in production builds if the button is removed.
    /// </summary>
    public class FloatingTextTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testText = "+5s";
        [SerializeField] private Color testColor = Color.yellow;

        private void Start()
        {
            var btn = GetComponent<Button>();
            if (btn)
                btn.onClick.AddListener(TestSpawn);
        }

        private void TestSpawn()
        {
            if (!FloatingTextSpawner.Instance)
            {
                Debug.LogWarning("[FloatingTextTester] FloatingTextSpawner.Instance is null! " +
                    "Make sure FloatingTextSpawner is active in the hierarchy when the game starts.");
                return;
            }

            FloatingTextSpawner.Instance.SpawnAtCenter(testText, testColor);
            Debug.Log("[FloatingTextTester] Spawned floating text.");
        }
    }
}
