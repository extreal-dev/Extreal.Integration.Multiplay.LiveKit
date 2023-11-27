using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public enum LiveKidMultiplayMessageCommand
    {
        None,
        Create,
        Update,
    };

    [Serializable]
    public class LiveKitMultiplayMessage
    {
        public LiveKidMultiplayMessageCommand LiveKidMultiplayMessageCommand => liveKidMultiplayMessageCommand;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private LiveKidMultiplayMessageCommand liveKidMultiplayMessageCommand;

        public NetworkObjectInfo Payload => payload;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo payload;

        public NetworkObjectInfo[] NetworkObjectInfos => networkObjectInfos;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo[] networkObjectInfos;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public LiveKitMultiplayMessage
        (
            LiveKidMultiplayMessageCommand liveKidMultiplayMessageCommand,
            NetworkObjectInfo payload = default,
            NetworkObjectInfo[] networkObjectInfos = default,
            string message = default
        )
        {
            this.liveKidMultiplayMessageCommand = liveKidMultiplayMessageCommand;
            this.payload = payload;
            this.networkObjectInfos = networkObjectInfos;
        }

        public string ToJson()
            => JsonUtility.ToJson(this);
    }
}
