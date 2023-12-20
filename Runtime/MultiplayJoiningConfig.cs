using Extreal.Integration.Messaging.Common;

namespace Extreal.Integration.Multiplay.Common
{
    public class MultiplayJoiningConfig
    {
        public MessagingJoiningConfig MessagingJoiningConfig { get; private set; }

        public MultiplayJoiningConfig(MessagingJoiningConfig messagingConnectionConfig)
            => MessagingJoiningConfig = messagingConnectionConfig;
    }
}
