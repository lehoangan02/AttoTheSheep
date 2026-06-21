using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AttoTheSheep.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [Header("Audio Clips")]
        [Tooltip("Button clicked sound")]
        [SerializeField] private AudioClip clickSound;
        
        [Tooltip("Button hover sound (optional)")]
        [SerializeField] private AudioClip hoverSound;

        public void OnPointerClick(PointerEventData eventData)
        {
            var button = GetComponent<Button>();
            if (button != null && button.interactable && clickSound != null)
            {
                PlaySound(clickSound);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var button = GetComponent<Button>();
            if (button != null && button.interactable && hoverSound != null)
            {
                PlaySound(hoverSound);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            // Thay vì bắt bạn phải gắn AudioSource vào mọi Button cực kỳ mất thời gian,
            // Script này tự động tạo một AudioSource tạm thời (2D Sound) để phát âm thanh rồi tự xóa.
            // Sau này nếu có AudioManager tổng, bạn chỉ việc sửa hàm này để gọi AudioManager.
            
            GameObject audioObj = new GameObject("TempButtonAudio_" + clip.name);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f; // Ép thành âm thanh 2D để nghe rõ ràng không bị ảnh hưởng bởi camera
            source.playOnAwake = false;
            source.volume = 1f;
            
            source.Play();
            
            // Tự động xóa object rác sau khi phát xong
            Destroy(audioObj, clip.length + 0.1f);
        }
    }
}
