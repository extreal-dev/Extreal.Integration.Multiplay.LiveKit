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

        public LiveKitMultiplayMessage(LiveKidMultiplayMessageCommand liveKidMultiplayMessageCommand, NetworkObjectInfo payload = default)
        {
            this.liveKidMultiplayMessageCommand = liveKidMultiplayMessageCommand;
            this.payload = payload;
        }

        public string ToJson()
            => JsonUtility.ToJson(this);
    }
}
