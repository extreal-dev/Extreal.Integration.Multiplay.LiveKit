namespace Extreal.Integration.Multiplay.Common
{
    public class MultiplayRoomInfo
    {
        public string Id { get; }
        public string Name { get; }

        public MultiplayRoomInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
