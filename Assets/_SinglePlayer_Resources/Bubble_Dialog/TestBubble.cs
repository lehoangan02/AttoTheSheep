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

    void Update()
    {
        // Press F to Activate and Show Text
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (bubbleDialog == null)
            {
                Debug.LogError("TestBubble: bubbleDialog reference is MISSING! Please drag the BubbleDialog into the Inspector.");
                return;
            }
            
            Debug.Log("TestBubble: 'F' pressed. Activating bubble...");
            
            // Activate the bubble FIRST so TextMeshPro is awake and can calculate sizes
            bubbleDialog.SetBubbleActive(true);
            
            // Then set the text (it will resize perfectly since scale is 0 at the start of the animation)
            // We pass this object's transform.position so the bubble's center-left edge aligns to this test object
            bubbleDialog.SetText(textToTest, transform.position);
        }

        // Press J to Deactivate
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (bubbleDialog != null)
            {
                Debug.Log("TestBubble: 'J' pressed. Deactivating bubble...");
                bubbleDialog.SetBubbleActive(false);
            }
        }
    }
}
