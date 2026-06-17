# UI and Presentation Integration Plan

This document explains how the frontend/UI can interact with the new Clean Architecture lobby system. Since you are handling the backend/infrastructure, the frontend developer just needs to know which "Presenter" methods to call and what events to listen to.

## 1. The Lobby Presenter (`LobbyPresenter.cs`)

The `LobbyPresenter` acts as the single point of contact for the UI. It holds the state and orchestrates the Use Cases.

### Key Properties for UI:
- `Lobby JoinedLobby`: The current lobby the player is in.
- `bool IsHost`: Whether the local player is the host.
- `List<Lobby> AvailableLobbies`: Results from the last search.

### Key Methods for UI (Buttons/Actions):
- `Task CreateLobby(string name, bool isPrivate)`
- `Task RefreshLobbyList()`
- `Task JoinLobby(string lobbyId)`
- `Task LeaveLobby()`

## 2. Integration Pattern (View-Presenter)

The frontend developer should create a MonoBehaviour (the "View") that holds a reference to the `LobbyPresenter`.

### Example UI Script (View):
```csharp
public class LobbyUIView : MonoBehaviour {
    [SerializeField] private LobbyManager lobbyManager; // The entry point
    
    // UI Button Call
    public async void OnJoinClicked(string id) {
        SetLoading(true);
        try {
            await lobbyManager.Presenter.JoinLobby(id);
            ShowLobbyRoomUI(); // Switch screen on success
        } catch (Exception e) {
            ShowError(e.Message);
        } finally {
            SetLoading(false);
        }
    }
}
```

## 3. Communication via Events
To keep the UI updated without constant polling, the `LobbyPresenter` should expose events:
- `event Action OnLobbyListUpdated`: UI refreshes the scroll view.
- `event Action<Lobby> OnJoinedLobbyUpdated`: UI updates player list or room details.
- `event Action<string> OnErrorOccurred`: UI shows a popup.

## 4. Current Implementation Status
- **Domain/Interfaces**: DONE (`ILobbyService`)
- **Domain/UseCases**: DONE (`Create`, `Join`, `Leave`, `Get`, `Heartbeat`)
- **Data/Infrastructure**: DONE (`UnityLobbyService`)
- **Presentation**: NEXT STEP (Creating the `LobbyPresenter` and refactoring `LobbyManager`).

## 5. Next Steps for Frontend
Once the `LobbyPresenter` is finalized, the frontend developer only needs to:
1. Drag the `LobbyManager` prefab/object into their UI scripts.
2. Subscribe to `lobbyManager.Presenter.OnJoinedLobbyUpdated`.
3. Call `await lobbyManager.Presenter.Method()` on button clicks.
