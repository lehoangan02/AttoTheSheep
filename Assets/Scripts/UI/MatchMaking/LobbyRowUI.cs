using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class LobbyRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playersText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button rowButton;

    private string _lobbyName;

    public void Setup(AttoTheSheep.Core.LobbyData data, Action<string> onClicked)
    {
        _lobbyName = data.LobbyName;

        if (lobbyNameText != null) lobbyNameText.text = data.LobbyName;
        if (playersText != null) playersText.text = $"{data.CurrentPlayers}/{data.MaxPlayers}";
        if (gameModeText != null) gameModeText.text = data.GameMode;

        if (rowButton != null)
        {
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(() => onClicked?.Invoke(data.LobbyId));
        }
    }
}
