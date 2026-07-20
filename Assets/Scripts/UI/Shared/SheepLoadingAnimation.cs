using UnityEngine;
using UnityEngine.UI;

namespace AttoTheSheep.UI.Shared
{
    [RequireComponent(typeof(Image))]
    public class SheepLoadingAnimation : MonoBehaviour
    {
        [Tooltip("Kéo các khung hình (sprites) của con cừu đang chạy vào đây")]
        public Sprite[] runFrames;

        [Tooltip("Số khung hình trên mỗi giây (FPS)")]
        public float frameRate = 12f;

        private Image _image;
        private float _timer;
        private int _currentFrame;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Update()
        {
            if (_image == null || runFrames == null || runFrames.Length == 0) return;

            _timer += Time.unscaledDeltaTime;

            float timePerFrame = 1f / frameRate;
            if (_timer >= timePerFrame)
            {
                _timer -= timePerFrame;
                _currentFrame = (_currentFrame + 1) % runFrames.Length;
                _image.sprite = runFrames[_currentFrame];
            }
        }
    }
}
