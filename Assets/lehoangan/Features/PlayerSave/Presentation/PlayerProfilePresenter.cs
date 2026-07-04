using UnityEngine;
using TMPro; // For TextMeshPro UI
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Services.Core; // Required for Cloud Save initialization
using Unity.Services.Authentication; // Required for Cloud Save authentication

public class PlayerProfilePresenter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Button addCoinsButton;
    [SerializeField] private Button saveButton;

    // Domain Use Cases and Data
    private LoadPlayerUseCase _loadPlayerUseCase;
    private IPlayerRepository _playerRepository;
    private PlayerProfile _currentPlayerProfile;

    private async void Start()
    {
        // 1. Initialize Unity Services & Authenticate (Required for Cloud Save)
        await InitializeServicesAsync();

        // 2. Setup Dependencies (In a real project, you might use Zenject or VContainer for this)
        _playerRepository = new UnityCloudSaveRepository();
        _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);

        // 3. Setup Buttons
        if (addCoinsButton != null) addCoinsButton.onClick.AddListener(OnAddCoinsClicked);
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);

        // 4. Load Initial Data
        await LoadPlayerData();
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Anonymous sign-in for testing Cloud Save
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); 
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    private async Task LoadPlayerData()
    {
        Debug.Log("Loading player data...");
        
        // Execute the Use Case to get the profile
        _currentPlayerProfile = await _loadPlayerUseCase.ExecuteAsync();

        // Update the UI
        UpdateUI();
        
        Debug.Log("Player data loaded successfully!");
    }

    private void UpdateUI()
    {
        if (_currentPlayerProfile == null) return;

        if (coinsText != null) coinsText.text = $"Coins: {_currentPlayerProfile.Coins}";
        if (expText != null) expText.text = $"Exp: {_currentPlayerProfile.Exp}";
    }

    // --- User Interaction Methods ---

    private void OnAddCoinsClicked()
    {
        if (_currentPlayerProfile == null) return;
        
        _currentPlayerProfile.AddExp(50); // Using the existing AddExp method from your PlayerProfile
        
        UpdateUI();
    }

    private async void OnSaveClicked()
    {
        if (_currentPlayerProfile == null) return;

        if (saveButton != null) saveButton.interactable = false; // Prevent spamming
        Debug.Log("Saving player data...");

        // Save using the repository (Alternatively, create a SavePlayerUseCase to encapsulate this)
        await _playerRepository.SaveAsync(_currentPlayerProfile);

        Debug.Log("Player data saved successfully!");
        if (saveButton != null) saveButton.interactable = true;
    }
}
