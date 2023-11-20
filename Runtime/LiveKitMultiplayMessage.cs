using System;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public enum LiveKidMultiplayMessageCommand
    {
        None,
        Create,
        Update,
        Delete,
    };

    [Serializable]
    public class LiveKitMultiplayMessage
    {
        public string Topic;
        public LiveKidMultiplayMessageCommand Command;
        public NetworkObject Payload;

        public LiveKitMultiplayMessage(string topic, LiveKidMultiplayMessageCommand command, NetworkObject payload = default)
        {
            Topic = topic;
            Command = command;
            Payload = payload;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
