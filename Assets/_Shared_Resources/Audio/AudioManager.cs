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

    [System.Serializable]
    public struct SceneMusicMapping
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("Background Music Settings")]
    [Tooltip("Khai báo Scene nào sẽ phát bài nhạc nào. Nếu đổi scene mà chung bài nhạc, nó sẽ không restart lại.")]
    public List<SceneMusicMapping> sceneMusicMap = new List<SceneMusicMapping>();
    
    [Header("BGM Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    private AudioSource bgmSource;

    private void Awake()
    {
        // Setup Singleton và giữ cho nó sống xuyên suốt các Scene
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Không bị hủy khi load màn mới
            InitializePool(); // Khởi tạo kho loa ngay khi game chạy
            InitializeBGM();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else 
        {
            // Nếu lỡ có 2 cái AudioManager sinh ra, hủy cái mới đi
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip clipToPlay = null;

        // Find the music for this scene
        foreach (var mapping in sceneMusicMap)
        {
            if (mapping.sceneName == scene.name)
            {
                clipToPlay = mapping.musicClip;
                break;
            }
        }

        // If no music is mapped to this scene, we can just let the old one play
        // (Or stop it if you prefer: if (clipToPlay == null) bgmSource.Stop(); )
        if (clipToPlay == null)
            return;

        // If the same music is already playing, DO NOT RESTART
        if (bgmSource.isPlaying && bgmSource.clip == clipToPlay)
        {
            return;
        }

        // Otherwise, switch the track and play
        bgmSource.clip = clipToPlay;
        bgmSource.Play();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject speakerObj = new GameObject("Speaker_" + i);
            speakerObj.transform.SetParent(this.transform); // Gom các loa làm con của AudioManager
            
            AudioSource source = speakerObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            
            // Cài đặt âm thanh 3D giả lập cho Game 2D
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
        
        // Nếu tất cả các loa đều đang phát (quá tải), lấy đại loa đầu tiên
        return audioPool[0]; 
    }

    // Hàm gọi để phát âm thanh từ BaseSkillComponent
    // Cập nhật hàm này: Thêm tham số float duration
    public void PlaySFX_Directional2D(AudioClip clip, Vector2 spawnPosition, float duration = 0f)
    {
        if (clip == null) return;

        AudioSource speaker = GetAvailableSpeaker();
        speaker.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        speaker.clip = clip;
        speaker.pitch = Random.Range(0.95f, 1.05f); 
        speaker.Play();

        // NẾU CÓ CÀI ĐẶT THỜI GIAN ÉP BUỘC TẮT (DURATION > 0)
        if (duration > 0f)
        {
            StartCoroutine(ForceStopSpeaker(speaker, clip, duration));
        }
    }

    // Coroutine canh giờ tắt loa
    private IEnumerator ForceStopSpeaker(AudioSource speaker, AudioClip originalClip, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Kiểm tra an toàn: Đảm bảo loa đang phát và file âm thanh vẫn là file cũ 
        // (Phòng trường hợp loa vừa tắt đã bị thằng khác mượn)
        if (speaker != null && speaker.isPlaying && speaker.clip == originalClip)
        {
            speaker.Stop();
        }
    }
}