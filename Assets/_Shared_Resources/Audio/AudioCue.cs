using UnityEngine;

[System.Serializable]
public class AudioCue
{
    public AudioClip clip;

    [Tooltip("Maximum playback time in seconds. Leave 0 to play the full clip.")]
    public float duration = 0f;

    public bool HasAudio()
    {
        return clip != null;
    }
}
