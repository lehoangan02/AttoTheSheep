using System;                                                                                                                                                                                                       
using System.Collections;                                                                                                                                                                                           
using TMPro;                                                                                                                                                                                                        
using UnityEngine;                                                                                                                                                                                                  
                                                                                                                                                                                                                    
/// <summary>
/// A dynamically resizing speech bubble that automatically scales to tightly fit its text.
/// It wraps text to maintain configurable aspect ratios (e.g. 10:1 wide) and stops growing at a defined maximum size.
/// 
/// Setup Instructions:
/// 1. Attach this script to a GameObject.
/// 2. Assign a SpriteRenderer (Ensure its 'Draw Mode' is set to 'Sliced' and it has valid borders).
/// 3. Assign a TextMeshPro component (Alignment should usually be Center/Middle).
/// </summary>
public class BubbleDialog : MonoBehaviour                                                                                                                                                                           
{                                                                                                                                                                                                                   
    [Header("References")]
    [Tooltip("The SpriteRenderer used for the bubble background. MUST be set to 'Sliced' draw mode in its component settings.")]
    [SerializeField] private SpriteRenderer bubbleSprite;
    
    [Tooltip("The TextMeshPro component for the dialogue text. Alignment should be Center/Middle.")]
    [SerializeField] private TextMeshPro textMeshPro;

    [Header("Bubble Sizing Settings")]
    [Tooltip("The minimum allowed shape of the bubble (Width, Height).\n\n" +
             "Example: (1, 1) ensures the bubble is never taller than a perfect square. " +
             "If text is very tall, the box is forced wider to maintain at least a 1:1 shape.")]
    [SerializeField] private Vector2 minRatio = new Vector2(1f, 1f);
    
    [Tooltip("The maximum allowed shape of the bubble (Width, Height).\n\n" +
             "Example: (10, 1) ensures the bubble is never wider than 10x its height. " +
             "If a sentence is too long, it will automatically word-wrap to prevent exceeding this 10:1 shape.")]
    [SerializeField] private Vector2 maxRatio = new Vector2(10f, 1f);
    
    [Tooltip("The absolute maximum physical dimensions (Width, Height) the bubble can grow to.\n\n" +
             "If the dialogue requires more space than this limit, the extra text will be cleanly truncated with an ellipsis (...).")]
    [SerializeField] private Vector2 maxSize = new Vector2(30f, 30f);
    
    // Hardcoded padding as requested
    private readonly Vector2 padding = new Vector2(2f, 2f);
                                                                                                                                                                                                                    
    [Header("Animation")]
    [Tooltip("How long it takes to pop in or out.")]
    [SerializeField] private float animationDuration = 0.25f;                                                                                                                       
                                                                                                                                                                                                                    
    [Header("Typing Effect")]                                                                                                                                                                                       
    [SerializeField] private float typeSpeed = 0.03f;                                                                                                                                                               
                                                                                                                                                                                                                    
    private Coroutine typingCoroutine;                                                                                                                                                                              
    private Coroutine animationCoroutine;                                                                                                                                                                           
    private string currentText = "";                                                                                                                                                                                
                                                                                                                                                                                                                    
