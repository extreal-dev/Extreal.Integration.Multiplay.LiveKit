using System.Text.Json;
using System.Text.Json.Serialization;

namespace Extreal.Integration.Multiplay.Messaging
{
    public enum MultiplayMessageCommand
    {
        Create,
        Update,
        CreateExistedObject,
        UserInitialized,
        Message,
    };

    public class MultiplayMessage
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public MultiplayMessageCommand Command { get; }
        public NetworkObjectInfo NetworkObjectInfo { get; }
        public NetworkObjectInfo[] NetworkObjectInfos { get; }
        public string Message { get; }

        public MultiplayMessage
        (
            MultiplayMessageCommand command,
            NetworkObjectInfo networkObjectInfo = default,
            NetworkObjectInfo[] networkObjectInfos = default,
            string message = default
        )
        {
            Command = command;
            NetworkObjectInfo = networkObjectInfo;
            NetworkObjectInfos = networkObjectInfos;
            Message = message;
        }

        public string ToJson()
        {
            if (NetworkObjectInfo != default)
            {
                NetworkObjectInfo.OnBeforeSerialize();
            }
            if (NetworkObjectInfos != default)
            {
                foreach (var networkObjectInfo in NetworkObjectInfos)
                {
                    networkObjectInfo.OnBeforeSerialize();
                }
            }

            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            });
        }

        public static MultiplayMessage FromJson(string json)
        {
            var multiplayMessage = JsonSerializer.Deserialize<MultiplayMessage>(json, new JsonSerializerOptions
            {
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            });
            if (multiplayMessage.NetworkObjectInfo != default)
            {
                multiplayMessage.NetworkObjectInfo.OnAfterDeserialize();
            }
            if (multiplayMessage.NetworkObjectInfos != default)
            {
                foreach (var networkObjectInfo in multiplayMessage.NetworkObjectInfos)
                {
                    networkObjectInfo.OnAfterDeserialize();
                }
            }


            return multiplayMessage;
        }
    }
}
