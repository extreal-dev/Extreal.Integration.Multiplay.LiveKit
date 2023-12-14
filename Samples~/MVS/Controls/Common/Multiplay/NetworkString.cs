using Unity.Collections;
using Unity.Netcode;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public struct NetworkString : INetworkSerializable
    {
        private FixedString64Bytes strBytes;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            => serializer.SerializeValue(ref strBytes);

        public override string ToString() => strBytes.ToString();

        public static implicit operator string(NetworkString str) => str.ToString();

        public static implicit operator NetworkString(string str) =>
            new NetworkString { strBytes = new FixedString64Bytes(str) };
    }
}
