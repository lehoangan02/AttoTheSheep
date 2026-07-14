using UnityEngine;
using QFSW.QC;
using System;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    public RelayPresenter Presenter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Dependency Injection Setup
        IRelayService relayService = new UnityRelayService();
        INetworkConnectionService connectionService = new UnityNetworkConnectionService();

        Presenter = new RelayPresenter(
            new CreateRelayUseCase(relayService, connectionService),
            new JoinRelayUseCase(relayService, connectionService)
        );
    }

    private void Start()
    {
        if (GameBootstrapper.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] GameBootstrapper Instance not found. Lobby might not work correctly if services aren't initialized.");
        }
    }

    [Command]
    private async void CreateRelay(int playerCount)
    {
        try
        {
            await Presenter.CreateRelay(playerCount);
            Debug.Log($"Relay created successfully with join code: {Presenter.HostData.JoinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create relay: {e.Message}");
        }
    }

    [Command]
    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining relay with code: {joinCode}");
            await Presenter.JoinRelay(joinCode);
            Debug.Log("Relay joined successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join relay: {e.Message}");
        }
    }
}
