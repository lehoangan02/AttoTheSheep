using UnityEngine;

public class SceneMusicTrigger : MonoBehaviour
{
    [Tooltip("Select the music you want to play when this scene starts.")]
    public AudioManager.MusicType musicType;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(musicType);
        }
    }
}
