using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.Retry;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.App.Config
{
    [CreateAssetMenu(
        menuName = nameof(MVS) + "/" + nameof(MultiplayConfig),
        fileName = nameof(MultiplayConfig))]
    public class MultiplayConfig : ScriptableObject
    {
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int hostMaxCapacity = 10;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int clientTimeoutSeconds = 5;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int clientMaxRetryCount = 3;

        public HostConfig HostConfig => new HostConfig(hostMaxCapacity);
        public ClientConfig ClientConfig => new ClientConfig(clientTimeoutSeconds, clientMaxRetryCount);
    }

    public class HostConfig
    {
        public int MaxCapacity { get; private set; }
        public HostConfig(int maxCapacity)
        {
            MaxCapacity = maxCapacity;
        }
    }

    public class ClientConfig
    {
        public IRetryStrategy RetryStrategy { get; private set; }
        public ClientConfig(int timeoutSeconds, int maxRetryCount)
        {
            RetryStrategy = new CountingRetryStrategy(maxRetryCount);
        }
    }
}
