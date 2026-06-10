using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float animationDuration = 0.12f;
    [SerializeField] private float entranceDelay;
    [SerializeField] private float entranceDuration = 0.35f;

    public void SetEntranceDelay(float delay) => entranceDelay = delay;

    private RectTransform _rectTransform;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _scaleRoutine;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _baseScale = _rectTransform.localScale;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = _baseScale * 0.85f;
    }

    private void Start()
    {
        StartCoroutine(PlayEntrance());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateTo(_baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateTo(_baseScale);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(_baseScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(_baseScale * hoverScale);
    }

    private IEnumerator PlayEntrance()
    {
        if (entranceDelay > 0f)
            yield return new WaitForSeconds(entranceDelay);

        float elapsed = 0f;
        Vector3 startScale = _rectTransform.localScale;
        float startAlpha = _canvasGroup.alpha;

        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceDuration);
            _rectTransform.localScale = Vector3.Lerp(startScale, _baseScale, t);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        _rectTransform.localScale = _baseScale;
        _canvasGroup.alpha = 1f;
    }

    private void AnimateTo(Vector3 targetScale)
    {
        if (_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);

        _scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = _rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        _rectTransform.localScale = targetScale;
        _scaleRoutine = null;
    }
}
