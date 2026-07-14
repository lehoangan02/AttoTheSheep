using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private AudioCue[] footstepSFX;
    [SerializeField] private float footstepInterval = 0.35f;

    [Header("Damage")]
    [SerializeField] private AudioCue hurtSFX;
    [SerializeField] private AudioCue deathSFX;

    private float nextFootstepTime;

    public void TryPlayFootstep(Vector2 position)
    {
        if (Time.time < nextFootstepTime) return;

        PlayFootstep(position);
        nextFootstepTime = Time.time + footstepInterval;
    }

    public void PlayFootstep()
    {
        PlayFootstep(transform.position);
    }

    public void PlayFootstep(Vector2 position)
    {
        AudioCue cue = GetRandomFootstep();
        PlayCue(cue, position);
    }

    public void PlayHurt()
    {
        PlayCue(hurtSFX, transform.position);
    }

    public void PlayDeath()
    {
        PlayCue(deathSFX, transform.position);
    }

    private AudioCue GetRandomFootstep()
    {
        if (footstepSFX == null || footstepSFX.Length == 0) return null;

        int startIndex = Random.Range(0, footstepSFX.Length);
        for (int i = 0; i < footstepSFX.Length; i++)
        {
            AudioCue cue = footstepSFX[(startIndex + i) % footstepSFX.Length];
            if (cue != null && cue.HasAudio())
                return cue;
        }

        return null;
    }

    private void PlayCue(AudioCue cue, Vector2 position)
    {
        if (cue == null || !cue.HasAudio() || AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFX_Directional2D(cue.clip, position, cue.duration);
    }
}
