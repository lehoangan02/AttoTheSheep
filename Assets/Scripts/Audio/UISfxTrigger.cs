using UnityEngine;
using UnityEngine.EventSystems;

public class UISfxTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && AudioManager.Instance.defaultUIButtonClickSFX != null)
            AudioManager.Instance.PlaySFX_2D(AudioManager.Instance.defaultUIButtonClickSFX);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && AudioManager.Instance.defaultUIButtonHoverSFX != null)
            AudioManager.Instance.PlaySFX_2D(AudioManager.Instance.defaultUIButtonHoverSFX);
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnPointerClick(null);
    }
}
