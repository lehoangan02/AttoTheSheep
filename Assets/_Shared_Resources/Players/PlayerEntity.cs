using UnityEngine;
using Unity.Netcode;

// KẾ THỪA TỪ LỚP CHUNG: Có sẵn mọi biến máu, tốc độ của NetworkEntity
public class PlayerEntity : NetworkEntity
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Vẫn gọi setup chỉ số gốc của lớp cha
        
        if (IsOwner)
        {
            Debug.Log("Player Entity đã được spawn!");
        }
    }

    // GHI ĐÈ LỚP CHA: Player chết thì báo Game Over thay vì xóa object
    protected override void Die()
    {
        base.Die();
        Debug.Log("Player đã bay màu! Hiện màn hình Game Over...");
        // Logic hồi sinh, trừ tiền, v.v.
    }
}