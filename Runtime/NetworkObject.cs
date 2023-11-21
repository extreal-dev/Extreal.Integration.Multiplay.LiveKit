using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class NetworkObjectInfo : ISerializationCallbackReceiver
    {
        public static NetworkObjectInfo Empty => new NetworkObjectInfo();

        public Guid ObjectGuid { get; private set; }
        [SerializeField] private string objectId;

        public int GameObjectHash => gameObjectHash;
        [SerializeField, SuppressMessage("Usage", "CC0052")] private int gameObjectHash;

        public Vector3 Position => position;
        [SerializeField] private Vector3 position;

        public Quaternion Rotation => rotation;
        [SerializeField] private Quaternion rotation;

        public string Message => message;
        [SerializeField] private string message;

        public string Name => name;
        [SerializeField] private string name;

        public DateTime CreatedAt { get; private set; }
        [SerializeField] private long createdAt;

        public DateTime UpdatedAt { get; private set; }
        [SerializeField] private long updatedAt;

        private LiveKitPlayerInputValues values;
        [SerializeField] private string jsonOfValues;

        public NetworkObjectInfo()
        {
            gameObjectHash = -1;

            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        public void OnBeforeSerialize()
        {
            objectId = ObjectGuid.ToString();

            createdAt = CreatedAt.ToBinary();
            updatedAt = UpdatedAt.ToBinary();

            jsonOfValues = JsonUtility.ToJson(values);
        }

        public void OnAfterDeserialize()
        {
            ObjectGuid = new Guid(objectId);

            CreatedAt = DateTime.FromBinary(createdAt);
            UpdatedAt = DateTime.FromBinary(updatedAt);
        }

        public void UpdateInput(in LiveKitPlayerInput input)
        {
            var typeOfValues = input.GetType();
            values = JsonUtility.FromJson(jsonOfValues, typeOfValues) as LiveKitPlayerInputValues;
            input.SetValues(values);
        }

        public void UpdateBehaviour(in LiveKitPlayerInput input)
            => values = input.Values;
    }
}
