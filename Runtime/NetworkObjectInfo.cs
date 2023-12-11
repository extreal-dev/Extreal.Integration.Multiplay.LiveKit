using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Multiplay.Common
{
    [Serializable]
    public class NetworkObjectInfo : ISerializationCallbackReceiver
    {
        public Guid ObjectGuid { get; private set; }
        [SerializeField] private string objectId;

        public int GameObjectHash => gameObjectHash;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int gameObjectHash;

        public Vector3 Position => position;
        [SerializeField] private Vector3 position;
        private Vector3 prePosition;

        public Quaternion Rotation => rotation;
        [SerializeField] private Quaternion rotation;
        private Quaternion preRotation;

        public DateTime CreatedAt { get; private set; }
        [SerializeField] private long createdAt;

        public DateTime UpdatedAt { get; private set; }
        [SerializeField] private long updatedAt;

        private MultiplayPlayerInputValues values;
        [SerializeField] private string jsonOfValues;

        public NetworkObjectInfo(int gameObjectHash, Vector3 position, Quaternion rotation)
        {
            this.gameObjectHash = gameObjectHash;
            this.position = position;
            this.rotation = rotation;

            ObjectGuid = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void OnBeforeSerialize()
        {
            objectId = ObjectGuid.ToString();

            createdAt = CreatedAt.ToBinary();
            updatedAt = UpdatedAt.ToBinary();

            if (values != null)
            {
                jsonOfValues = JsonUtility.ToJson(values);
            }
        }

        public void OnAfterDeserialize()
        {
            ObjectGuid = new Guid(objectId);

            CreatedAt = DateTime.FromBinary(createdAt);
            UpdatedAt = DateTime.FromBinary(updatedAt);
        }

        public bool CheckWhetherToSendData()
            => position != prePosition || rotation != preRotation || (values != null && values.CheckWhetherToSendData());

        public void Updated()
            => UpdatedAt = DateTime.UtcNow;

        public void GetTransformFrom(Transform transform)
        {
            prePosition = position;
            position = transform.position;

            preRotation = rotation;
            rotation = transform.rotation;
        }

        public void ApplyValuesTo(in MultiplayPlayerInput input)
        {
            if (string.IsNullOrEmpty(jsonOfValues))
            {
                return;
            }

            var typeOfValues = input.Values.GetType();
            values = JsonUtility.FromJson(jsonOfValues, typeOfValues) as MultiplayPlayerInputValues;
            input.SetValues(values);
        }

        public void GetValuesFrom(in MultiplayPlayerInput input)
            => values = input.Values;
    }
}
