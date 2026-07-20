using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles between two OnScreenButton children (Headbutt vs Interact)
/// based on whether the player is near an interactable NPC.
///
/// The basic-attack (Headbutt) button is shown by default.
/// When <see cref="InteractionContext.AnyInRange"/> is true, the Headbutt
/// button is hidden and the Interact button is shown in its place.
/// </summary>
public class MobileContextButton : MonoBehaviour
{
    [SerializeField]
    private GameObject headbuttButton;

    [SerializeField]
    private GameObject interactButton;

    private void Awake()
    {
        if (headbuttButton == null && interactButton == null)

    }

    private void OnEnable()
    {
        InteractionContext.AnyInRangeChanged += HandleContextChanged;
        HandleContextChanged(InteractionContext.AnyInRange);
    }

    private void OnDisable()
    {
        InteractionContext.AnyInRangeChanged -= HandleContextChanged;
    }

    private void HandleContextChanged(bool anyInRange)
    {
        if (headbuttButton != null) headbuttButton.SetActive(!anyInRange);
        if (interactButton != null) interactButton.SetActive(anyInRange);
    }
}
