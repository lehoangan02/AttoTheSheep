using UnityEngine;

public enum EnemyAudioCueType
{
    Guard,
    Hurt,
    Death
}

[System.Serializable]
public class EnemyAttackAudio
{
    [Tooltip("ID used by the enemy brain, for example Fast, Strong, Left, Right.")]
    public string attackId;
    public AudioCue start;
    public AudioCue hit;
}

[System.Serializable]
public class EnemyAudioSet
{
    [Header("Default Attack")]
    public AudioCue attackStart;
    public AudioCue attackHit;

    [Header("Attack Variants")]
    public EnemyAttackAudio[] attackVariants;

    [Header("Combat")]
    public AudioCue guard;
    public AudioCue hurt;
    public AudioCue death;

    public AudioCue GetCue(EnemyAudioCueType cueType)
    {
        return cueType switch
        {
            EnemyAudioCueType.Guard => guard,
            EnemyAudioCueType.Hurt => hurt,
            EnemyAudioCueType.Death => death,
            _ => null
        };
    }

    public AudioCue GetAttackStartCue(string attackId)
    {
        return GetAttackCue(attackId, false);
    }

    public AudioCue GetAttackHitCue(string attackId)
    {
        return GetAttackCue(attackId, true);
    }

    private AudioCue GetAttackCue(string attackId, bool isHitCue)
    {
        if (!string.IsNullOrWhiteSpace(attackId) && attackVariants != null)
        {
            foreach (EnemyAttackAudio attackAudio in attackVariants)
            {
                if (attackAudio == null) continue;
                if (!string.Equals(attackAudio.attackId, attackId, System.StringComparison.OrdinalIgnoreCase)) continue;

                AudioCue cue = isHitCue ? attackAudio.hit : attackAudio.start;
                if (cue != null && cue.HasAudio())
                    return cue;

                break;
            }
        }

        return isHitCue ? attackHit : attackStart;
    }
}
