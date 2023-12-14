namespace Extreal.Integration.Multiplay.Common.MVS.Controls.ClientControl
{
    public class Group
    {
        public string Id { get; private set; }
        public string Name { get; private set; }

        public Group(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
