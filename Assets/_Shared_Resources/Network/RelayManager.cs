using UnityEngine;
using Unity.Netcode;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay; // <-- ADDED THIS MISSING NAMESPACE

using TMPro;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(2);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("JOIN CODE: " + joinCode);

            joinCodeText.text = joinCode;

            // Restored the constructor (now valid because of the new using directive)
            var relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton
                .GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinRelay()
    {
        try
        {
            string joinCode = joinCodeInput.text;

            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Restored the constructor (now valid because of the new using directive)
            var relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton
                .GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}