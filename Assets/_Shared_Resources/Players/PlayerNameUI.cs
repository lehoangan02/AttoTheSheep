using UnityEngine;
using Unity.Netcode;
using TMPro; // Assuming you are using TextMeshPro for UI

public class PlayerNameUI : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    private PlayerController controller;

    void Awake()
    {
        // Find the PlayerController whether this script is on the UI Text, the Canvas, or the Root Player object
        controller = GetComponentInParent<PlayerController>();
        if (controller == null)
            controller = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        if (controller == null)
        {
            Debug.LogError($"🔴 [PlayerNameUI] Không tìm thấy PlayerController ở Object cha của {gameObject.name}");
            return;
        }

        if (nameText == null)
        {
            Debug.LogError($"🔴 [PlayerNameUI] Chưa kéo thả Text (TMP) vào biến 'Name Text' trong Inspector của {gameObject.name}");
            return;
        }

        // 1. Update the UI immediately when this player spawns
        UpdateNameTag(new PlayerController.PlayerPublicData(), controller.netPlayerPublicData.Value);

        // 2. Subscribe to listen for any future name changes
        controller.netPlayerPublicData.OnValueChanged += UpdateNameTag;
    }

    private void UpdateNameTag(PlayerController.PlayerPublicData previousValue, PlayerController.PlayerPublicData newValue)
    {
        if (nameText != null)
        {
            string newNameStr = newValue.playerName.ToString();
            // Convert the FixedString64Bytes to a standard string
            nameText.text = newNameStr;
            Debug.Log($"🟢 [PlayerNameUI] Cập nhật UI thành công: {newNameStr} cho {controller.gameObject.name}");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (controller != null)
        {
            // Always unsubscribe to prevent memory leaks when the player disconnects
            controller.netPlayerPublicData.OnValueChanged -= UpdateNameTag;
        }
    }
}