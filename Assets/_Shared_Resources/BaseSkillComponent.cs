using UnityEngine;
using Unity.Netcode;

// Lớp trừu tượng định nghĩa "Mọi Kỹ năng đều phải có 2 hàm này"
public abstract class BaseSkillComponent : NetworkBehaviour
{
    // 1. Hàm chạy trên Server (Tính sát thương, vật lý, trừ máu)
    // Truyền vào NetworkEntity để biết ai là người tung chiêu (Atto, Quái, hay Boss)
    public abstract void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null);

    // 2. Hàm chạy trên Client (Bật Particle, Animation, Âm thanh)
    public abstract void ClientPlayVisual(SkillData data);
}