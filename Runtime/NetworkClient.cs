using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
{
    /// <summary>
    /// Class that holds clients and the objects they own.
    /// </summary>
    public class NetworkClient
    {
        /// <summary>
        /// Client ID.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Objects to be spawned.
        /// </summary>
        public IReadOnlyList<GameObject> NetworkObjects => networkObjects;
        private readonly List<GameObject> networkObjects = new List<GameObject>();

        /// <summary>
        /// Creates a new NetworkClient.
        /// </summary>
        /// <param name="clientId">Client ID.</param>
        [SuppressMessage("Usage", "CC0057")]
        public NetworkClient(string clientId)
            => ClientId = clientId;

        internal void AddNetworkObject(GameObject networkObject)
            => networkObjects.Add(networkObject);
    }
}
