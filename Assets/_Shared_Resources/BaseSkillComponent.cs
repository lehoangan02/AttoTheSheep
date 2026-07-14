using UnityEngine;
using System.Collections; // Nhớ thêm thư viện này để dùng Coroutine
using Unity.Netcode;

public abstract class BaseSkillComponent : NetworkBehaviour
{
    public abstract void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null);

    // Thay đổi ở đây: Gọi Coroutine thay vì chạy trực tiếp
    public virtual void ClientPlayVisual(SkillData data)
    {
        StartCoroutine(PlayVisualSequenceRoutine(data));
    }

    // Coroutine xử lý việc chờ Cast xong mới phát Active
    protected virtual IEnumerator PlayVisualSequenceRoutine(SkillData data)
    {
        // 1. Chạy hiệu ứng Cast trước
        ClientPlayCastEffect(data);

        // 2. Tính toán thời gian cần phải chờ (thời gian của Cast)
        float waitTime = 0f;
        
        if (data.castSFX.HasAudio())
        {
            // Nếu bạn có set duration > 0 trên Inspector thì chờ theo duration
            // Nếu để bằng 0, thì chờ theo độ dài thực tế của file âm thanh (clip.length)
            waitTime = data.castSFX.duration > 0f ? data.castSFX.duration : data.castSFX.clip.length;
        }

        // 3. Dừng lại chờ cho Cast phát xong
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // 4. Hết thời gian chờ, bắt đầu phát Active
        ClientPlayActiveEffect(data);
    }

    // --- CÁC HÀM TIỆN ÍCH DƯỚI NÀY GIỮ NGUYÊN NHƯ CŨ ---

    public virtual void ClientPlayCastEffect(SkillData data)
    {
        if (data.castSFX.HasAudio() && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX_Directional2D(data.castSFX.clip, transform.position, data.castSFX.duration);
        }
    }

    public virtual void ClientPlayActiveEffect(SkillData data)
    {
        if (data.activeSFX.HasAudio() && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX_Directional2D(data.activeSFX.clip, transform.position, data.activeSFX.duration);
        }
    }

    public virtual void ClientPlayHitEffect(SkillData data, Vector2 hitPosition)
    {
        if (data.hitSFX.HasAudio() && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX_Directional2D(data.hitSFX.clip, hitPosition, data.hitSFX.duration);
        }
    }
}