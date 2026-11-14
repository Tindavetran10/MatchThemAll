using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// One goal on the HUD: item icon + remaining count. On completion the "Complete" animation flips
    /// the card; while it turns, the back face is shown whenever the card faces away from the camera.
    /// </summary>
    public class GoalCard : MonoBehaviour
    {
        [Header("Front")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private GameObject checkMark;

        [Header("Flip")]
        [SerializeField] private GameObject backFace;
        [SerializeField] private Animator animator;

        private bool _flipping;

        private void Start() => animator.enabled = false; // the Animator only runs for the flip

        private void LateUpdate()
        {
            if (_flipping) backFace.SetActive(transform.forward.z < 0f);
        }

        public void Configure(int amount, Sprite icon)
        {
            iconImage.sprite = icon;
            amountText.text = amount.ToString();
        }

        public void UpdateAmount(int amount)
        {
            amountText.text = amount.ToString();
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
            Tween.PunchScale(transform, Vector3.one * 0.15f, 0.35f);
        }

        public void Complete()
        {
            amountText.text = string.Empty;
            checkMark.SetActive(true);
            _flipping = true;
            animator.enabled = true;
            animator.Play("Complete");
        }

        public void Uncomplete()
        {
            _flipping = false;
            animator.enabled = false;
            transform.localRotation = Quaternion.identity;
            checkMark.SetActive(false);
            backFace.SetActive(true);
        }
    }
}
