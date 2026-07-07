using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any NPC or interactive object.
/// When the player enters the trigger zone AND presses the interact key,
/// the dialogue panel opens.
///
/// For in-editor testing: you can also call TriggerDialogue() directly
/// from a Button's OnClick event.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Content")]
    [Tooltip("The dialogue asset to play when triggered.")]
    public DialogueData dialogueData;

    [Header("Trigger Settings")]
    [Tooltip("If true, the player must also be inside this object's collider to interact.")]
    public bool requireProximity = true;

    [Tooltip("The Input System action that triggers dialogue.")]
    [SerializeField] private InputActionReference interactAction;

    private bool _playerInRange = false;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += _ => TryInteract();
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= _ => TryInteract();
            interactAction.action.Disable();
        }
    }

    private void TryInteract()
    {
        if (requireProximity && !_playerInRange) return;
        TriggerDialogue();
    }

    // Called by collider trigger zone (requires 2D Collider set to "Is Trigger")
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            InteractionContext.EnterRange();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            InteractionContext.ExitRange();
        }
    }

    /// <summary>
    /// Call this from a UI button or any event to open the dialogue.
    /// </summary>
    public void TriggerDialogue()
    {
        if (DialogueManager.Instance != null && dialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueData);
        }
        else
        {
            Debug.LogWarning("[DialogueTrigger] Missing DialogueManager or DialogueData!");
        }
    }
}
