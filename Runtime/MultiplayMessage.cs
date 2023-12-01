using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public enum MultiplayMessageCommand
    {
        None,
        Create,
        Update,
        UserConnected,
        Message,
    };

    [Serializable]
    public class MultiplayMessage
    {
        public MultiplayMessageCommand MultiplayMessageCommand => multiplayMessageCommand;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private MultiplayMessageCommand multiplayMessageCommand;

        public NetworkObjectInfo NetworkObjectInfo => networkObjectInfo;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo networkObjectInfo;

        public NetworkObjectInfo[] NetworkObjectInfos => networkObjectInfos;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo[] networkObjectInfos;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public MultiplayMessage
        (
            MultiplayMessageCommand multiplayMessageCommand,
            NetworkObjectInfo networkObjectInfo = default,
            NetworkObjectInfo[] networkObjectInfos = default,
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
