using UnityEngine;
using System.Threading.Tasks;

public class ActionBarController : MonoBehaviour
{
    [Header("Assign your 4 slots here in order")]
    public ActionSlot[] actionSlots; // Array size should be 4 in the inspector

    private LoadPlayerUseCase _loadPlayerUseCase;
    private IPlayerRepository _playerRepository;
    private PlayerProfile _currentPlayerProfile;

    private async void Start()
    {
        Debug.Log("[ActionBarController] Start running.");
        
        // Auto-assign actionSlots if they were not assigned in the inspector
        if (actionSlots == null || actionSlots.Length == 0)
        {
            actionSlots = GetComponentsInChildren<ActionSlot>();
            Debug.Log($"[ActionBarController] Auto-populated {actionSlots.Length} action slots.");
        }

        // Setup Dependencies
        _playerRepository = new UnityCloudSaveRepository();
        _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);

        // Load the player data
        _currentPlayerProfile = await _loadPlayerUseCase.ExecuteAsync();
        
        Debug.Log($"[ActionBarController] Profile loaded. FlockShield: {_currentPlayerProfile.FlockShieldCount}, SpeedBoost: {_currentPlayerProfile.SpeedBoostCount}");

        // Subscribe to profile changes
        _currentPlayerProfile.OnProfileUpdated += HandleProfileUpdated;

        InitializeSlots(_currentPlayerProfile);
    }

    private void HandleProfileUpdated()
    {
        Debug.Log("[ActionBarController] HandleProfileUpdated triggered by event!");
        InitializeSlots(_currentPlayerProfile);
    }

    private void OnDestroy()
    {
        if (_currentPlayerProfile != null)
        {
            _currentPlayerProfile.OnProfileUpdated -= HandleProfileUpdated;
        }
    }

    private void InitializeSlots(PlayerProfile profile)
    {
        Debug.Log($"[ActionBarController] InitializeSlots. Total slots in array: {actionSlots?.Length ?? -1}");
        
        if (actionSlots == null) return;

        foreach (var slot in actionSlots)
        {
            if (slot == null) 
            {
                Debug.Log("[ActionBarController] Found a null slot in actionSlots array!");
                continue;
            }
            if (slot.itemData == null)
            {
                Debug.Log($"[ActionBarController] Slot {slot.gameObject.name} has NULL itemData!");
                continue;
            }

            slot.InjectDependencies(profile, _playerRepository);
            slot.InitializeUI();
        }
    }

    void Update()
    {
        // Listen for standard keyboard numbers
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
            TriggerSlot(0);
        
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
            TriggerSlot(1);
        
        if (Input.GetKeyDown(KeyCode.Alpha3)) 
            TriggerSlot(2);
        
        if (Input.GetKeyDown(KeyCode.Alpha4)) 
            TriggerSlot(3);
    }

    private void TriggerSlot(int index)
    {
        // Safety check to ensure the array is set up properly
        if (index >= 0 && index < actionSlots.Length && actionSlots[index] != null)
        {
            if (actionSlots[index].UseItem())
            {
                InvokeAttoSkill(actionSlots[index].itemData.itemName);
            }
        }
    }

    private void InvokeAttoSkill(string itemName)
    {
        int cheatId = 0;
        switch (itemName)
        {
            case "Shield": cheatId = 1; break;
            case "DeathTotem": cheatId = 2; break;
            case "Meat": cheatId = 3; break;
            case "MushShroom": cheatId = 4; break;
        }

        if (cheatId == 0)
        {
            Debug.LogWarning($"[ActionBar] Unrecognized item name for skill invocation: {itemName}");
            return;
        }

        GameObject localPlayer = null;

        if (Unity.Netcode.NetworkManager.Singleton != null && 
            Unity.Netcode.NetworkManager.Singleton.LocalClient != null &&
            Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            localPlayer = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        }
        else
        {
            var playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                localPlayer = playerController.gameObject;
            }
        }

        if (localPlayer != null)
        {
            var playerCheats = localPlayer.GetComponent<PlayerCheats>();
            if (playerCheats != null)
            {
                playerCheats.ActivateCheat(cheatId);
                Debug.Log($"[ActionBar] Triggered Atto Skill '{itemName}' (Cheat ID: {cheatId}) on player.");
            }
            else
            {
                Debug.LogWarning("[ActionBar] Local player does not have a PlayerCheats component!");
            }
        }
        else
        {
            Debug.LogWarning("[ActionBar] Could not find local player to invoke skill!");
        }
    }
}