    private void Awake()                                                                                                                                                                                            
    {                                                                                                                                                                                                               
        // Ensure we start hidden with scale 0                                                                                                                                                                      
        transform.localScale = Vector3.zero;                                                                                                                                                                        
        gameObject.SetActive(false);
        
        // Force rendering on top of level elements and mobile UI (if they share the same camera/sorting space)
        if (bubbleSprite != null)
        {
            bubbleSprite.sortingLayerName = "UI"; // Default high layer in most projects
            bubbleSprite.sortingOrder = 32000;
        }
        if (textMeshPro != null)
        {
            var textRenderer = textMeshPro.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerName = "UI";
                textRenderer.sortingOrder = 32001; // Text must be exactly above the sprite
            }
        }
    }                                                                                                                                                                                                               
                                                                                                                                                                                                                    
    /// <summary>
    /// Sets the dialogue text, immediately resizes the bubble to fit tightly, aligns the center-left to a specific position, and begins typing.
    /// Note: Ensure the bubble is activated via SetBubbleActive(true) before calling this.
    /// </summary>
    /// <param name="text">The string to display.</param>
    /// <param name="position">The Vector2 world position where the center-left edge of the bubble should be placed.</param>
    public void SetText(string text, Vector2 position)                                                                                                                                                                                
    {                                                                                                                                                                                                               
        Debug.Log($"BubbleDialog: Setting text to '{text}' at position {position}");
        currentText = text;                                                                                                                                                                                         
                                                                                                                                                                                                                    
        // Hide characters initially to prevent flickering before the animation/typing starts                                                                                                                       
        textMeshPro.maxVisibleCharacters = 0;                                                                                                                                                                       
                                                                                                                                                                                                                    
        Vector2 finalSize = ResizeBubble(text);
        
        // Align the center-left of the bubble to the given position
        // By placing the parent exactly at the target position and shifting the children RIGHT by half the width,
        // the bubble's left edge stays anchored exactly at the target position even during the scaling animation!
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        
        bubbleSprite.transform.localPosition = new Vector3(finalSize.x / 2f, 0f, 0f);
        textMeshPro.transform.localPosition = new Vector3(finalSize.x / 2f, 0f, 0f);                                                                                                                                                                                         
                                                                                                                                                                                                                    
        // If the bubble is already on-screen, restart the typewriter effect immediately                                                                                                                            
        if (gameObject.activeInHierarchy)                                                                                                                                                                           
        {                                                                                                                                                                                                           
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);                                                                                                                                            
            typingCoroutine = StartCoroutine(TypeTextCoroutine(text));                                                                                                                                              
        }                                                                                                                                                                                                           
    }                                                                                                                                                                                                               
                                                                                                                                                                                                                    
    /// <summary>
    /// Activates or deactivates the speech bubble with a smooth scaling animation.
    /// </summary>
    /// <param name="active">True to show the bubble, false to hide it.</param>
    public void SetBubbleActive(bool active)                                                                                                                                                                        
    {                                                                                                                                                                                                               
        Debug.Log($"BubbleDialog: SetBubbleActive({active}) called.");
        if (active)                                                                                                                                                                                                 
        {                                                                                                                                                                                                           
            gameObject.SetActive(true);                                                                                                                                                                             
                                                                                                                                                                                                                    
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);                                                                                                                                      
                                                                                                                                                                                                                    
            // Animate scale up using the new poppy math algorithm
            animationCoroutine = StartCoroutine(ScaleAnimation(transform.localScale, Vector3.one, null));                                                                                                               
                                                                                                                                                                                                                    
            // Start typing effect when activated                                                                                                                                                                   
            if (!string.IsNullOrEmpty(currentText))                                                                                                                                                                 
            {                                                                                                                                                                                                       
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);                                                                                                                                        
                typingCoroutine = StartCoroutine(TypeTextCoroutine(currentText));                                                                                                                                   
            }                                                                                                                                                                                                       
        }                                                                                                                                                                                                           
        else                                                                                                                                                                                                        
        {                                                                                                                                                                                                           
            if (gameObject.activeInHierarchy)                                                                                                                                                                       
            {                                                                                                                                                                                                       
                if (animationCoroutine != null) StopCoroutine(animationCoroutine);                                                                                                                                  
                                                                                                                                                                                                                    
                // Animate scale down, then deactivate the gameObject once it hits 0
                animationCoroutine = StartCoroutine(ScaleAnimation(transform.localScale, Vector3.zero, () => gameObject.SetActive(false)));                                                                             
            }                                                                                                                                                                                                           
        }                                                                                                                                                                                                           
    }                                                                                                                                                                                                               
                                                                                                                                                                                                                    
    /// <summary>
    /// Core logic that calculates the optimal bounding box for the text.
    /// It enforces ratio limits (preventing it from being too thin or too tall), 
    /// applies hardcoded padding, and clamps to the absolute maxSize.
    /// </summary>
    /// <returns>The final calculated size (width and height) of the bubble.</returns>
    private Vector2 ResizeBubble(string text)
    {
        // Calculate float ratios from the Vector2 inputs
        float minRatioValue = minRatio.x / minRatio.y;
        float maxRatioValue = maxRatio.x / maxRatio.y;

        // Ensure min and max are safely sorted
        float safeMinRatio = Mathf.Min(minRatioValue, maxRatioValue);
        float safeMaxRatio = Mathf.Max(minRatioValue, maxRatioValue);
        
        int originalVisible = textMeshPro.maxVisibleCharacters;
        textMeshPro.maxVisibleCharacters = 99999;
        
        // 1. Reset constraints to measure text
        textMeshPro.overflowMode = TextOverflowModes.Overflow; 
        textMeshPro.enableWordWrapping = true; 
        textMeshPro.margin = Vector4.zero; // Remove margin temporarily to get accurate raw size
        
        textMeshPro.rectTransform.sizeDelta = new Vector2(10000, 10000);
        textMeshPro.text = text;
        textMeshPro.ForceMeshUpdate();

        if (string.IsNullOrEmpty(text))
        {
            bubbleSprite.size = Vector2.zero;
            textMeshPro.maxVisibleCharacters = originalVisible;
            return Vector2.zero;
        }

        Vector2 rawSize = new Vector2(textMeshPro.preferredWidth, textMeshPro.preferredHeight);
        float currentRatio = rawSize.x / rawSize.y;
        float targetTextWidth = rawSize.x;

        // 2. If it's too wide, calculate width to hit our max ratio
        if (currentRatio > safeMaxRatio)
        {
            float area = rawSize.x * rawSize.y;
            targetTextWidth = Mathf.Sqrt(area * safeMaxRatio);
        }
        // 3. If it's too tall, calculate width to hit our min ratio
        else if (currentRatio < safeMinRatio)
        {
            float area = rawSize.x * rawSize.y;
            targetTextWidth = Mathf.Sqrt(area * safeMinRatio);
        }

        // CRITICAL FIX: Clamp the target width BEFORE wrapping! 
        // We subtract padding.x because the targetTextWidth is for the TEXT, and the box has padding.
        targetTextWidth = Mathf.Min(targetTextWidth, maxSize.x - padding.x);

        // Apply width to wrap text accurately. No margin is added here because margin is zeroed out.
        textMeshPro.rectTransform.sizeDelta = new Vector2(targetTextWidth, 10000);
        textMeshPro.ForceMeshUpdate();

        // Get the final layout size needed after wrapping
        Vector2 finalTextSize = new Vector2(textMeshPro.textBounds.size.x, textMeshPro.preferredHeight);
        
        // Final bubble size adds our hardcoded padding safely
        Vector2 finalBubbleSize = new Vector2(finalTextSize.x + padding.x, finalTextSize.y + padding.y);

        finalBubbleSize.x = Mathf.Min(finalBubbleSize.x, maxSize.x);
        finalBubbleSize.y = Mathf.Min(finalBubbleSize.y, maxSize.y);

        bubbleSprite.size = finalBubbleSize;
        textMeshPro.rectTransform.sizeDelta = finalBubbleSize;
        
        // Push the text inward using TextMeshPro Margins so the padding works perfectly!
        textMeshPro.margin = new Vector4(padding.x / 2f, padding.y / 2f, padding.x / 2f, padding.y / 2f);
        
        textMeshPro.overflowMode = TextOverflowModes.Ellipsis;
        textMeshPro.ForceMeshUpdate();
        
        textMeshPro.maxVisibleCharacters = originalVisible;
        
        return finalBubbleSize;
    }
                                                                                                                                                                                                                    
    private IEnumerator TypeTextCoroutine(string text)                                                                                                                                                              
    {                                                                                                                                                                                                               
        textMeshPro.text = text;                                                                                                                                                                                    
        textMeshPro.ForceMeshUpdate();                                                                                                                                                                              
                                                                                                                                                                                                                    
        int totalChars = textMeshPro.textInfo.characterCount;                                                                                                                                                       
                                                                                                                                                                                                                    
        // Fallback for instant typing                                                                                                                                                                              
        if (typeSpeed <= 0f)                                                                                                                                                                                        
        {                                                                                                                                                                                                           
            textMeshPro.maxVisibleCharacters = totalChars;                                                                                                                                                          
            yield break;                                                                                                                                                                                            
        }                                                                                                                                                                                                           
                                                                                                                                                                                                                    
        textMeshPro.maxVisibleCharacters = 0;                                                                                                                                                                       
                                                                                                                                                                                                                    
        for (int i = 0; i <= totalChars; i++)                                                                                                                                                                       
        {                                                                                                                                                                                                           
            textMeshPro.maxVisibleCharacters = i;                                                                                                                                                                   
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    /// <summary>
    /// Coroutine to smoothly animate the scale of the bubble for popping in/out.
    /// Uses a bouncy "Overshoot" easing for a more energetic, poppy feel.
    /// </summary>
    private IEnumerator ScaleAnimation(Vector3 startScale, Vector3 endScale, Action onComplete = null)
    {
        float elapsed = 0f;
        float overshoot = 1.70158f; // Standard constant for a nice poppy bounce

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = Mathf.Min(t, 1f); // Ensure math doesn't break on the last frame

            float easeT;
            if (endScale.sqrMagnitude > startScale.sqrMagnitude)
            {
                // Pop In: EaseOutBack (Overshoots past the target scale, then settles back down)
                float t2 = t - 1f;
                easeT = 1f + (overshoot + 1f) * (t2 * t2 * t2) + overshoot * (t2 * t2);
            }
            else
            {
                // Pop Out: EaseInBack (Anticipates by inflating slightly before shrinking to 0)
                easeT = (overshoot + 1f) * (t * t * t) - overshoot * (t * t);
            }

            // CRITICAL: Use LerpUnclamped so the scale can physically exceed the 1.0 limit to bounce!
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, easeT);
            yield return null;
        }

        transform.localScale = endScale;
        onComplete?.Invoke();
    }
}