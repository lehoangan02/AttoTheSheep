using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private Canvas _canvas;
    private RectTransform _leftDoor;
    private RectTransform _rightDoor;

    [SerializeField] private float transitionDuration = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("SceneTransitionManager");
            Instance = obj.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateUI();
    }

    private void CreateUI()
    {
        // 1. Setup Canvas
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999; // Always on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Setup Left Door
        GameObject leftObj = new GameObject("LeftDoor");
        leftObj.transform.SetParent(canvasObj.transform, false);
        _leftDoor = leftObj.AddComponent<RectTransform>();
        Image leftImg = leftObj.AddComponent<Image>();
        leftImg.color = Color.black;
        
        _leftDoor.anchorMin = new Vector2(0, 0);
        _leftDoor.anchorMax = new Vector2(0.5f, 1);
        _leftDoor.pivot = new Vector2(1, 0.5f);
        _leftDoor.offsetMin = Vector2.zero;
        _leftDoor.offsetMax = Vector2.zero;

        // 3. Setup Right Door
        GameObject rightObj = new GameObject("RightDoor");
        rightObj.transform.SetParent(canvasObj.transform, false);
        _rightDoor = rightObj.AddComponent<RectTransform>();
        Image rightImg = rightObj.AddComponent<Image>();
        rightImg.color = Color.black;

        _rightDoor.anchorMin = new Vector2(0.5f, 0);
        _rightDoor.anchorMax = new Vector2(1, 1);
        _rightDoor.pivot = new Vector2(0, 0.5f);
        _rightDoor.offsetMin = Vector2.zero;
        _rightDoor.offsetMax = Vector2.zero;

        // Hide doors initially
        SetDoorsProgress(0f);
    }

    /// <summary>
    /// Set the progress of the doors. 0 = fully open (hidden), 1 = fully closed (black screen)
    /// </summary>
    private void SetDoorsProgress(float progress)
    {
        // Smoothstep for better easing
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        // When progress is 0, offset is 1000 (roughly half screen width). When 1, offset is 0.
        // We use an arbitrarily large value to ensure it goes completely offscreen.
        // Since reference resolution is 1920, half is 960. 1200 is safe.
        float offset = Mathf.Lerp(1200f, 0f, eased);

        if (_leftDoor != null) _leftDoor.anchoredPosition = new Vector2(-offset, 0);
        if (_rightDoor != null) _rightDoor.anchoredPosition = new Vector2(offset, 0);
    }

    public void TransitionTo(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. Close doors
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDoorsProgress(elapsed / transitionDuration);
            yield return null;
        }
        SetDoorsProgress(1f);

        // Tích hợp Loading Screen: Bật con cừu chạy sau khi cửa đã đóng kín
        if (AttoTheSheep.Core.LoadingManager.Instance != null)
        {
            AttoTheSheep.Core.LoadingManager.Instance.Show("msg_loading_game");
            yield return null; // Đợi 1 frame để UI update
        }

        // 2. Load Scene
        yield return SceneManager.LoadSceneAsync(sceneName);

        // Optional short delay
        yield return new WaitForSecondsRealtime(0.1f);

        // Tắt Loading Screen trước khi mở cửa
        if (AttoTheSheep.Core.LoadingManager.Instance != null)
        {
            AttoTheSheep.Core.LoadingManager.Instance.Hide();
        }

        // 3. Open doors
        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDoorsProgress(1f - (elapsed / transitionDuration));
            yield return null;
        }
        SetDoorsProgress(0f);
    }
}
