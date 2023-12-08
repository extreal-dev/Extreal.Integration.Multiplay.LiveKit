namespace Extreal.Integration.Multiplay.Common
{
    public class Room
    {
        public string Id { get; }
        public string Name { get; }

        public Room(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
