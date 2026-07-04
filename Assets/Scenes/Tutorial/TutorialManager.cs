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
        instructionBubble.SetText(message, showPosition);
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
                        foreach (Key key in step.acceptedKeys)
                        {
                            if (Keyboard.current[key].isPressed) return true;
                        }
                        return false;
                    });
                    break;
                
                case TutorialCondition.WaitMouseLeftClick:
                    yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame);
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