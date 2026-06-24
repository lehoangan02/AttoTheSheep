namespace AttoTheSheep.Core
{
    [System.Serializable]
    public struct LobbyData
    {
        public string LobbyId;
        public string LobbyName;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string GameMode;
        public bool IsPrivate;
        public string LobbyCode;
    }
}
