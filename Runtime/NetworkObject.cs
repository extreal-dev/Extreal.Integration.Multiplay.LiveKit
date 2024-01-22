using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Multiplay.Messaging
{
    [Serializable]
    public class NetworkObject : ISerializationCallbackReceiver
    {
        public Guid ObjectGuid { get; private set; }
        [SerializeField] private string objectId;

        public string GameObjectKey => gameObjectKey;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private string gameObjectKey;

        public Vector3 Position => position;
        [SerializeField] private Vector3 position;
        private Vector3 prePosition;

        public Quaternion Rotation => rotation;
        [SerializeField] private Quaternion rotation;
        private Quaternion preRotation;

        private PlayerInputValues values;
        [SerializeField] private string jsonOfValues;

        public NetworkObject(string gameObjectKey, Vector3 position, Quaternion rotation)
        {
            this.gameObjectKey = gameObjectKey;
            this.position = position;
            this.rotation = rotation;

            ObjectGuid = Guid.NewGuid();
        }

        public void OnBeforeSerialize()
        {
            objectId = ObjectGuid.ToString();

            if (values != null)
            {
                jsonOfValues = JsonUtility.ToJson(values);
            }
        }

        public void OnAfterDeserialize()
            => ObjectGuid = new Guid(objectId);

        public bool CheckWhetherToSendData()
            => position != prePosition || rotation != preRotation || (values != null && values.CheckWhetherToSendData());

        public void GetTransformFrom(Transform transform)
        {
            prePosition = position;
            position = transform.position;

            preRotation = rotation;
            rotation = transform.rotation;
        }

        public void ApplyValuesTo(in PlayerInput input)
        {
            var typeOfValues = input.Values.GetType();
            values = JsonUtility.FromJson(jsonOfValues, typeOfValues) as PlayerInputValues;
            input.ApplyValues(values);
        }

        public void GetValuesFrom(in PlayerInput input)
            => values = input.Values;
    }
}
