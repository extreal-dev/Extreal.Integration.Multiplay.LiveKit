using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
{
    public enum MultiplayMessageCommand
    {
        Create,
        Update,
        CreateExistedObject,
        ClientInitialized,
        Message,
    };

    [Serializable]
    public class MultiplayMessage
    {
        public MultiplayMessageCommand Command => command;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private MultiplayMessageCommand command;

        public NetworkObject NetworkObjectInfo => networkObjectInfo;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObject networkObjectInfo;

        public NetworkObject[] NetworkObjectInfos => networkObjectInfos;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObject[] networkObjectInfos;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public MultiplayMessage
        (
            MultiplayMessageCommand command,
            NetworkObject networkObjectInfo = default,
            NetworkObject[] networkObjectInfos = default,
            string message = default
        )
        {
            this.command = command;
            this.networkObjectInfo = networkObjectInfo;
            this.networkObjectInfos = networkObjectInfos;
            this.message = message;
        }

        public string ToJson() => JsonUtility.ToJson(this);

        public static MultiplayMessage FromJson(string messageJson) => JsonUtility.FromJson<MultiplayMessage>(messageJson);
    }
}
