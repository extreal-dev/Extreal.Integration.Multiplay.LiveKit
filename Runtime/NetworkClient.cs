using System.Collections.Generic;
using LiveKit;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class NetworkClient
    {
        public Participant Participant { get; }
        public GameObject PlayerObject { get; private set; }
        public IReadOnlyList<GameObject> NetworkObjects => networkObjects;
        private readonly List<GameObject> networkObjects = new List<GameObject>();

        public NetworkClient(Participant participant)
            => Participant = participant;

        public void SetPlayerObject(GameObject playerObject)
            => PlayerObject = playerObject;

        internal void AddNetworkObjects(List<GameObject> networkObjects)
            => this.networkObjects.AddRange(networkObjects);
    }
}
