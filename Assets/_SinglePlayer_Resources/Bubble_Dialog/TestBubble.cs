using UnityEngine;

public class TestBubble : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject that has the SpeechBubble script here.")]
    public BubbleDialog bubbleDialog;

    [Header("Test Settings")]
    [Tooltip("The text that will appear when you press F.")]
    [TextArea(10, 5)]
    public string textToTest = "Hello! This is a dynamically resizing speech bubble. It should clamp the ratio properly and type out smoothly.";

    [ContextMenu("Show Bubble")]
    public void ShowBubble()
    {
        if (bubbleDialog == null)
        {

            return;
        }

        bubbleDialog.SetBubbleActive(true);
        bubbleDialog.SetText(textToTest, transform.position);
    }

    [ContextMenu("Hide Bubble")]
    public void HideBubble()
    {
        if (bubbleDialog != null)
        {

            bubbleDialog.SetBubbleActive(false);
        }
    }
}
