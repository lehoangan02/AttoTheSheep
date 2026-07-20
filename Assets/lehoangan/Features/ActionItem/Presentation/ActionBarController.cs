using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class ActionBarController : MonoBehaviour
{
    [Header("Assign your 4 slots here in order")]
    public ActionSlot[] actionSlots; // Array size should be 4 in the inspector
    [SerializeField] private InputActionReference[] actionBarActions;

    private LoadPlayerUseCase _loadPlayerUseCase;
    private IPlayerRepository _playerRepository;
    private PlayerProfile _currentPlayerProfile;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>[] _actionBarHandlers;

    private async void Start()
    {

        if (Application.isMobilePlatform)
        {
            bool isMobileCanvas = false;
            Transform current = transform;
            while (current != null)
            {
                if (current.name.Contains("MobileControlsCanvas"))
                {
                    isMobileCanvas = true;
                    break;
                }
                current = current.parent;
            }

            if (!isMobileCanvas)
            {

                gameObject.SetActive(false);
                return;
            }
        }

        // Auto-assign actionSlots if they were not assigned in the inspector
        if (actionSlots == null || actionSlots.Length == 0)
        {
            actionSlots = GetComponentsInChildren<ActionSlot>();

        }

        if (GameBootstrapper.Instance != null)
        {
            // Wait for Bootstrapper to finish fetching and fixing the profile
            await GameBootstrapper.Instance.InitializationTask;
            _playerRepository = GameBootstrapper.Instance.PlayerRepository;
            _currentPlayerProfile = GameBootstrapper.Instance.CurrentProfile;
        }
        else
        {
            // Fallback for test scenes without GameBootstrapper
            _playerRepository = new UnityCloudSaveRepository();
            _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);
            _currentPlayerProfile = await _loadPlayerUseCase.ExecuteAsync();
        }

        if (_currentPlayerProfile != null)
        {
            // Subscribe to profile changes
            _currentPlayerProfile.OnProfileUpdated += HandleProfileUpdated;
            InitializeSlots(_currentPlayerProfile);
        }

        HookActionBarInput();
    }

    private void HandleProfileUpdated()
    {

        InitializeSlots(_currentPlayerProfile);
    }
    private void OnDestroy()
    {
        if (_currentPlayerProfile != null)
        {
            _currentPlayerProfile.OnProfileUpdated -= HandleProfileUpdated;
        }

        if (actionBarActions != null)
        {
            for (int i = 0; i < actionBarActions.Length; i++)
            {
                if (actionBarActions[i] != null)
                {
                    if (_actionBarHandlers != null && i < _actionBarHandlers.Length && _actionBarHandlers[i] != null)
                        actionBarActions[i].action.performed -= _actionBarHandlers[i];
                    actionBarActions[i].action.Disable();
                }
            }
        }
    }

    private void InitializeSlots(PlayerProfile profile)
    {

        if (actionSlots == null) return;

        foreach (var slot in actionSlots)
        {
            if (slot == null)
            {

                continue;
            }
            if (slot.itemData == null)
            {

                continue;
            }

            slot.InjectDependencies(profile, _playerRepository);
            slot.InitializeUI();
        }
    }

    private void HookActionBarInput()
    {
        if (actionBarActions == null || actionBarActions.Length == 0)
        {

            return;
        }

        _actionBarHandlers = new System.Action<InputAction.CallbackContext>[actionBarActions.Length];
        for (int i = 0; i < actionBarActions.Length && i < 4; i++)
        {
            if (actionBarActions[i] != null)
            {
                int index = i;
                _actionBarHandlers[i] = _ => TriggerSlot(index);
                actionBarActions[i].action.performed += _actionBarHandlers[i];
                actionBarActions[i].action.Enable();
            }
        }
    }

    private bool IsMultiplayerScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "MultiplayerLevel" || sceneName == "SampleScene";
    }

    private void TriggerSlot(int index)
    {
        if (IsMultiplayerScene())
        {

            return;
        }

        // Safety check to ensure the array is set up properly
        if (index >= 0 && index < actionSlots.Length && actionSlots[index] != null)
        {
            if (actionSlots[index].UseItem())
            {
                string assetName = ((UnityEngine.Object)actionSlots[index].itemData).name;
                InvokeAttoSkill(assetName);
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

            }
            else
            {

            }
        }
        else
        {

        }
    }
}