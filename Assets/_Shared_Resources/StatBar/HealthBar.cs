using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image mainFill;   // Kéo thanh MainFill vào đây
    [SerializeField] private Image ghostFill;  // Kéo thanh GhostFill vào đây

    [Header("Settings")]
    [SerializeField] private float shrinkSpeed = 2f; // Tốc độ co rút của thanh ghost

    private float targetFillAmount = 1f;

    void Update()
    {
        // Nếu thanh Ghost đang nhiều hơn thanh máu thực tế (Main)
        if (ghostFill.fillAmount > targetFillAmount)
        {
            // Co rút mượt mà đuổi theo thanh Main bằng Lerp
            ghostFill.fillAmount = Mathf.Lerp(ghostFill.fillAmount, targetFillAmount, Time.deltaTime * shrinkSpeed);
        }
        else
        {
            ghostFill.fillAmount = targetFillAmount;
        }
    }

    /// <summary>
    /// Hàm gọi từ Script Quái Vật khi Quái bị mất máu
    /// </summary>
    public void UpdateHealth(float currentHP, float maxHP)
    {
        // Tránh lỗi chia cho 0 và giới hạn tỷ lệ từ 0 đến 1
        targetFillAmount = Mathf.Clamp01(currentHP / maxHP);
        
        // Thanh máu chính (đỏ tươi) tụt ngay lập tức để người chơi cảm giác có lực sát thương
        mainFill.fillAmount = targetFillAmount;
    }
}