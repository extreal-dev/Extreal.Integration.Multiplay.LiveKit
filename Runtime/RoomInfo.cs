using System;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class RoomInfo
    {
        public string Id => id;
        [SerializeField] private string id;

        public string Name => name;
        [SerializeField] private string name;
    }
}
