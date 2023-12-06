using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public class NetworkClient
    {
        public string UserIdentity { get; }
        public GameObject PlayerObject { get; private set; }
        public IReadOnlyList<GameObject> NetworkObjects => networkObjects;
        private readonly List<GameObject> networkObjects = new List<GameObject>();

        public NetworkClient(string userIdentity)
            => UserIdentity = userIdentity;

        public void SetPlayerObject(GameObject playerObject)
            => PlayerObject = playerObject;

        internal void AddNetworkObject(GameObject networkObject)
            => networkObjects.Add(networkObject);
    }
}
