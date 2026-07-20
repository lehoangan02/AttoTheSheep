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

            return;
        }

        if (nameText == null)
        {

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