namespace Extreal.Integration.Multiplay.LiveKit
{
    public class RedisConnectionConfig : ConnectionConfig
    {
        public string RoomName { get; }

        public RedisConnectionConfig(string url, string roomName) : base(url)
        {
            RoomName = roomName;
        }
    }
}
