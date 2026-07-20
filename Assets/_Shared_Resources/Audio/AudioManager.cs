using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    // Singleton pattern
    public static AudioManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Số lượng loa chuẩn bị sẵn trong kho")]
    public int poolSize = 20;
    private List<AudioSource> audioPool = new List<AudioSource>();

    public enum MusicType
    {
        None,
        UI,
        FTUE,
        Level1,
        Level2,
        Level3
    }

    [Header("Music Tracks (Auto-Assigned)")]
    public AudioClip uiMusic;
    public AudioClip ftueMusic;
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;

    [Header("UI SFX")]
    [Tooltip("Âm thanh mặc định khi click vào bất kỳ UI Button nào")]
    public AudioClip defaultUIButtonClickSFX;
    [Tooltip("Âm thanh khi lướt chuột ngang qua Button (Hover)")]
    public AudioClip defaultUIButtonHoverSFX;

    [Header("BGM Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    private AudioSource bgmSource;

    [Header("SFX Volume")]
    [Range(0f, 1f)] public float sfxVolume = 0.4f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (uiMusic == null) uiMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Background_Music/MENU_Derp Nugget.mp3");
        if (ftueMusic == null) ftueMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Background_Music/FTUE_Ave Marimba.mp3");
        if (level1Music == null) level1Music = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Background_Music/LEVEL1_Club Seamus.mp3");
        if (level2Music == null) level2Music = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Background_Music/LEVEL2_Galway.mp3");
        if (level3Music == null) level3Music = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Shared_Resources/Background_Music/LEVEL3_Mountain Emperor.mp3");
    }
#endif

    private void Awake()
    {
        // Force the volumes to be balanced based on feedback
        bgmVolume = 1.0f;
        sfxVolume = 0.7f;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
            InitializeBGM();
        }
        else
        {

            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
    }

    private void InitializeBGM()
    {
        GameObject bgmObj = new GameObject("BGM_Player");
        bgmObj.transform.SetParent(this.transform);
        bgmSource = bgmObj.AddComponent<AudioSource>();
        bgmSource.loop = true; // Loop the music
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D sound for BGM
        bgmSource.volume = bgmVolume;
    }

    private AudioClip GetClipFromType(MusicType type)
    {
        switch (type)
        {
            case MusicType.UI: return uiMusic;
            case MusicType.FTUE: return ftueMusic;
            case MusicType.Level1: return level1Music;
            case MusicType.Level2: return level2Music;
            case MusicType.Level3: return level3Music;
            default: return null;
        }
    }

    public void PlayMusic(MusicType targetType)
    {
        AudioClip clipToPlay = GetClipFromType(targetType);

        if (clipToPlay == null)
        {
            bgmSource.Stop();
            return;
        }

        if (bgmSource.isPlaying && bgmSource.clip == clipToPlay)
        {
            return;
        }

        bgmSource.clip = clipToPlay;
        bgmSource.Play();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject speakerObj = new GameObject("Speaker_" + i);
            speakerObj.transform.SetParent(this.transform);

            AudioSource source = speakerObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = sfxVolume;

            source.spatialBlend = 1f;
            source.minDistance = 15f;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;

            audioPool.Add(source);
        }
    }

    private AudioSource GetAvailableSpeaker()
    {
        foreach (AudioSource source in audioPool)
        {
            if (!source.isPlaying) return source;
        }

        return audioPool[0];
    }

    public void PlaySFX_Directional2D(AudioClip clip, Vector2 spawnPosition, float duration = 0f)
    {
        if (clip == null) return;

        AudioSource speaker = GetAvailableSpeaker();
        speaker.spatialBlend = 1f;
        speaker.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        speaker.clip = clip;
        speaker.pitch = Random.Range(0.95f, 1.05f);
        speaker.Play();

        if (duration > 0f)
        {
            StartCoroutine(ForceStopSpeaker(speaker, clip, duration));
        }
    }

    public void PlaySFX_2D(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource speaker = GetAvailableSpeaker();
        speaker.spatialBlend = 0f; // Force 2D
        speaker.clip = clip;
        speaker.pitch = Random.Range(0.95f, 1.05f);
        speaker.Play();
    }

    private IEnumerator ForceStopSpeaker(AudioSource speaker, AudioClip originalClip, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (speaker != null && speaker.isPlaying && speaker.clip == originalClip)
        {
            speaker.Stop();
        }
    }
}