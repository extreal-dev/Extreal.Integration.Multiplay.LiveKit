using System;
using System.Diagnostics.CodeAnalysis;
using LiveKit;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public enum LiveKidMultiplayMessageCommand
    {
        None,
        Create,
        Update,
        UserConnected,
        Message,
    };

    [Serializable]
    public class LiveKitMultiplayMessage
    {
        public LiveKidMultiplayMessageCommand LiveKidMultiplayMessageCommand => liveKidMultiplayMessageCommand;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private LiveKidMultiplayMessageCommand liveKidMultiplayMessageCommand;

        public DataPacketKind DataPacketKind { get; }
        public RemoteParticipant ToParticipant { get; }

        public NetworkObjectInfo NetworkObjectInfo => networkObjectInfo;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo networkObjectInfo;

        public NetworkObjectInfo[] NetworkObjectInfos => networkObjectInfos;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private NetworkObjectInfo[] networkObjectInfos;

        public string Message => message;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string message;

        public LiveKitMultiplayMessage
        (
            LiveKidMultiplayMessageCommand liveKidMultiplayMessageCommand,
            DataPacketKind dataPacketKind = DataPacketKind.RELIABLE,
            RemoteParticipant toParticipant = default,
            NetworkObjectInfo networkObjectInfo = default,
            NetworkObjectInfo[] networkObjectInfos = default,
            string message = default
        )
        {
            this.liveKidMultiplayMessageCommand = liveKidMultiplayMessageCommand;
            DataPacketKind = dataPacketKind;
            ToParticipant = toParticipant;
            this.networkObjectInfo = networkObjectInfo;
            this.networkObjectInfos = networkObjectInfos;
            this.message = message;
        }

        public string ToJson()
            => JsonUtility.ToJson(this);
    }
}
