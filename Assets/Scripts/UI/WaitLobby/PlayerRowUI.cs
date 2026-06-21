using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerRowUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostIndicator;
    [SerializeField] private Button kickButton;

    public void Setup(Sprite avatar, string playerName, bool isHost, bool isLocalPlayerHost, bool isMe, System.Action onKickClicked)
    {
        if (avatarImage != null) avatarImage.sprite = avatar;
        if (playerNameText != null) playerNameText.text = playerName;
        
        // Host indicator is only visible for the host
        if (hostIndicator != null) hostIndicator.SetActive(isHost);

        // Kick button is only visible IF the local player is the host AND this row is NOT the local player
        if (kickButton != null)
        {
            kickButton.gameObject.SetActive(isLocalPlayerHost && !isMe);
            kickButton.onClick.RemoveAllListeners();
            if (onKickClicked != null)
            {
                kickButton.onClick.AddListener(() => onKickClicked());
            }
        }
    }
}
