using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Multiplay.Messaging
{
    [Serializable]
    public class NetworkObject : ISerializationCallbackReceiver
    {
        [JsonIgnore] public Guid ObjectGuid { get; private set; }
        [SuppressMessage("Usage", "CC0047")]
        public string ObjectId { get; set; }

        public string GameObjectKey { get; }

        [SuppressMessage("Usage", "CC0047")]
        public Vector3 Position { get; set; }
        private Vector3 prePosition;

        [SuppressMessage("Usage", "CC0047")]
        public Quaternion Rotation { get; set; }
        private Quaternion preRotation;

        private PlayerInputValues values;
        [SuppressMessage("Usage", "CC0047")]
        public string JsonOfValues { get; set; }

        private const float InterpolationPeriod = 0.3f;
        private float elapsedTime;

        public NetworkObject(string gameObjectKey, Vector3 position, Quaternion rotation)
        {
            GameObjectKey = gameObjectKey;
            Position = position;
            Rotation = rotation;

            ObjectGuid = Guid.NewGuid();
        }

        public void OnBeforeSerialize()
        {
            ObjectId = ObjectGuid.ToString();

            if (values != null)
            {
                JsonOfValues = JsonConvert.SerializeObject(values, values.GetType(), new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
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
            transform.SetPositionAndRotation(Vector3.LerpUnclamped(prePosition, Position, ratio), Quaternion.LerpUnclamped(preRotation, Rotation, ratio));
        }

        public void ApplyValuesTo(in PlayerInput input)
        {
            var typeOfValues = input.Values.GetType();
            var options = new JsonSerializerSettings
            {
                Converters = { new Vector2Converter(), new Vector3Converter(), new QuaternionConverter() },
            };
            values = JsonConvert.DeserializeObject(JsonOfValues, typeOfValues, options) as PlayerInputValues;
            input.ApplyValues(values);
        }

        public void GetValuesFrom(in PlayerInput input)
            => values = input.Values;
    }
}
