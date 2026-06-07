using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{
    // Các Event (Interface) để các component con cắm vào lấy dữ liệu
    public event Action<Vector2> OnMoveInputChanged;
    public event Action<int> OnSkillActivated; // Trả về ID của Skill (0, 1, 2, 3)
    public event Action<Vector2> OnMapClicked; 

    // ----------------------------------------------------
    // NHẬN INPUT DI CHUYỂN
    // ----------------------------------------------------
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        Vector2 moveInput = value.Get<Vector2>();
        OnMoveInputChanged?.Invoke(moveInput);
    }

    // ----------------------------------------------------
    // NHẬN INPUT ĐÁNH THƯỜNG (SKILL 0) - Dòng này vừa được thêm!
    // ----------------------------------------------------
    public void OnBasicAttack(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(0); // Gọi SkillSlot số 0
    }

    // ----------------------------------------------------
    // NHẬN INPUT CÁC KỸ NĂNG KHÁC (SKILL 1, 2, 3)
    // ----------------------------------------------------
    public void OnSkill1(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(1); // Gọi SkillSlot số 1 (Lướt)
    }

    public void OnSkill2(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(2); // Gọi SkillSlot số 2 (Đánh rắm)
    }

    public void OnSkill3(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        OnSkillActivated?.Invoke(3); // Gọi SkillSlot số 3 (Chiêu cuối)
    }

    // ----------------------------------------------------
    // NHẬN INPUT CLICK CHUỘT (Cho Bầy Cừu)
    // ----------------------------------------------------
    public void OnClick(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        if (Camera.main != null)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            OnMapClicked?.Invoke(mouseWorldPos);
        }
    }
}