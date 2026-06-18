using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // Thêm thư viện này

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    public GameObject existingEnemy;
    public TextMeshProUGUI tutorialText; 

    private PlayerEntity playerInstance; 

    private void Start()
    {
        if (existingEnemy != null)
        {
            existingEnemy.SetActive(false);
        }

        StartCoroutine(TutorialRoutine());
    }

    private IEnumerator TutorialRoutine()
    {
        if (tutorialText == null)
        {
            Debug.LogError("TutorialManager: Bạn chưa gán TextMeshProUGUI!");
            yield break;
        }

        // PHASE 0: CHỜ PLAYER
        tutorialText.text = "Đang tải màn chơi...";
        yield return new WaitUntil(() => FindAnyObjectByType<PlayerEntity>() != null);
        playerInstance = FindAnyObjectByType<PlayerEntity>();

        // PHASE 1: DI CHUYỂN (Sử dụng Keyboard.current)
        tutorialText.text = "Sử dụng các phím W A S D để di chuyển.";
        yield return new WaitUntil(() => 
            Keyboard.current.wKey.isPressed || 
            Keyboard.current.aKey.isPressed || 
            Keyboard.current.sKey.isPressed || 
            Keyboard.current.dKey.isPressed);
        
        yield return new WaitForSeconds(1.5f);

        // PHASE 2: NHẬN SÁT THƯƠNG
        tutorialText.text = "Cẩn thận! Kẻ địch xuất hiện!";
        existingEnemy.SetActive(true);
        yield return new WaitForSeconds(3f); 

        // PHASE 3: ĐÁNH TRẢ (Sử dụng Keyboard.current)
        tutorialText.text = "Nhấn nút 0 để đánh trả và tiêu diệt kẻ địch!";
        
        // Chờ người chơi nhấn phím 0
        yield return new WaitUntil(() => Keyboard.current.digit0Key.wasPressedThisFrame);
        
        // Sau đó chờ kẻ địch bị tiêu diệt hoặc ẩn đi
        yield return new WaitUntil(() => existingEnemy == null || !existingEnemy.activeSelf);

        // PHASE 4: KẾT THÚC
        tutorialText.text = "Tuyệt vời! Bạn đã hoàn thành bài hướng dẫn.";
        yield return new WaitForSeconds(2.5f);
        
        tutorialText.gameObject.SetActive(false); 
    }
}