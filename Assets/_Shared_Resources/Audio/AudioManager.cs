using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class AudioManager : MonoBehaviour
{
    // Singleton pattern
    public static AudioManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Số lượng loa chuẩn bị sẵn trong kho")]
    public int poolSize = 20; 
    private List<AudioSource> audioPool = new List<AudioSource>();

    private void Awake()
    {
        // Setup Singleton và giữ cho nó sống xuyên suốt các Scene
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Không bị hủy khi load màn mới
            InitializePool(); // Khởi tạo kho loa ngay khi game chạy
        }
        else 
        {
            // Nếu lỡ có 2 cái AudioManager sinh ra, hủy cái mới đi
            Destroy(gameObject);
        }
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