using UnityEngine;
using TMPro;
using System.Linq;

public class WaveDisplayMultiplayer : MonoBehaviour
{
    [Tooltip("Drag the TextMeshPro child object here. If left empty, the script will try to find it in children automatically.")]
    [SerializeField] private TextMeshProUGUI waveText;
    
    private WaveController[] allWaves;
    private int lastClearedCount = -1;
    private int lastRoundCount = -1;

    void Start()
    {
        // Auto-assign TMP if the user forgot to drag it in
        if (waveText == null)
        {
            waveText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Scan the scene/level for all wave controllers
        // Note: FindObjectsSortMode.None is fast and works perfectly for this
        allWaves = Object.FindObjectsByType<WaveController>(FindObjectsSortMode.None);
        
        // Subscribe to each wave's cleared event
        foreach (var wave in allWaves)
        {
            if (wave != null)
            {
                wave.OnWaveCleared += HandleWaveCleared;
            }
        }

        UpdateDisplay();
    }

    void OnDestroy()
    {
        // Cleanup event subscriptions to prevent memory leaks
        if (allWaves != null)
        {
            foreach (var wave in allWaves)
            {
                if (wave != null)
                {
                    wave.OnWaveCleared -= HandleWaveCleared;
                }
            }
        }
    }

    private void HandleWaveCleared(WaveController wave)
    {
        UpdateDisplay();
    }

    void Update()
    {
        // Poll for changes gracefully in case a wave resets (e.g. infinite mode or game restart)
        // without firing the explicit OnWaveCleared event.
        if (allWaves == null || allWaves.Length == 0) return;
        
        int currentClearedCount = allWaves.Count(w => w != null && w.IsCleared);
        int currentRoundCount = LevelManager.Instance != null ? LevelManager.Instance.InfiniteRoundCount : 0;
        
        if (currentClearedCount != lastClearedCount || currentRoundCount != lastRoundCount)
        {
            UpdateDisplay(currentClearedCount, currentRoundCount);
        }
    }

    private void UpdateDisplay(int clearedCount = -1, int roundCount = -1)
    {
        if (waveText == null || allWaves == null || allWaves.Length == 0) return;
        
        if (clearedCount == -1)
        {
            clearedCount = allWaves.Count(w => w != null && w.IsCleared);
        }
        if (roundCount == -1)
        {
            roundCount = LevelManager.Instance != null ? LevelManager.Instance.InfiniteRoundCount : 0;
        }
        
        lastClearedCount = clearedCount;
        lastRoundCount = roundCount;
        
        // Calculate the absolute endless wave number
        int absoluteWaveIndex = (roundCount * allWaves.Length) + clearedCount + 1;
        
        // Ensure it always has at least 2 digits (01, 09, 10, etc.)
        waveText.text = $"Wave {absoluteWaveIndex:00}";
    }
}
