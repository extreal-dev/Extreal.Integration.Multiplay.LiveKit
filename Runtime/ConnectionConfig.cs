namespace Extreal.Integration.Multiplay.LiveKit
{
    public class ConnectionConfig
    {
        public string Url { get; }

        public ConnectionConfig(string url)
            => Url = url;
    }
}
