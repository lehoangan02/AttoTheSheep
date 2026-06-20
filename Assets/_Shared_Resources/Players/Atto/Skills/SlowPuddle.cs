using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SlowPuddle : NetworkBehaviour
{
    private float slowMultiplier;
    private float duration;
    
    private List<GameObject> objectsInside = new List<GameObject>();

    public void Initialize(float multiplier, float time)
    {
        slowMultiplier = multiplier;
        duration = time;
        
        Debug.Log($"🌊 [SlowPuddle] Đã khởi tạo vũng nước! Bán kính/Làm chậm: {multiplier}, Tồn tại: {time}s");

        // Chỉ Server mới có quyền đếm ngược thời gian để hủy vũng nước
        if (IsServer)
        {
            Invoke(nameof(DespawnPuddle), duration);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject target = other.gameObject;
        if (!objectsInside.Contains(target))
        {
            Debug.Log($"🚶‍♂️ [SlowPuddle] VỪA BƯỚC VÀO: {target.name}");
            objectsInside.Add(target);
            ApplySpeedModifier(target, slowMultiplier);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject target = other.gameObject;
        if (objectsInside.Contains(target))
        {
            Debug.Log($"🏃‍♂️ [SlowPuddle] VỪA BƯỚC RA: {target.name}");
            objectsInside.Remove(target);
            ApplySpeedModifier(target, 1f); // Trả lại tốc độ bình thường
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("💥 [SlowPuddle] Vũng nước bốc hơi! Đang xóa debuff cho những ai còn đứng bên trong...");
        
        // Khi vũng nước biến mất, dọn dẹp debuff cho những ai còn đứng trong đó
        foreach (var obj in objectsInside)
        {
            if (obj != null) ApplySpeedModifier(obj, 1f);
        }
        objectsInside.Clear();
    }

    private void DespawnPuddle()
    {
        if (IsServer && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void ApplySpeedModifier(GameObject target, float multiplier)
    {
        // Tìm Rigidbody2D của đối tượng (bắt buộc phải có Rigidbody mới di chuyển được)
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>() ?? target.GetComponentInParent<Rigidbody2D>();
        
        if (targetRb == null) 
        {
            Debug.Log($"⚠️ [SlowPuddle] BỎ QUA: {target.name} vì không tìm thấy Rigidbody2D.");
            return; 
        }

        GameObject rootObj = targetRb.gameObject;
        
        // Kiểm tra xem đối tượng này đã bị gắn script SlowDebuff chưa?
        SlowDebuff debuff = rootObj.GetComponent<SlowDebuff>();

        if (multiplier < 1f) 
        {
            // BƯỚC VÀO VŨNG NƯỚC: Tự động gán script Debuff bằng code
            if (debuff == null) 
            {
                debuff = rootObj.AddComponent<SlowDebuff>(); 
                Debug.Log($"🐌 [SlowPuddle] ĐÃ GẮN script SlowDebuff vào: {rootObj.name}");
            }
            debuff.slowMultiplier = multiplier;
        }
        else 
        {
            // BƯỚC RA NGOÀI: Tự động xóa script Debuff
            if (debuff != null)
            {
                Destroy(debuff);
                Debug.Log($"✨ [SlowPuddle] ĐÃ XÓA script SlowDebuff khỏi: {rootObj.name}");
            }
        }
    }
}