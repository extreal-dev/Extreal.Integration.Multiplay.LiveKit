using Extreal.Integration.Multiplay.Common.MVS.App.AssetWorkflow;
using Extreal.Integration.Multiplay.Common;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Extreal.Integration.Messaging.Redis;
using SocketIOClient;
using Extreal.Integration.Messaging.Common;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.ClientControl
{
    public class ClientControlScope : LifetimeScope
    {
        [SerializeField] private MultiplayClient multiplayClient;

        protected override void Configure(IContainerBuilder builder)
        {
            var assetHelper = Parent.Container.Resolve<AssetHelper>();

            // builder.Register<GroupManager>(Lifetime.Singleton);

            var redisMessagingConfig = new RedisMessagingConfig("http://localhost:3030", new SocketIOOptions { EIO = EngineIO.V4 });
            var redisMessagingTransport = RedisMessagingTransportProvider.Provide(redisMessagingConfig);

            var messagingGroupManager = new GroupManager();
            messagingGroupManager.SetTransport(redisMessagingTransport);
            builder.RegisterComponent(messagingGroupManager);

            var messagingClient = new MessagingClient();
            messagingClient.SetTransport(redisMessagingTransport);
            var queuingMessagingClient = new QueuingMessagingClient(messagingClient);
            multiplayClient.SetMessagingClient(queuingMessagingClient);
            builder.RegisterComponent(multiplayClient);


            builder.RegisterEntryPoint<ClientControlPresenter>();
        }
    }
}
