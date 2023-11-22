using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

using UnityEngine.InputSystem;
using static UnityEngine.UI.CanvasScaler;
using LiveKit;
using Extreal.Core.Logging;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitMultiplayClient : MonoBehaviour
    {
        [SerializeField] private string relayURL = "http://localhost:7881";
        [SerializeField] private string roomName = "MultiplayTest";
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public List<NetworkClient> ConnectedClients;
        public IObservable<Unit> OnConnected;
        public IObservable<Unit> OnDisconnecting;
        public IObservable<Unit> OnUnexpectedDisconnected;
        public IObservable<RemoteParticipant> OnUserConnected;
        public IObservable<RemoteParticipant> OnUserDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected;
        public IObservable<(Participant participant, GameObject playerObject)> OnPlayerSpawned;
        public IObservable<(Participant participant, string messageJson)> OnMessageReceived;

        private NetworkClient localClient;
        private LiveKitPlayerInput input;
        private NetworkObjectInfo localPlayerNetworkObjectInfo;
        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfos = new Dictionary<Guid, NetworkObjectInfo>();

        private readonly Dictionary<int, GameObject> networkObjectPrefabsDict = new Dictionary<int, GameObject>();

        private LiveKitMultiplayTransport transport;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(LiveKitMultiplayClient));

        public async UniTask ConnectAsync(string token, string url = default)
        {
            if (transport.IsConnected)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("This client is already connected.");
                }
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                url = relayURL;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={url}, token={token}");
            }

            transport = new LiveKitMultiplayTransport();
            await transport.ConnectAsync(url, token);
        }

        public async UniTask DisconnectAsync() { }

        public GameObject[] Spawn(List<GameObject> networkPrefabs)
        {
            // localPlayerNetworkObjectInfo = new NetworkObjectInfo();
            // localPlayerNetworkObjectInfo.ObjectGuid = Guid.NewGuid();
            // localPlayerNetworkObjectInfo.GameObjectHash = Utility.GetGameObjectHash(playerPrefab);
            // localPlayerNetworkObjectInfo.Name = PlayerName;

            // var localPlayer = Instantiate(
            //     playerObject,
            //     localPlayerNetworkObjectInfo.Position,
            //     localPlayerNetworkObjectInfo.Rotation
            // );
            // localClient.SetPlayerObject(localPlayer);

            // localPlayer.TryGetComponent(out input);

            // localNetworkObjectInfos.Add(localPlayerNetworkObjectInfo.ObjectGuid, localPlayerNetworkObjectInfo);
            return Array.Empty<GameObject>();
        }

        public void SendMessage(string messageName, string messageJson, DataPacketKind dataPacketKind) { }
        public async UniTask<Room[]> ListRooms() { return Array.Empty<Room>(); }
        public void DeleteRoom() { }

        private enum StatType
        {
            TXTotal,
            RXTotal,
            TXNum,
            RXNum,
        }

        public enum ControlType
        {
            None,
            FirstPerson,
            ThirdPerson,
        }

        private ulong txTotal = 0;
        private ulong rxTotal = 0;
        private ulong txNum = 0;
        private ulong rxNum = 0;

        private Queue<string> messageQueue = new Queue<string>();

        public void EnqueueMessage(string message)
        {
            messageQueue.Enqueue(message);
        }

        public void StartClient(string endpoint, string roomName)
        {

        }

        //         public async UniTask StopClient()
        //         {
        //             Utility.DebugLogger(this, "StopClient()");
        //             if (transport != null)
        //             {
        //                 // Destroy(mainPlayer);
        //                 Utility.DebugLogger(
        //                     this,
        //                     $"Delete mainPlayer.ObjectGuid: {localPlayerNetworkObjectInfo.ObjectGuid.ToString()}"
        //                 );
        //                 await transport.Close();
        //                 transport = null;

        //                 foreach (KeyValuePair<Guid, GameObject> pair in localGameObjectDict)
        //                 {
        //                     // Guid guid = pair.Key;
        //                     GameObject gameObj = pair.Value;
        //                     gameObj.Destroy();
        //                 }

        //                 localGameObjectDict.Clear();
        //                 localNetworkObjectInfos.Clear();
        //             }
        //             else
        //             {
        //                 Utility.DebugLogger(this, "pubsubClient is already disconnected.");
        //             }
        //             localPlayerNetworkObjectInfo = null;
        //         }

        //         private void Initialize()
        //         {
        //             Utility.DebugLogger(this, $"Initialize()");
        //             foreach (GameObject p in networkObjectPrefabs)
        //             {
        //                 int hash = Utility.GetGameObjectHash(p);
        //                 Debug.Log($"prefab for networkObject: {p.ToString()}, hash: {hash}");
        //                 networkObjectPrefabsDict.Add(hash, p);
        //             }
        //         }

        //         public void Start()
        //         {
        //             // ネットワークオブジェクトのハッシュを取得して辞書に格納する
        //             Initialize();
        //         }

        //         // public async UniTask Update(){
        //         public async UniTask Update()
        //         {
        //             if (transport == null)
        //                 return;

        //             // ネットワークオブジェクトの処理
        //             // リクエストキューにメッセージを追加して参加者に送信
        //             foreach (KeyValuePair<Guid, NetworkObjectInfo> pair in localNetworkObjectInfos)
        //             {
        //                 Guid guid = pair.Key;
        //                 NetworkObjectInfo networkObj = pair.Value;

        //                 // ローカルのゲームオブジェクトの情報をネットワークのオブジェクトに反映する
        //                 GameObject n = localGameObjectDict[guid];
        //                 networkObj.Position = n.transform.position;
        //                 networkObj.Rotation = n.transform.rotation;

        //                 // ToDo: 遅いので速くする方法を考えたほうが良い
        //                 // StarterAssetsInputs controlInput = n.GetComponent<StarterAssetsInputs>();
        //                 CometInputs controlInput = n.GetComponent<CometInputs>();
        //                 networkObj.UpdateBehaviour(in controlInput);

        //                 // ToDo: オブジェクト生成時刻の設定
        //                 networkObj.DateTime_UpdatedAt = DateTime.Now;
        //                 // 状態の時刻の設定
        //                 networkObj.DateTime_UpdatedAt = DateTime.Now;

        //                 // もしもメッセージがあれば含める
        //                 networkObj.Message = string.Empty;
        //                 if (messageQueue.Count > 0)
        //                 {
        //                     networkObj.Message = messageQueue.Dequeue();
        //                     // TextMessageSpawn(n, networkObj.Message);
        //                     TextMessageSpawn(n, guid.GetHashCode(), networkObj.Name, networkObj.Message);
        //                 }

        //                 Message msg = new Message(roomName, networkObj);
        //                 msg.Command = MessageCommand.Update;
        //                 msg.Payload = networkObj;

        //                 txTotal += (ulong)msg.ToJson().Length;
        //                 txNum++;

        //                 transport.RequestQueue.Enqueue(msg);
        //             }

        //             // レスポンスキューを処理
        //             // 受け取ったリクエストキューを処理する
        //             while (transport.ResponseQueueCount() > 0)
        //             {
        //                 // Message msg = pubsubClient.ResponseQueue.Dequeue();
        //                 Message msg = transport.DequeueResponse();
        //                 if (msg == null)
        //                     continue;

        //                 rxTotal += (ulong)msg.ToJson().Length;
        //                 rxNum++;
        //                 NetworkObjectInfo networkObj = msg.Payload;
        //                 // 自分のキャラクターの場合は無視
        //                 if (networkObj.ObjectGuid.ToString() == localPlayerNetworkObjectInfo.ObjectGuid.ToString())
        //                 {
        //                     continue;
        //                 }

        //                 switch (msg.Command)
        //                 {
        //                     case MessageCommand.Create:
        //                         MessageCreateObject(networkObj);
        //                         break;
        //                     case MessageCommand.Update:
        //                         MessageUpdateObject(networkObj);
        //                         break;
        //                     case MessageCommand.Delete:
        //                         MessageDeleteObject(networkObj);
        //                         break;
        //                     default:
        //                         break;
        //                 }
        // #if DEVELOPMENT_BUILD
        //                 Utility.DebugLogger(this, $"ResponseQueue: {msg.ToJson()}");
        // #endif
        //             }
        //             // リクエストキューの処理
        //             // await pubsubClient.Update();
        //             transport.Update();
        //         }

        //         private void MessageCreateObject(NetworkObjectInfo obj)
        //         {
        //             // ネットワークオブジェクトの生成
        //             string guid = obj.ObjectGuid.ToString();
        //             int hash = obj.GameObjectHash;
        //             Utility.DebugLogger(
        //                 this,
        //                 $"Time: {obj.DateTime_CreatedAt}: Create NetworkObject[{guid}], PrefabHash: {hash}"
        //             );
        //             // プレハブの取得
        //             GameObject prefab = networkObjectPrefabsDict[hash];
        //             // オブジェクトの生成
        //             if (soundSpawn != null)
        //                 AudioSource.PlayClipAtPoint(soundSpawn, obj.Position, AudioVolume);
        //             GameObject newObj = Instantiate(prefab, obj.Position, obj.Rotation);
        //             bool nameOp = newObj.TryGetComponent<PlayerNameOperation>(
        //                 out PlayerNameOperation _playerNameOperation
        //             );
        //             if (nameOp)
        //                 _playerNameOperation.SetName(obj.Name);
        //             else
        //                 Utility.DebugLogger(this, $"PlayerNameOperation is not found.");

        //             // もしPlayerInputがある場合は無効にする
        //             if (newObj.TryGetComponent<PlayerInput>(out PlayerInput playerInput))
        //                 playerInput.enabled = false;

        //             // 辞書に登録
        //             localGameObjectDict.Add(obj.ObjectGuid, newObj);
        //             return;
        //         }

        //         private void MessageDeleteObject(NetworkObjectInfo obj)
        //         {
        //             // ネットワークオブジェクトの削除
        //             string guid = obj.ObjectGuid.ToString();
        //             Utility.DebugLogger(
        //                 this,
        //                 $"Time: {obj.DateTime_UpdatedAt}, Delete NetworkObject[{guid}]"
        //             );
        //             if (localGameObjectDict.ContainsKey(obj.ObjectGuid))
        //             {
        //                 GameObject delObj = localGameObjectDict[obj.ObjectGuid];
        //                 if (soundDespawn != null)
        //                     AudioSource.PlayClipAtPoint(
        //                         soundDespawn,
        //                         delObj.transform.position,
        //                         AudioVolume
        //                     );
        //                 // 辞書から削除
        //                 localGameObjectDict.Remove(obj.ObjectGuid);
        //                 // オブジェクトの削除
        //                 delObj.Destroy();
        //             }
        //             else
        //             {
        //                 Utility.DebugLogger(this, $"Delete NetworkObject not found[{obj.ObjectGuid}]");
        //             }
        //             return;
        //         }

        //         private void MessageUpdateObject(NetworkObjectInfo obj)
        //         {
        //             // ネットワークオブジェクトの更新
        //             string guid = obj.ObjectGuid.ToString();
        //             // Utility.DebugLogger(this, $"Update NetworkObject[{guid}]");
        //             if (localGameObjectDict.ContainsKey(obj.ObjectGuid))
        //             {
        //                 GameObject updateObj = localGameObjectDict[obj.ObjectGuid];
        //                 string dmsg0 = $"Time:{obj.DateTime_UpdatedAt}, Update NetworkObject[{guid}]";
        //                 string dmsg1 =
        //                     $"Prev: Pos({updateObj.transform.position}), Rot({updateObj.transform.rotation})";
        //                 string dmsg2 = $"Curr: Pos({obj.Position}), Rot({obj.Rotation})";
        //                 string diff_pos = (updateObj.transform.position - obj.Position).ToString();
        //                 string diff_rot = (
        //                     updateObj.transform.rotation * Quaternion.Inverse(obj.Rotation)
        //                 ).ToString();
        //                 // Utility.DebugLogger(this, $"{dmsg0}, {diff_pos}, {diff_rot}, {dmsg1}, {dmsg2}");

        //                 // 遅いのでどうにかすると良い
        //                 // StarterAssetsInputs controlInput = updateObj.GetComponent<StarterAssetsInputs>();
        //                 CometInputs controlInput = updateObj.GetComponent<CometInputs>();
        //                 // controlInputにobjの状態を反映
        //                 obj.UpdateInput(in controlInput);

        //                 // もし位置が大きくズレていたら補正
        //                 // 位置と回転の更新
        //                 if (Vector3.Distance(updateObj.transform.position, obj.Position) > 0.0f)
        //                 {
        //                     // Botの場合は位置補正をしない
        //                     if (obj.Position == Vector3.zero)
        //                     { }
        //                     else
        //                     {
        //                         mainContext.Post(
        //                             _ =>
        //                             {
        //                                 // メインスレッドでないとtransformは更新できない
        //                                 updateObj.transform.position = obj.Position;
        //                                 updateObj.transform.rotation = obj.Rotation;
        //                             },
        //                             null
        //                         );
        //                         // Utility.DebugLogger(this, $"Update NetworkObject[{guid}], Position corrected");
        //                     }
        //                 }

        //                 // メッセージの処理
        //                 TextMessageSpawn(updateObj, obj.ObjectGuid.GetHashCode(), obj.Name, obj.Message);
        //             }
        //             else
        //                 Utility.DebugLogger(
        //                     this,
        //                     $"Error Update: NetworkObject not found[{obj.ObjectGuid}]"
        //                 );

        //             return;
        //         }

        // #if UNITY_EDITOR
        //         public async void OnApplicationQuit()
        //         {
        //             await StopClient();
        //         }
        // #endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    public class LiveKitMultiplayTransport : NativeLiveKitMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    }
#else
    public class LiveKitMultiplayTransport : WebGLLiveKitMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    };
#endif
}
