using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class NetworkObjectInfo : ISerializationCallbackReceiver
    {
        public Guid ObjectGuid { get; private set; }
        [SerializeField] private string objectId;

        public int InstanceId => instanceId;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int instanceId;

        public Vector3 Position => position;
        [SerializeField] private Vector3 position;

        public Quaternion Rotation => rotation;
        [SerializeField] private Quaternion rotation;

        public DateTime CreatedAt { get; private set; }
        [SerializeField] private long createdAt;

        public DateTime UpdatedAt { get; private set; }
        [SerializeField] private long updatedAt;

        private MultiplayPlayerInputValues values;
        [SerializeField] private string jsonOfValues;

        public NetworkObjectInfo(int instanceId, Vector3 position, Quaternion rotation)
        {
            this.instanceId = instanceId;
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

        public void Updated()
            => UpdatedAt = DateTime.UtcNow;

        public void GetTransformFrom(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
        }

        public void ApplyValuesTo(in LiveKitPlayerInput input)
        {
            if (string.IsNullOrEmpty(jsonOfValues))
            {
                return;
            }

            var typeOfValues = input.GetType();
            values = JsonUtility.FromJson(jsonOfValues, typeOfValues) as MultiplayPlayerInputValues;
            input.SetValues(values);
        }

        public void GetValuesFrom(in LiveKitPlayerInput input)
            => values = input.Values;
    }
}
