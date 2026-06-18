using UnityEngine;
using QFSW.QC;

public class FPSLimiter : MonoBehaviour
{
    public static FPSLimiter Instance { get; private set; }

    [SerializeField] private int _targetFPS = 60;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        ApplyFPSLimit(_targetFPS);
    }

    [Command("set-fps", "Sets the target frame rate (30, 60, 120, 240). Use 0 or -1 for uncapped.")]
    public void SetTargetFPS(int fps)
    {
        _targetFPS = fps;
        ApplyFPSLimit(_targetFPS);
        Debug.Log($"[FPSLimiter] Target FPS set to: {(_targetFPS > 0 ? _targetFPS.ToString() : "Uncapped")}");
    }

    private void ApplyFPSLimit(int fps)
    {
        // QualitySettings.vSyncCount must be 0 for Application.targetFrameRate to work
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    [Command("get-fps")]
    public int GetCurrentTargetFPS()
    {
        return Application.targetFrameRate;
    }
}
