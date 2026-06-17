using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine; // [QUAN TRỌNG] Đổi thành Unity.Cinemachine cho bản mới

public class PlayerEntity : NetworkEntity
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); 
        
        if (IsOwner)
        {
            Debug.Log("Player Entity has been spawned!");
            SetupVirtualCamera();
        }
    }

    private void SetupVirtualCamera()
    {
        // [CẬP NHẬT] Tìm CinemachineCamera thay vì CinemachineVirtualCamera
        CinemachineCamera vCam = FindAnyObjectByType<CinemachineCamera>();

        if (vCam != null)
        {
            // Gán bản thân vào mục Tracking Target
            vCam.Follow = this.transform; 
            
            Debug.Log("🎥 [Camera] Đã setup Cinemachine focus vào Local Player!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Camera] Không tìm thấy CinemachineCamera nào trong Scene!");
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player has been defeated! Showing Game Over screen...");
    }
}