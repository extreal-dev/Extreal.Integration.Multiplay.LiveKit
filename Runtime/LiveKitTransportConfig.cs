namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitTransportConfig : TransportConfig
    {
        public string ApiServerUrl { get; }

        public LiveKitTransportConfig(string apiServerUrl)
            => ApiServerUrl = apiServerUrl;
    }
}
