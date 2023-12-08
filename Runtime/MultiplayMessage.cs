using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public enum MultiplayMessageCommand
    {
        None,
        Join,
        AvatarName,
        Create,
        Update,
        UserConnected,
        UserInitialized,
        Message,
    };

    [Serializable]
    public class MultiplayMessage
    {
        public MultiplayMessageCommand MultiplayMessageCommand => multiplayMessageCommand;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private MultiplayMessageCommand multiplayMessageCommand;

        public NetworkObject NetworkObjectInfo => networkObjectInfo;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObject networkObjectInfo;

        public NetworkObject[] NetworkObjectInfos => networkObjectInfos;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObject[] networkObjectInfos;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public MultiplayMessage
        (
            MultiplayMessageCommand multiplayMessageCommand,
            NetworkObject networkObjectInfo = default,
            NetworkObject[] networkObjectInfos = default,
            string message = default
        )
        {
            this.multiplayMessageCommand = multiplayMessageCommand;
            this.networkObjectInfo = networkObjectInfo;
            this.networkObjectInfos = networkObjectInfos;
            this.message = message;
        }

        public string ToJson()
            => JsonUtility.ToJson(this);
    }
}
