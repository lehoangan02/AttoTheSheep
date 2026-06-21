using UnityEngine;
using UnityEngine.EventSystems;
// Removed unused DG.Tweening
using System.Collections;

namespace AttoTheSheep.UI
{
    /// <summary>
    /// Adds a juice/animation effect to buttons on hover and click.
    /// Follows the Open/Closed Principle: We can add this component to any button without modifying the button's core logic.
    /// </summary>
    public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScaleMultiplier = 1.1f;
        [SerializeField] private float clickScaleMultiplier = 0.95f;
        [SerializeField] private float animationDuration = 0.1f;

        private Vector3 _originalScale;
        private Coroutine _currentAnimation;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AnimateToScale(_originalScale * hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateToScale(_originalScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateToScale(_originalScale * clickScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // If we are still hovering after click, go to hover scale, otherwise original scale.
            // Simplified: return to hover scale.
            AnimateToScale(_originalScale * hoverScaleMultiplier);
        }

        private void AnimateToScale(Vector3 targetScale)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
            _currentAnimation = StartCoroutine(ScaleCoroutine(targetScale));
        }

        private IEnumerator ScaleCoroutine(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                
                // Simple ease-out
                t = 1f - Mathf.Pow(1f - t, 3f);

                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void OnDisable()
        {
            // Reset scale if disabled while animating
            transform.localScale = _originalScale;
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
        }
    }
}
