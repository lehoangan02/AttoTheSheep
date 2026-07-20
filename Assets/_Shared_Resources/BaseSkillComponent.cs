using UnityEngine;
using System.Collections;
using Unity.Netcode;

public abstract class BaseSkillComponent : NetworkBehaviour
{
    public abstract void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null);

    public virtual void ClientPlayVisual(SkillData data)
    {
        StartCoroutine(PlayVisualSequenceRoutine(data));
    }

    protected virtual IEnumerator PlayVisualSequenceRoutine(SkillData data)
    {

        ClientPlayCastEffect(data);

        float waitTime = 0f;

        if (data.castSFX.HasAudio())
        {

            waitTime = data.castSFX.duration > 0f ? data.castSFX.duration : data.castSFX.clip.length;
        }

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        ClientPlayActiveEffect(data);
    }

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