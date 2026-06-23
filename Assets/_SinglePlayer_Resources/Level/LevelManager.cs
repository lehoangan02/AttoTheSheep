using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelState
{
    /// <summary>Waiting for a player trigger.</summary>
    Idle,
    /// <summary>A wave is being spawned or fought.</summary>
    WaveInProgress,
    /// <summary>All waves cleared.</summary>
    LevelComplete
}

/// <summary>
/// Scene-level singleton that owns the level state machine.
/// Gatekeeps wave starts, tracks cleared waves, fires level completion.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Waves")]
    [Tooltip("Drag every WaveController in this level here.")]
    [SerializeField] private List<WaveController> allWaves = new List<WaveController>();

    [Header("Debug")]
    [SerializeField] private bool logStateChanges;

    private readonly HashSet<WaveController> _clearedWaves = new HashSet<WaveController>();
    private LevelState _currentState = LevelState.Idle;

    public LevelState CurrentState => _currentState;

    /// <summary>Fired when every wave in allWaves has been cleared.</summary>
    public event Action OnLevelComplete;

    // --- Singleton ---

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LevelManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        foreach (WaveController wave in allWaves)
        {
            if (wave != null)
                wave.OnWaveCleared += HandleWaveCleared;
        }
    }

    void OnDestroy()
    {
        foreach (WaveController wave in allWaves)
        {
            if (wave != null)
                wave.OnWaveCleared -= HandleWaveCleared;
        }

        if (Instance == this)
            Instance = null;
    }

    // --- Public API ---

    /// <summary>
    /// Called by WaveTrigger. Returns true if the wave was accepted and started.
    /// </summary>
    public bool TryStartWave(WaveController wave)
    {
        if (wave == null)
        {
            Debug.LogError("[LevelManager] TryStartWave called with null wave.");
            return false;
        }

        if (_currentState != LevelState.Idle)
        {
            if (logStateChanges)
                Debug.Log($"[LevelManager] Rejected start of '{wave.name}' — state is {_currentState}.");
            return false;
        }

        if (_clearedWaves.Contains(wave))
        {
            if (logStateChanges)
                Debug.Log($"[LevelManager] Rejected start of '{wave.name}' — already cleared.");
            return false;
        }

        SetState(LevelState.WaveInProgress);
        wave.BeginWave();
        return true;
    }

    /// <summary>Resets the entire level. All waves refill, all state cleared.</summary>
    public void ResetLevel()
    {
        if (logStateChanges) Debug.Log("[LevelManager] Resetting level.");

        _clearedWaves.Clear();

        foreach (WaveController wave in allWaves)
        {
            if (wave != null)
                wave.ResetWave();
        }

        SetState(LevelState.Idle);
    }

    // --- Internal ---

    private void HandleWaveCleared(WaveController wave)
    {
        _clearedWaves.Add(wave);

        if (logStateChanges)
            Debug.Log($"[LevelManager] Wave cleared: '{wave.name}'. {_clearedWaves.Count}/{allWaves.Count}.");

        if (_clearedWaves.Count >= allWaves.Count)
        {
            SetState(LevelState.LevelComplete);
            OnLevelComplete?.Invoke();
            Debug.Log("[LevelManager] Level Complete!");
        }
        else
        {
            SetState(LevelState.Idle);
        }
    }

    private void SetState(LevelState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        if (logStateChanges) Debug.Log($"[LevelManager] State → {newState}");
    }
}
