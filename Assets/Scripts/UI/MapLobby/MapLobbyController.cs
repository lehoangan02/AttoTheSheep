using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapLobbyController : MonoBehaviour
{
    [Header("Level Nodes")]
    public List<Button> levelNodes; // 0 = Level 1, 1 = Level 2, 2 = Level 3
    public List<Sprite> levelUnlockedSprites; // Sprites for unlocked nodes
    public Sprite levelLockedSprite;

    [Header("UI Buttons")]
    public Button bagButton;
    public Button shopButton;
    public Button upgradeButton;
    public Button backButton;

    [Header("Progression")]
    public int defaultUnlockedLevel = 1;

    private void Start()
    {
        // Add click listeners to side UI buttons
        if (bagButton != null) bagButton.onClick.AddListener(OnBagClicked);
        if (shopButton != null) shopButton.onClick.AddListener(OnShopClicked);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        // Fetch progression from PlayerPrefs (Default is 1)
        int maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", defaultUnlockedLevel);

        // Force all levels to be unlocked by default
        maxUnlockedLevel = 999;

        if (levelNodes == null || levelNodes.Count == 0)
        {
            levelNodes = new List<Button>();
            int nodeIndex = 1;
            while (true)
            {
                GameObject nodeObj = GameObject.Find("Node_" + nodeIndex);
                if (nodeObj != null)
                {
                    Button btn = nodeObj.GetComponent<Button>();
                    if (btn != null) levelNodes.Add(btn);
                }
                else
                {
                    break;
                }
                nodeIndex++;
            }
        }

        // Setup Level Nodes
        for (int i = 0; i < levelNodes.Count; i++)
        {
            if (levelNodes[i] == null) continue;

            int levelIndex = i + 1; // 1-based level
            var img = levelNodes[i].GetComponent<Image>();

            if (levelIndex <= maxUnlockedLevel)
            {
                // Unlocked
                levelNodes[i].interactable = true;
                if (img != null && i < levelUnlockedSprites.Count)
                {
                    img.sprite = levelUnlockedSprites[i];
                }
            }
            else
            {
                // Locked
                levelNodes[i].interactable = false;
                if (img != null && levelLockedSprite != null)
                {
                    img.sprite = levelLockedSprite;
                }
            }

            // Capture the index for the delegate
            int currentLevel = levelIndex;
            levelNodes[i].onClick.AddListener(() => OnLevelClicked(currentLevel));
        }
    }

    private void OnBagClicked()
    {

    }

    private void OnShopClicked()
    {

    }

    private void OnUpgradeClicked()
    {

    }

    private void OnLevelClicked(int levelIndex)
    {
        string sceneName = "Level" + levelIndex;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(sceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void OnBackClicked()
    {

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo("MainMenu");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Call this to cheat or progress the game
    public void UnlockNextLevel()
    {
        int current = PlayerPrefs.GetInt("MaxUnlockedLevel", defaultUnlockedLevel);
        if (current < levelNodes.Count)
        {
            PlayerPrefs.SetInt("MaxUnlockedLevel", current + 1);
            PlayerPrefs.Save();

            // Reload scene to refresh UI
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo(gameObject.scene.name);
        }
    }
}
