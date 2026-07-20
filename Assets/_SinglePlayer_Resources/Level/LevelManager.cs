using System;
using System.Collections;
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

    [Header("Infinite Mode")]
    [Tooltip("When enabled, waves must be completed in the listed order. After all waves are cleared, the cycle repeats indefinitely.")]
    [SerializeField] private bool infiniteMode;

    [Tooltip("Seconds to wait before auto-starting the next wave in infinite mode.")]
    [SerializeField] private float infiniteModeWaveDelay = 3f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges;

    private readonly HashSet<WaveController> _clearedWaves = new HashSet<WaveController>();
    private int _currentWaveIndex;
    private LevelState _currentState = LevelState.Idle;

    public LevelState CurrentState => _currentState;

    /// <summary>Fired when every wave in allWaves has been cleared.</summary>
    public event Action OnLevelComplete;

    // --- Singleton ---

    void Awake()
    {
        if (Instance != null && Instance != this)
        {

            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.ResetSessionCoins();
        }

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

            return false;
        }

        if (_currentState != LevelState.Idle)
        {
            if (logStateChanges)

            return false;
        }

        if (infiniteMode)
        {
            int expectedIndex = _currentWaveIndex % allWaves.Count;
            int requestedIndex = allWaves.IndexOf(wave);

            if (requestedIndex != expectedIndex)
            {
                if (logStateChanges)

                return false;
            }

            if (_clearedWaves.Contains(wave))
            {
                if (logStateChanges)

                return false;
            }
        }
        else
        {
            if (_clearedWaves.Contains(wave))
            {
                if (logStateChanges)

                return false;
            }
        }

        SetState(LevelState.WaveInProgress);
        wave.BeginWave();
        return true;
    }

    /// <summary>Resets the entire level. All waves refill, all state cleared.</summary>
    public void ResetLevel()
    {
        if (logStateChanges)

        _clearedWaves.Clear();
        _currentWaveIndex = 0;

        foreach (WaveController wave in allWaves)
        {
            if (wave != null)
                wave.ResetWave();
        }

        SetState(LevelState.Idle);
    }

    private int _infiniteRoundCount = 0;
    public int InfiniteRoundCount => _infiniteRoundCount;
    private List<GameObject> _allEnemyPrefabsInLevel = new List<GameObject>();

    private void HandleWaveCleared(WaveController wave)
    {
        _clearedWaves.Add(wave);

        if (infiniteMode)
        {
            _currentWaveIndex++;

            if (logStateChanges)

            if (_clearedWaves.Count >= allWaves.Count)
            {

                _infiniteRoundCount++;

                // Collect all enemies to inject into later rounds
                if (_allEnemyPrefabsInLevel.Count == 0)
                {
                    HashSet<GameObject> uniqueEnemies = new HashSet<GameObject>();
                    foreach (WaveController w in allWaves)
                    {
                        if (w != null && w.Data != null && w.Data.EnemyPrefabs != null)
                        {
                            foreach(var prefab in w.Data.EnemyPrefabs)
                                uniqueEnemies.Add(prefab);
                        }
                    }
                    _allEnemyPrefabsInLevel.AddRange(uniqueEnemies);
                }

                foreach (WaveController w in allWaves)
                {
                    if (w != null)
                    {
                        w.SetScaling(_infiniteRoundCount, _allEnemyPrefabsInLevel);
                        w.ResetWave();
                    }
                }

                _clearedWaves.Clear();
                _currentWaveIndex = 0;
                SetState(LevelState.Idle);
                StartCoroutine(AutoStartNextWaveAfterDelay());
            }
            else
            {
                SetState(LevelState.Idle);
                StartCoroutine(AutoStartNextWaveAfterDelay());
            }
        }
        else
        {
            if (logStateChanges)

            if (_clearedWaves.Count >= allWaves.Count)
            {
                SetState(LevelState.LevelComplete);
                OnLevelComplete?.Invoke();

            }
            else
            {
                SetState(LevelState.Idle);
            }
        }
    }

    private IEnumerator AutoStartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(infiniteModeWaveDelay);

        int nextIndex = _currentWaveIndex % allWaves.Count;
        if (nextIndex < allWaves.Count)
        {
            TryStartWave(allWaves[nextIndex]);
        }
    }

    private void SetState(LevelState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        if (logStateChanges)
    }
}
