using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events; // Thêm thư viện này để dùng UnityEvent

// Định nghĩa các loại điều kiện để vượt qua một bước hướng dẫn
public enum TutorialCondition
{
    WaitForPlayerSpawn,
    WaitTime,
    WaitKeyPress,
    WaitMouseLeftClick,
    WaitEnemyDefeated,
    WaitPlayerInHealZone
}

// Class chứa dữ liệu của từng bước, có thể chỉnh sửa ngay trên Inspector
[System.Serializable]
public class TutorialStep
{
    [Header("Step Info")]
    [Tooltip("Tên gợi nhớ trên Editor (VD: 'Bước 1 - Di chuyển')")]
    public string stepName; 
    
    [Tooltip("Nội dung chữ sẽ hiển thị trong bong bóng")]
    [TextArea(2, 4)]
    public string instructionText;

    [Header("Actions (Chạy khi bắt đầu bước)")]
    [Tooltip("Sự kiện kích hoạt ngay khi bước này bắt đầu (VD: Kéo thả bật quái, bật cừu)")]
    public UnityEvent onStepStart;

    [Header("Completion Condition (Điều kiện qua bước)")]
    public TutorialCondition conditionType;

    [Tooltip("Thời gian chờ (Nếu chọn điều kiện WaitTime)")]
    public float waitTime = 1.5f;

    [Tooltip("Danh sách các phím được chấp nhận (Chỉ cần bấm 1 trong các phím này là qua bước)")]
    public List<Key> acceptedKeys = new List<Key>();

    [Tooltip("Quái vật cần tiêu diệt (Nếu chọn điều kiện WaitEnemyDefeated)")]
    public GameObject enemyTarget;
}

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo thả quái vật trên Scene vào đây để tắt lúc đầu game")]
    public GameObject existingEnemy; // <--- BẠN THÊM DÒNG NÀY VÀO ĐÂY NHÉ

    [Tooltip("Kéo thả script FlockManager vào đây để dùng cho điều kiện HealZone")]
    public FlockManager flockManager;
    
    [Tooltip("Kéo thả BubbleDialog có sẵn trên Scene vào đây")]
    public BubbleDialog instructionBubble;
    
    [Tooltip("Khoảng cách từ Player đến bong bóng (X, Y)")]
    public Vector2 bubbleOffset = new Vector2(0f, 2f);

    [Header("Tutorial Sequence Configuration")]
    [Tooltip("Thêm và tùy chỉnh các bước hướng dẫn tại đây")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    private PlayerEntity playerInstance;

    private IEnumerator Start()
    {
        // 1. Đảm bảo bong bóng bị ẩn ngay lập tức để không bị lỗi UI
        if (instructionBubble != null) instructionBubble.SetBubbleActive(false);

        // 2. [QUAN TRỌNG] Chờ 1 frame để tất cả các Object trên Scene (Enemy, Flock) 
        // hoàn tất việc biên dịch và chạy xong Awake(), Start(), Spawn() mạng...
        yield return null;

        // 3. Lúc này dữ liệu đã sẵn sàng, ta mới tắt chúng đi để chờ đến đúng bước Tutorial
        if (existingEnemy != null) existingEnemy.SetActive(false);
        if (flockManager != null) flockManager.gameObject.SetActive(false);

        // 4. Bắt đầu kịch bản Tutorial
        StartCoroutine(TutorialRoutine());
    }
        private void Update()
    {
        // Vẫn giữ nguyên logic Tracking Player
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

        // Duyệt qua từng bước đã được setup trên Inspector
        foreach (TutorialStep step in tutorialSteps)
        {
            // 1. Chạy các sự kiện đầu bước (VD: Gọi hàm bật GameObject)
            step.onStepStart?.Invoke();

            // 2. Hiện chữ hướng dẫn
            ShowInstruction(step.instructionText);

            // 3. Chờ điều kiện hoàn thành
            switch (step.conditionType)
            {
                case TutorialCondition.WaitForPlayerSpawn:
                    yield return new WaitUntil(() => FindAnyObjectByType<PlayerEntity>() != null);
                    playerInstance = FindAnyObjectByType<PlayerEntity>();
                    // Dịch bong bóng về ngay sát Player
                    instructionBubble.transform.position = (Vector2)playerInstance.transform.position + bubbleOffset;
                    break;

                case TutorialCondition.WaitTime:
                    yield return new WaitForSeconds(step.waitTime);
                    break;

                case TutorialCondition.WaitKeyPress:
                    // Quét qua toàn bộ danh sách phím được cấu hình
                    yield return new WaitUntil(() => 
                    {
                        foreach (Key key in step.acceptedKeys)
                        {
                            // Dùng isPressed (như code ban đầu của bạn) để bắt phím di chuyển nhạy hơn
                            if (Keyboard.current[key].isPressed) 
                            {
                                return true; // Chỉ cần 1 phím được bấm là thỏa mãn điều kiện
                            }
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

        // Hoàn thành toàn bộ kịch bản, ẩn bong bóng
        if (instructionBubble != null) instructionBubble.SetBubbleActive(false); 
    }
}