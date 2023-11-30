using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public enum MultiplayMessageCommand
    {
        None,
        Join,
        Create,
        Update,
        UserConnected,//serverは気にしなくてよい
        Message,//serverは気にしなくてよい
    };

    [Serializable]
    public class MultiplayMessage
    {
        public string UserIdentity => userIdentity;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string userIdentity;

        public string Topic => topic;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string topic;

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
            string userIdentity,
            string topic,
            MultiplayMessageCommand multiplayMessageCommand,
            NetworkObjectInfo networkObjectInfo = default,
            NetworkObjectInfo[] networkObjectInfos = default,
            string message = default
        )
        {
            this.userIdentity = userIdentity;
            this.topic = topic;
            this.multiplayMessageCommand = multiplayMessageCommand;
            this.networkObjectInfo = networkObjectInfo;
            this.networkObjectInfos = networkObjectInfos;
            this.message = message;
        }

        public string ToJson()
            => JsonUtility.ToJson(this);
    }
}
