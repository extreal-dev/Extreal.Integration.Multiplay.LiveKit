using Newtonsoft.Json;

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

    public class MultiplayMessage
    {
        public MultiplayMessageCommand Command { get; }
        public NetworkObject NetworkObject { get; }
        public NetworkObject[] NetworkObjects { get; }
        public string Message { get; }

        public MultiplayMessage
        (
            MultiplayMessageCommand command,
            NetworkObject networkObject = default,
            NetworkObject[] networkObjects = default,
            string message = default
        )
        {
            Command = command;
            NetworkObject = networkObject;
            NetworkObjects = networkObjects;
            Message = message;
        }

        public string ToJson()
        {
            if (NetworkObject != default)
            {
                NetworkObject.OnBeforeSerialize();
            }
            if (NetworkObjects != default)
            {
                foreach (var networkObject in NetworkObjects)
                {
                    networkObject.OnBeforeSerialize();
                }
            }

            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            });
        }

        public static MultiplayMessage FromJson(string json)
        {
            var multiplayMessage = JsonConvert.DeserializeObject<MultiplayMessage>(json, new JsonSerializerSettings
            {
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            });
            if (multiplayMessage.NetworkObject != default)
            {
                multiplayMessage.NetworkObject.OnAfterDeserialize();
            }
            if (multiplayMessage.NetworkObjects != default)
            {
                foreach (var networkObject in multiplayMessage.NetworkObjects)
                {
                    networkObject.OnAfterDeserialize();
                }
            }

            return multiplayMessage;
        }
    }
}
