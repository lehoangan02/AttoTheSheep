using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using System.Collections.Generic;


public class LobbyManager : MonoBehaviour
{
    private float heartbeatTimer;
    private Lobby hostLobby;
    private Lobby joinedLobby;

    private async void Start()
    {
        InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            string customArgument = ParrelSync.ClonesManager.GetArgument();
            options.SetProfile($"Clone{customArgument}");
        }
#endif

        await UnityServices.InitializeAsync(options);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Update is called once per frame
    void Update()
    {
        HandleLobbyHeartbeat();
    }
    [Command]
    public async void CreateLobby(string playerName, bool isPrivate)
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayer = 5;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptions);
            hostLobby = lobby;
            joinedLobby = lobby;
            Debug.Log("Created lobby with name: " + lobby.Name + " and id: " + lobby.Id);
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        
    }
    [Command]
    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);

            Debug.Log("Number of lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log("Lobby name: " + lobby.Name + " | Lobby ID: " + lobby.Id);
            }
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);

        }
    }
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0)
            {
                const float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    [Command]
    public async void JoinPrivateLobby(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            Debug.Log("Successfully joined lobby with id: " + lobbyCode);
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    [Command]
    public async void JoinLobby(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);
            joinedLobby = lobby;
            Debug.Log("Successfully joined lobby with id: " + lobbyId);
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public async void QuickJoinLobby()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            joinedLobby = lobby;
        }catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    private Player GetPlayer()
    {
        string playerName = "Sheep " + Random.Range(1, 100).ToString(); 
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }
    public async void LeaveLobby()
    {   
        try
        {
            if (joinedLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("Successfully left lobby: " + joinedLobby.Id);
                joinedLobby = null;
            }
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public async void KickPlayer(int index)
    {
        try
        {
            if (joinedLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[index].Id);
                Debug.Log("Successfully left lobby: " + joinedLobby.Id);
                joinedLobby = null;
            }
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public async void MigrateLobbyHost(int newPlayerIndex)
    {
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[newPlayerIndex].Id
            });
            joinedLobby = hostLobby;
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        } catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
