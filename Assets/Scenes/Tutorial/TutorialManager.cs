using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events; 

public enum TutorialCondition
{
    WaitForPlayerSpawn,
    WaitTime,
    WaitKeyPress,
    WaitMouseLeftClick,
    WaitEnemyDefeated,
    WaitPlayerInHealZone
}

[System.Serializable]
public class TutorialStep
{
    [Header("Step Info")]
    public string stepName; 
    
    [TextArea(2, 4)]
    public string instructionText;

    [Header("Actions (Chạy khi bắt đầu bước)")]
    public UnityEvent onStepStart;

    [Header("Completion Condition (Điều kiện qua bước)")]
    public TutorialCondition conditionType;

    public float waitTime = 1.5f;
    public List<Key> acceptedKeys = new List<Key>();
    public GameObject enemyTarget;
}

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    public GameObject existingEnemy;
    public FlockManager flockManager;
    public BubbleDialog instructionBubble;
    public Vector2 bubbleOffset = new Vector2(0f, 2f);

    [Header("Teleport Logic Settings")]
    [Tooltip("Vị trí giấu object ở rất xa khung hình")]
    public Vector3 hiddenPosition = new Vector3(9999f, 9999f, 0f);
    
    // Biến để nhớ vị trí gốc
    private Vector3 originalEnemyPos;
    private Vector3 originalFlockPos;

    [Header("Tutorial Sequence Configuration")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    private PlayerEntity playerInstance;

    private IEnumerator Start()
    {
        // QUAN TRỌNG: FTUE là màn chơi đơn (offline tutorial), cần tự động khởi chạy Host để spawn Player!
        if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Unity.Netcode.NetworkManager.Singleton.StartHost();
        }

        if (instructionBubble != null) instructionBubble.SetBubbleActive(false);

        yield return null;

        // --- ĐỔI LOGIC TẠI ĐÂY ---
        // Lưu vị trí gốc và ném ra xa thay vì dùng SetActive(false)
        if (existingEnemy != null) 
        {
            originalEnemyPos = existingEnemy.transform.position;
            existingEnemy.transform.position = hiddenPosition;
        }
        
        if (flockManager != null) 
        {
            originalFlockPos = flockManager.transform.position;
            flockManager.transform.position = hiddenPosition;
        }

        StartCoroutine(TutorialRoutine());
    }

    private void Update()
    {
        if (instructionBubble != null && instructionBubble.gameObject.activeInHierarchy && playerInstance != null)
        {
            instructionBubble.transform.position = (Vector2)playerInstance.transform.position + bubbleOffset;
        }
    }

    private void ShowInstruction(string message)
    {
        if (instructionBubble == null) return;
        Vector2 showPosition = playerInstance != null ? (Vector2)playerInstance.transform.position + bubbleOffset : Vector2.zero;

        instructionBubble.SetBubbleActive(false);
        instructionBubble.SetBubbleActive(true);
        
        string localizedMessage = AttoTheSheep.Core.LocalizationManager.Instance != null 
            ? AttoTheSheep.Core.LocalizationManager.Instance.GetText(message) 
            : message;

        Debug.Log($"[TutorialManager] Original instruction text: '{localizedMessage}'. IsMobilePlatform: {Application.isMobilePlatform}");

        // Special case: Replace PC controls with mobile controls dynamically if on mobile
        if (Application.isMobilePlatform)
        {
            localizedMessage = System.Text.RegularExpressions.Regex.Replace(localizedMessage, @"\bwasd\b", "the Joystick", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            localizedMessage = System.Text.RegularExpressions.Regex.Replace(localizedMessage, @"\bj\b", "the Attack button", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            localizedMessage = System.Text.RegularExpressions.Regex.Replace(localizedMessage, @"\bleft click\b", "tap the screen", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            localizedMessage = System.Text.RegularExpressions.Regex.Replace(localizedMessage, @"\bleft-click\b", "tap the screen", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        Debug.Log($"[TutorialManager] Processed instruction text: '{localizedMessage}'");

        instructionBubble.SetText(localizedMessage, showPosition);
    }

    private IEnumerator TutorialRoutine()
    {
        if (instructionBubble == null) yield break;

        foreach (TutorialStep step in tutorialSteps)
        {
            // Gọi sự kiện đầu bước (Bạn sẽ gọi hàm Teleport tại đây)
            step.onStepStart?.Invoke();
            ShowInstruction(step.instructionText);

            switch (step.conditionType)
            {
                case TutorialCondition.WaitForPlayerSpawn:
                    yield return new WaitUntil(() => FindAnyObjectByType<PlayerEntity>() != null);
                    playerInstance = FindAnyObjectByType<PlayerEntity>();
                    instructionBubble.transform.position = (Vector2)playerInstance.transform.position + bubbleOffset;
                    break;

                case TutorialCondition.WaitTime:
                    yield return new WaitForSeconds(step.waitTime);
                    break;

                case TutorialCondition.WaitKeyPress:
                    yield return new WaitUntil(() => 
                    {
                        // Cross-platform Input check (Mobile & PC via PlayerInput)
                        if (playerInstance != null)
                        {
                            var playerInput = playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                            if (playerInput != null && playerInput.actions != null)
                            {
                                // If the tutorial asks for WASD, they are waiting for Movement
                                if (step.acceptedKeys.Contains(Key.W) || step.acceptedKeys.Contains(Key.A) || step.acceptedKeys.Contains(Key.UpArrow))
                                {
                                    var moveAction = playerInput.actions["Move"];
                                    if (moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > 0.05f) return true;
                                }
                                // If the tutorial asks for J, they are waiting for Attack/Headbutt
                                if (step.acceptedKeys.Contains(Key.J))
                                {
                                    var attackAction = playerInput.actions["Headbutt"];
                                    if (attackAction != null && attackAction.triggered) return true;
                                }
                            }
                        }

                        // Fallback: Direct physical keyboard check for PC users
                        if (Keyboard.current != null)
                        {
                            foreach (Key key in step.acceptedKeys)
                            {
                                if (Keyboard.current[key].isPressed) return true;
                            }
                        }
                        return false;
                    });
                    break;
                
                case TutorialCondition.WaitMouseLeftClick:
                    yield return new WaitUntil(() => 
                    {
                        // Cross-platform Input check for 'Click'
                        if (playerInstance != null)
                        {
                            var playerInput = playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                            if (playerInput != null && playerInput.actions != null)
                            {
                                var clickAction = playerInput.actions["Click"];
                                // .triggered is the proper way to catch single-frame taps across all Input System versions
                                if (clickAction != null && clickAction.triggered) return true;
                            }
                        }

                        // Fallback: Direct physical device checks
                        if (UnityEngine.InputSystem.Touchscreen.current != null)
                        {
                            if (UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
                        }

                        if (Mouse.current != null)
                        {
                            return Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame;
                        }
                        return false;
                    });
                    break;

                case TutorialCondition.WaitEnemyDefeated:
                    yield return new WaitUntil(() => step.enemyTarget == null || !step.enemyTarget.activeSelf);
                    break;

                case TutorialCondition.WaitPlayerInHealZone:
                    if (flockManager != null && playerInstance != null)
                    {
                        yield return new WaitUntil(() => flockManager.IsPositionInsideHealZone(playerInstance.transform.position));
                    }
                    break;
            }
        }

        if (instructionBubble != null) instructionBubble.SetBubbleActive(false); 
    }

    // --- CÁC HÀM PUBLIC ĐỂ GỌI TRONG UNITY EVENT (INSPECTOR) ---
    
    public void TeleportEnemyBack()
    {
        if (existingEnemy != null) 
        {
            existingEnemy.transform.position = originalEnemyPos;
        }
    }

    public void TeleportFlockBack()
    {
        if (flockManager != null) 
        {
            flockManager.transform.position = originalFlockPos;
        }
    }
}