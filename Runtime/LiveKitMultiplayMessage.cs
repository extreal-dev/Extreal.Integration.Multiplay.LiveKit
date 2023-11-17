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
    public class LiveKitMultiplayMessage<T> where T : LiveKitPlayerInputValues, new()
    {
        public string Topic;
        public LiveKidMultiplayMessageCommand Command;
        public NetworkObject<T> Payload;

        public LiveKitMultiplayMessage(string topic, LiveKidMultiplayMessageCommand command, NetworkObject<T> payload = default)
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
