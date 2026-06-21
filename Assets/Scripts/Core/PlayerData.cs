namespace AttoTheSheep.Core
{
    [System.Serializable]
    public struct PlayerData
    {
        public string PlayerId;
        public string PlayerName;
        public bool IsHost;
        public bool IsLocalPlayer;
        public int AvatarIndex;
    }
}
