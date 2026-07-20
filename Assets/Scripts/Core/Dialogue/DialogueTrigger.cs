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

    [Tooltip("Event triggered when this dialogue finishes playing.")]
    public UnityEngine.Events.UnityEvent onDialogueEnded;

    [Header("Trigger Settings")]
    [Tooltip("If true, the player must also be inside this object's collider to interact.")]
    public bool requireProximity = true;

    [Header("Proximity Events (Optional)")]
    [Tooltip("Fired when the player enters the trigger collider.")]
    public UnityEngine.Events.UnityEvent onPlayerEnterRange;

    [Tooltip("Fired when the player exits the trigger collider.")]
    public UnityEngine.Events.UnityEvent onPlayerExitRange;

    [Header("Input Action (Keyboard/Gamepad)")]

    [Tooltip("The Input System action that triggers dialogue.")]
    [SerializeField] private InputActionReference interactAction;

    private bool _playerInRange = false;

    private void Start()
    {
        // 1. Auto-assign Main Camera to any child World Space Canvases so they can be clicked
        Canvas[] childCanvases = GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in childCanvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 31000; // Render over mobile joystick (10000) so it's clickable!
            }
        }

        // 2. Auto-link any child UI Buttons to TriggerDialogue() so you don't have to set it manually!
        UnityEngine.UI.Button[] childButtons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in childButtons)
        {
            btn.onClick.AddListener(TriggerDialogue);
        }
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += HandleInteract;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= HandleInteract;
            interactAction.action.Disable();
        }
    }

    private void TryInteract()
    {
        if (requireProximity && !_playerInRange) return;
        TriggerDialogue();
    }

    private void HandleInteract(InputAction.CallbackContext ctx) => TryInteract();

    // Called by collider trigger zone (requires 2D Collider set to "Is Trigger")
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                bool isLocal = true;
                if (pc.NetworkObject != null && pc.NetworkObject.IsSpawned) isLocal = pc.IsOwner;

                if (isLocal)
                {
                    _playerInRange = true;
                    InteractionContext.EnterRange();
                    onPlayerEnterRange?.Invoke();
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                bool isLocal = true;
                if (pc.NetworkObject != null && pc.NetworkObject.IsSpawned) isLocal = pc.IsOwner;

                if (isLocal)
                {
                    _playerInRange = false;
                    InteractionContext.ExitRange();
                    onPlayerExitRange?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Call this from a UI button or any event to open the dialogue.
    /// </summary>
    public void TriggerDialogue()
    {

        if (DialogueManager.Instance != null && dialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueData, () => onDialogueEnded?.Invoke());
        }
        else
        {

        }
    }
}
