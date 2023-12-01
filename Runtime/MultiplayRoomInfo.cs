using System;

namespace Extreal.Integration.Multiplay.Common
{
    [Serializable]
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
