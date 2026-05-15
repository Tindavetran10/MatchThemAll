using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    public class GoalCard : MonoBehaviour
    {
        [Header(" Elements ")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private GameObject checkMark;
        [SerializeField] private GameObject backFace;
        [SerializeField] private Animator animator;

        // Guard: only run the dot-product check while the flip animation is active
        private bool _isAnimating;

        private void Start() => animator.enabled = false;

        private void Update()
        {
            if (!_isAnimating) return;
            backFace.SetActive(Vector3.Dot(Vector3.forward, transform.forward) < 0);
        }

        public void Configure(int initialAmount, Sprite icon)
        {
            amountText.text = initialAmount.ToString();
            iconImage.sprite = icon;
        }

        public void UpdateAmount(int amount)
        {
            amountText.text = amount.ToString();
            Bump();
        }

        private void Bump()
        {
            LeanTween.cancel(gameObject);
            transform.localScale = Vector3.one;
            LeanTween.scale(gameObject, Vector3.one * 1.1f, 0.25f).setLoopPingPong(1);
        }

        public void Complete()
        {
            _isAnimating = true;
            animator.enabled = true;
            checkMark.SetActive(true);
            amountText.text = "";
            animator.Play("Complete");
        }
    }
}