using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the test button in MainMenu.
/// On click, tells the DialogueManager to show the assigned mock dialogue.
/// This script uses a runtime AddListener in Start() which is always reliable.
/// </summary>
[RequireComponent(typeof(Button))]
public class TestDialogueButton : MonoBehaviour
{
    [Tooltip("The dialogue data to display when this button is clicked.")]
    public DialogueData dialogueData;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenDialogue);
    }

    private void OpenDialogue()
    {

        var mgr = DialogueManager.Instance;
        if (mgr == null)
        {
            // Fallback: search the scene (handles cases where Awake fired out of order)
            mgr = FindFirstObjectByType<DialogueManager>();
            if (mgr == null)
            {

                return;
            }
        }

        if (dialogueData == null)
        {

            return;
        }

        mgr.StartDialogue(dialogueData);
    }
}
