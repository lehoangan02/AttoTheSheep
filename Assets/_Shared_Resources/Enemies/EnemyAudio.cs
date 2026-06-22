using UnityEngine;

[RequireComponent(typeof(EnemyEntity))]
public class EnemyAudio : MonoBehaviour
{
    private EnemyEntity entity;

    private void Awake()
    {
        entity = GetComponent<EnemyEntity>();
    }

    public void Play(EnemyAudioCueType cueType)
    {
        Play(cueType, transform.position);
    }

    public void Play(EnemyAudioCueType cueType, Vector2 position)
    {
        AudioCue cue = entity?.Data?.audio?.GetCue(cueType);
        PlayCue(cue, position);
    }

    public void PlayAttackStart(string attackId)
    {
        PlayAttackStart(attackId, transform.position);
    }

    public void PlayAttackStart(string attackId, Vector2 position)
    {
        AudioCue cue = entity?.Data?.audio?.GetAttackStartCue(attackId);
        PlayCue(cue, position);
    }

    public void PlayAttackHit(string attackId, Vector2 position)
    {
        AudioCue cue = entity?.Data?.audio?.GetAttackHitCue(attackId);
        PlayCue(cue, position);
    }

    private void PlayCue(AudioCue cue, Vector2 position)
    {
        if (cue == null || !cue.HasAudio() || AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFX_Directional2D(cue.clip, position, cue.duration);
    }
}
