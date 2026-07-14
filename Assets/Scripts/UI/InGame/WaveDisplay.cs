using UnityEngine;
using TMPro;
using System.Linq;

public class WaveDisplay : MonoBehaviour
{
    [Tooltip("Drag the TextMeshPro child object here. If left empty, the script will try to find it in children automatically.")]
    [SerializeField] private TextMeshProUGUI waveText;
    
    private WaveController[] allWaves;
    private int lastClearedCount = -1;

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
        if (currentClearedCount != lastClearedCount)
        {
            UpdateDisplay(currentClearedCount);
        }
    }

    private void UpdateDisplay(int clearedCount = -1)
    {
        if (waveText == null || allWaves == null || allWaves.Length == 0) return;
        
        // If not explicitly passed, calculate the cleared count
        if (clearedCount == -1)
        {
            clearedCount = allWaves.Count(w => w != null && w.IsCleared);
        }
        
        lastClearedCount = clearedCount;
        
        // We show (clearedCount + 1) for the *current* wave being played, unless we cleared them all.
        int currentWaveDisplay = Mathf.Min(clearedCount + 1, allWaves.Length);
        
        if (clearedCount == allWaves.Length)
        {
            waveText.text = "Waves Cleared!";
        }
        else
        {
            waveText.text = $"Wave: {currentWaveDisplay}/{allWaves.Length}";
        }
    }
}
