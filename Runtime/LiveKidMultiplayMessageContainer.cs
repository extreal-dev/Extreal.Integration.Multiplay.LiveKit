using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class LiveKidMultiplayMessageContainer
    {
        public string MessageName => messageName;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string messageName;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public LiveKidMultiplayMessageContainer(string messageName, string message)
        {
            this.messageName = messageName;
            this.message = message;
        }
    }
}
