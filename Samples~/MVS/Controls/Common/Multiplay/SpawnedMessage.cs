using Unity.Netcode;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public struct SpawnedMessage : INetworkSerializable
    {
        public readonly ulong NetworkObjectId => networkObjectId;
        public readonly string AvatarAssetName => avatarAssetName;

        private ulong networkObjectId;
        private string avatarAssetName;

        public SpawnedMessage(ulong networkObjectId, string avatarAssetName)
        {
            this.networkObjectId = networkObjectId;
            this.avatarAssetName = avatarAssetName;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref avatarAssetName);
        }
    }
}
