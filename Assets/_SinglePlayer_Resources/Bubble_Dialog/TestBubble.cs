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
            Debug.LogError("TestBubble: bubbleDialog reference is MISSING! Please drag the BubbleDialog into the Inspector.");
            return;
        }
        Debug.Log("TestBubble: 'Show Bubble' context menu activated.");
        bubbleDialog.SetBubbleActive(true);
        bubbleDialog.SetText(textToTest, transform.position);
    }

    [ContextMenu("Hide Bubble")]
    public void HideBubble()
    {
        if (bubbleDialog != null)
        {
            Debug.Log("TestBubble: 'Hide Bubble' context menu activated.");
            bubbleDialog.SetBubbleActive(false);
        }
    }
}
