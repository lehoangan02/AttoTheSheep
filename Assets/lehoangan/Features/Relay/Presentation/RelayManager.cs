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

        }
    }

    [Command]
    private async void CreateRelay(int playerCount)
    {
        try
        {
            await Presenter.CreateRelay(playerCount);

        }
        catch (Exception e)
        {

        }
    }

    [Command]
    private async void JoinRelay(string joinCode)
    {
        try
        {

            await Presenter.JoinRelay(joinCode);

        }
        catch (Exception e)
        {

        }
    }
}
