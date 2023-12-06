namespace Extreal.Integration.Multiplay.Common
{
    public class ConnectionConfig
    {
        public string Url { get; }

        public ConnectionConfig(string url)
            => Url = url;
    }
}
