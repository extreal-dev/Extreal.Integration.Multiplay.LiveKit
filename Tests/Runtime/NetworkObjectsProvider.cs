using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging.Test
{
    public class NetworkObjectsProvider : MonoBehaviour, INetworkObjectsProvider
    {
        public GameObject NetworkObject => networkObject;
        [SerializeField] private GameObject networkObject;

        public GameObject SpawnFailedObject => spawnFailedObject;
        [SerializeField] private GameObject spawnFailedObject;

        public Dictionary<string, GameObject> Provide()
            => new Dictionary<string, GameObject> { { "testKey", networkObject } };
    }
}
