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

            GameObject audioObj = new GameObject("TempButtonAudio_" + clip.name);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.volume = 1f;

            source.Play();

            Destroy(audioObj, clip.length + 0.1f);
        }
    }
}
