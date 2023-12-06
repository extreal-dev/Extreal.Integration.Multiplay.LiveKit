using Extreal.Integration.Messaging.Common;

namespace Extreal.Integration.Multiplay.Common
{
    public class MessagingMultiplayConnectionConfig : MultiplayConnectionConfig
    {
        public MessagingConnectionConfig MessagingConnectionConfig { get; }

        public MessagingMultiplayConnectionConfig(MessagingConnectionConfig connectionConfig)
            => MessagingConnectionConfig = connectionConfig;
    }
}
