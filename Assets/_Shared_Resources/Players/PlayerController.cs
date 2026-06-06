using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{
    // Các Event (Interface) để các component con cắm vào lấy dữ liệu
    public event Action<Vector2> OnMoveInputChanged;
    public event Action<int> OnSkillActivated; // Trả về ID của Skill (1, 2, 3)
    
    // ĐÂY LÀ DÒNG BỊ THIẾU: Event để truyền tọa độ click chuột cho đàn cừu
    public event Action<Vector2> OnMapClicked; 

    // Nhận Input di chuyển và phát tán Event
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 moveInput = value.Get<Vector2>();
        OnMoveInputChanged?.Invoke(moveInput);
    }

    // Nhận Input Kỹ năng 1 (Ví dụ: Phím J hoặc Click Chuột trái)
    public void OnSkill1(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(1);
    }

    // Nhận Input Kỹ năng 2 (Ví dụ: Phím K hoặc Phím E)
    public void OnSkill2(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(2);
    }

    // Nhận Input Kỹ năng 3 (Ví dụ: Phím L hoặc Phím Q)
    public void OnSkill3(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(3);
    }

    // ĐÂY LÀ HÀM BỊ THIẾU: Nhận Input Click chuột (Yêu cầu phải có Action tên "Click" trong Input Actions)
    public void OnClick(InputValue value)
    {
        // Đặt bẫy ngay cửa ngõ:

        // Nếu không phải chủ phòng, hoặc là hành động nhả chuột ra thì hủy bỏ
        if (!IsOwner || !value.isPressed) return;
        if (Camera.main != null)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            OnMapClicked?.Invoke(mouseWorldPos);
        }
    }
}