namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitConnectionConfig : ConnectionConfig
    {
        public string RoomName { get; }
        public string AccessToken { get; }

        public LiveKitConnectionConfig(string url, string roomName, string accessToken) : base(url)
        {
            RoomName = roomName;
            AccessToken = accessToken;
        }
    }
}
