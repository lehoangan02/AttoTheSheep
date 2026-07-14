using System.Collections.Generic;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [System.Serializable]
    public class AudioCueEntry
    {
        public string id;
        public AudioCue cue;
    }

    [SerializeField] private AudioCueEntry[] cues;

    private Dictionary<string, AudioCue> map;

    private void Awake()
    {
        map = new Dictionary<string, AudioCue>(System.StringComparer.OrdinalIgnoreCase);
        if (cues == null) return;

        for (int i = 0; i < cues.Length; i++)
        {
            AudioCueEntry entry = cues[i];
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.id)) continue;

            map[entry.id] = entry.cue;
        }
    }

    public void Play(string cueId)
    {
        Play(cueId, transform.position);
    }

    public void Play(string cueId, Vector2 position)
    {
        if (string.IsNullOrWhiteSpace(cueId)) return;
        if (map == null || !map.TryGetValue(cueId, out AudioCue cue)) return;

        PlayCue(cue, position);
    }

    private void PlayCue(AudioCue cue, Vector2 position)
    {
        if (cue == null || !cue.HasAudio() || AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFX_Directional2D(cue.clip, position, cue.duration);
    }
}
