using UnityEngine;
using System;
using System.Reflection;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class NetworkObject : ISerializationCallbackReceiver
    {
        public static NetworkObject Empty
        {
            get { return new NetworkObject(); }
        }

        [NonSerialized]
        public Guid ObjectGuid;

        [SerializeField]
        private string ObjectId;

        public int GameObjectHash;
        public Vector3 Position;
        public Quaternion Rotation;

        public string Message;
        public string Name;

        [NonSerialized]
        public DateTime DateTime_CreatedAt;

        [SerializeField]
        private long CreatedAt;

        [NonSerialized]
        public DateTime DateTime_UpdatedAt;

        [SerializeField]
        private long UpdatedAt;

        [NonSerialized]
        public LiveKitPlayerInputValues Values;
        public string TypeNameOfValues;
        public string JsonOfValues;

        public NetworkObject()
        {
            this.ObjectGuid = Guid.Empty;
            this.GameObjectHash = -1;
            this.Position = Vector3.zero;
            this.Rotation = Quaternion.identity;

            this.DateTime_CreatedAt = DateTime.Now;
            this.DateTime_UpdatedAt = DateTime.Now;

            this.Message = string.Empty;

            this.Name = string.Empty;
        }

        public void OnBeforeSerialize()
        {
            ObjectId = ObjectGuid.ToString();

            CreatedAt = DateTime_CreatedAt.ToBinary();
            UpdatedAt = DateTime_UpdatedAt.ToBinary();

            TypeNameOfValues = Values.GetType().FullName;
            JsonOfValues = JsonUtility.ToJson(Values);
        }

        public void OnAfterDeserialize()
        {
            ObjectGuid = new Guid(ObjectId);

            DateTime_CreatedAt = DateTime.FromBinary(CreatedAt);
            DateTime_UpdatedAt = DateTime.FromBinary(UpdatedAt);

            var typeOfValues = Assembly.GetExecutingAssembly().GetType(TypeNameOfValues);
            Values = JsonUtility.FromJson(JsonOfValues, typeOfValues) as LiveKitPlayerInputValues;
        }

        public void UpdateInput(in LiveKitPlayerInput input)
            => input.SetValues(Values);

        public void UpdateBehaviour(in LiveKitPlayerInput input)
            => Values = input.Values;
    }
}
