using UnityEngine;
using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Extreal.Integration.Multiplay.Messaging
{
    public class NetworkObjectInfo : ISerializationCallbackReceiver
    {
        [JsonIgnore] public Guid ObjectGuid { get; private set; }
        public string ObjectId { get; set; }

        public int GameObjectHash { get; }

        public Vector3 Position { get; set; }
        private Vector3 prePosition;

        public Quaternion Rotation { get; set; }
        private Quaternion preRotation;

        private PlayerInputValues values;
        public string JsonOfValues { get; set; }

        private const float InterpolationPeriod = 0.3f;
        private float elapsedTime;

        public NetworkObjectInfo(int gameObjectHash, Vector3 position, Quaternion rotation)
        {
            GameObjectHash = gameObjectHash;
            Position = position;
            Rotation = rotation;

            ObjectGuid = Guid.NewGuid();
        }

        public void OnBeforeSerialize()
        {
            ObjectId = ObjectGuid.ToString();

            if (values != null)
            {
                JsonOfValues = JsonSerializer.Serialize(values, values.GetType(), new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
                });
            }
        }

        public void OnAfterDeserialize()
            => ObjectGuid = new Guid(ObjectId);

        public bool CheckWhetherToSendData()
        {
            elapsedTime += Time.deltaTime;
            var willSendData = elapsedTime > InterpolationPeriod || (values != null && values.CheckWhetherToSendData());
            if (willSendData)
            {
                elapsedTime = 0f;
            }
            return willSendData;
        }

        public void GetTransformFrom(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }

        public void SetPreTransform(Transform transform)
        {
            prePosition = transform.position;
            preRotation = transform.rotation;
        }

        public void SetTransformTo(Transform transform)
        {
            elapsedTime += Time.deltaTime;
            var ratio = elapsedTime / InterpolationPeriod;
            transform.position = Vector3.LerpUnclamped(prePosition, Position, ratio);
            transform.rotation = Quaternion.LerpUnclamped(preRotation, Rotation, ratio);
        }

        public void ApplyValuesTo(in PlayerInput input)
        {
            var typeOfValues = input.Values.GetType();
            var options = new JsonSerializerOptions
            {
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            };
            values = JsonSerializer.Deserialize(JsonOfValues, typeOfValues, options) as PlayerInputValues;
            input.ApplyValues(values);
        }

        public void GetValuesFrom(in PlayerInput input)
            => values = input.Values;
    }
}
