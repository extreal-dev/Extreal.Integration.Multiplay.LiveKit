using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

using System.Threading;

using UnityEngine.InputSystem;
using static UnityEngine.UI.CanvasScaler;
using LiveKit;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitMultiplayClient : MonoBehaviour
    {
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

        public async UniTask ConnectAsync(string url, string token) { }
        public async UniTask DisconnectAsync() { }
        public GameObject[] Spawn(List<GameObject> networkPrefabs) { return Array.Empty<GameObject>(); }
        public void SendMessage(string messageName, string messageJson, DataPacketKind dataPacketKind) { }

        // private enum StatType
        // {
        //     TXTotal,
        //     RXTotal,
        //     TXNum,
        //     RXNum,
        // }

        // public enum ControlType
        // {
        //     None,
        //     FirstPerson,
        //     ThirdPerson,
        // }

        // [SerializeField]
        // public int playerPrefabIndex = 0;

        // [SerializeField]
        // public GameObject[] networkObjectPrefabs = new GameObject[0];

        // [SerializeField]
        // public string RelayURL = "ws://localhost:3000";

        // [SerializeField]
        // public string RoomName = "JSRoom2";

        // [SerializeField]
        // public string PlayerName = "UnityPlayer";


        // [SerializeField]
        // public GameObject messageBubblePrefab = null;

        // [SerializeField]
        // public AudioClip soundSpawn = null;

        // [SerializeField]
        // public AudioClip soundDespawn = null;
        // public float AudioVolume = 0.5f;

        // // ネットワーク共有されるプレハブを格納した辞書 <int GameObjectHash, GameObject>
        // private Dictionary<int, GameObject> networkObjectPrefabsDict =
        //     new Dictionary<int, GameObject>();

        // // 自分が主たる持ち主であるネットワークオブジェクトを格納する辞書
        // private Dictionary<Guid, NetworkObject> networkObjectDict =
        //     new Dictionary<Guid, NetworkObject>();

        // // ローカルのゲームオブジェクトを格納する辞書
        // /*
        // private Dictionary<Guid, GameObject> localGameObjectDict =
        //     new Dictionary<Guid, GameObject>();
        // */
        // private Dictionary<Guid, GameObject> localGameObjectDict = new Dictionary<Guid, GameObject>();

        // private NetworkObject mainPlayer = null;
        // private GameObject mainPlayerGameObject = null;

        // // Pub/Subクライアント
        // //         private SocketIOTransport pubsubClient = null;

        // //         // 自分のキャラクターに対するコントローラ入力の有無
        // //         private bool _hasInput = false;

        // //         // private StarterAssetsInputs _input = null;
        // //         private CometInputs _input = null;

        // //         // mainContext = SynchronizationContext.Current;
        // //         private SynchronizationContext mainContext = null;

        // //         private ulong txTotal = 0;
        // //         private ulong rxTotal = 0;
        // //         private ulong txNum = 0;
        // //         private ulong rxNum = 0;

        // //         private Queue<string> messageQueue = new Queue<string>();

        // //         public void EnqueueMessage(string message)
        // //         {
        // //             messageQueue.Enqueue(message);
        // //         }

        // //         private void TextMessageSpawn(
        // //             GameObject obj,
        // //             int textureIndex,
        // //             string playerName,
        // //             string message
        // //         )
        // //         {
        // //             float messageAlive = 8.0f;
        // //             int maxMessageLength = 140;

        // //             // 吹き出し用のプレハブが設定されていなければ何もしない
        // //             if (messageBubblePrefab == null)
        // //                 return;
        // //             // もしもメッセージが空だったら表示しない
        // //             if (message == string.Empty)
        // //                 return;

        // //             // もしも吹き出しが残っていたら削除する
        // //             // ゲームオブジェクトからTransformを取り出す
        // //             Transform tf = obj.transform.Find(messageBubblePrefab.name);
        // //             if (tf != null)
        // //                 tf.gameObject.Destroy();
        // //             else
        // //                 Utility.DebugLogger(this, $"Error: TextMessageCanvas not found");

        // //             // 吹き出しの生成
        // //             GameObject messageBubble = Instantiate(messageBubblePrefab);
        // //             Destroy(messageBubble, messageAlive);
        // //             messageBubble.transform.SetParent(obj.transform);

        // //             messageBubble.name = messageBubblePrefab.name;
        // //             if (messageBubble.TryGetComponent<SpeechBubble>(out SpeechBubble _speechBubble))
        // //             {
        // //                 if (maxMessageLength <= message.Length)
        // //                 {
        // //                     message = message.Substring(0, maxMessageLength);
        // //                 }
        // //                 _speechBubble.SetMessage(textureIndex, playerName, message);
        // //             }

        // //             return;
        // //         }

        // //         // public StarterAssetsInputs GetMainPlayerInput()
        // //         public CometInputs GetMainPlayerInput()
        // //         {
        // //             return _input;
        // //         }

        // //         public CinemachineVirtualCamera GetCinemachineVirtualCamera()
        // //         {
        // //             bool hasVcam = TryGetComponent<CinemachineVirtualCamera>(
        // //                 out CinemachineVirtualCamera _vCam
        // //             );
        // //             return hasVcam ? _vCam : null;
        // //         }

        // //         public void SetCameraTarget(GameObject target)
        // //         {
        // //             bool hasVcam = false;
        // //             // GameObject mainCamera = Camera.main.gameObject;
        // //             hasVcam = TryGetComponent<CinemachineVirtualCamera>(out CinemachineVirtualCamera _vCam);
        // //             if (hasVcam)
        // //             {
        // //                 Utility.DebugLogger(this, $"Get cinemachineVirtualCamera: {_vCam}");
        // //                 _vCam.Follow = target.transform;
        // //                 _vCam.LookAt = target.transform;
        // //             }
        // //             else
        // //                 Utility.DebugLogger(this, $"CinemachineVirtualCamera is not found.");
        // //         }

        // //         public void SwitchPersonView(ControlType controlType)
        // //         {
        // //             if (mainPlayerGameObject == null)
        // //             {
        // //                 Utility.DebugLogger(this, $"mainPlayerGameObject is null.");
        // //                 return;
        // //             }
        // //             if (
        // //                 mainPlayerGameObject.TryGetComponent<ThirdPersonController>(
        // //                     out ThirdPersonController _thirdPersonController
        // //                 )
        // //             )
        // //             {
        // //                 switch (controlType)
        // //                 {
        // //                     case ControlType.FirstPerson:
        // //                         _thirdPersonController.RotationSmoothTime = 0.3f;
        // //                         break;
        // //                     case ControlType.ThirdPerson:
        // //                         _thirdPersonController.RotationSmoothTime = 0.12f;
        // //                         break;
        // //                     default:
        // //                         break;
        // //                 }
        // //             }
        // //             return;
        // //         }

        // //         public void StartClient(string endpoint, string roomName, string playerName)
        // //         {
        // //             RelayURL = endpoint;
        // //             RoomName = roomName;
        // //             PlayerName = playerName;

        // //             Utility.DebugLogger(this, $"StartClient({endpoint}, {roomName}, {playerName})");
        // //             if (pubsubClient != null)
        // //             {
        // //                 Utility.DebugLogger(this, "pubsubClient is already connected.");
        // //                 return;
        // //             }
        // //             GameObject mainPlayerPrefab = networkObjectPrefabs[playerPrefabIndex];
        // //             mainPlayer = new NetworkObject();
        // //             mainPlayer.ObjectGuid = Guid.NewGuid();
        // //             mainPlayer.GameObjectHash = Utility.GetGameObjectHash(mainPlayerPrefab);
        // //             mainPlayer.Name = PlayerName;

        // //             // メインプレイヤーの生成
        // //             if (soundSpawn != null)
        // //                 AudioSource.PlayClipAtPoint(soundSpawn, mainPlayer.Position, AudioVolume);
        // //             mainPlayerGameObject = Instantiate(
        // //                 mainPlayerPrefab,
        // //                 mainPlayer.Position,
        // //                 mainPlayer.Rotation
        // //             );
        // //             bool hasName = mainPlayerGameObject.TryGetComponent<PlayerNameOperation>(
        // //                 out PlayerNameOperation _playerNameOperation
        // //             );
        // //             if (hasName)
        // //                 _playerNameOperation.SetName(PlayerName);
        // //             else
        // //                 Utility.DebugLogger(this, $"PlayerNameOperation is not found.");

        // //             // SetCameraTarget(mainPlayerGameObject);

        // //             // _hasInput = mainPlayerGameObject.TryGetComponent<StarterAssetsInputs>(out _input);
        // //             _hasInput = mainPlayerGameObject.TryGetComponent<CometInputs>(out _input);
        // //             if (mainPlayerGameObject.TryGetComponent<PlayerInput>(out PlayerInput _playerInput))
        // //                 _playerInput.enabled = true;
        // //             else
        // //                 Utility.DebugLogger(this, $"PlayerInput is not found.");

        // //             localGameObjectDict.Add(mainPlayer.ObjectGuid, mainPlayerGameObject);
        // //             networkObjectDict.Add(mainPlayer.ObjectGuid, mainPlayer);

        // //             pubsubClient = new SocketIOTransport(endpoint, RoomName, mainPlayer);
        // //             Task.Run(async () => await pubsubClient.Connect());
        // //             return;
        // //         }

        // //         public async UniTask StopClient()
        // //         {
        // //             Utility.DebugLogger(this, "StopClient()");
        // //             if (pubsubClient != null)
        // //             {
        // //                 // Destroy(mainPlayer);
        // //                 Utility.DebugLogger(
        // //                     this,
        // //                     $"Delete mainPlayer.ObjectGuid: {mainPlayer.ObjectGuid.ToString()}"
        // //                 );
        // //                 await pubsubClient.Close();
        // //                 pubsubClient = null;

        // //                 foreach (KeyValuePair<Guid, GameObject> pair in localGameObjectDict)
        // //                 {
        // //                     // Guid guid = pair.Key;
        // //                     GameObject gameObj = pair.Value;
        // //                     gameObj.Destroy();
        // //                 }

        // //                 localGameObjectDict.Clear();
        // //                 networkObjectDict.Clear();
        // //             }
        // //             else
        // //             {
        // //                 Utility.DebugLogger(this, "pubsubClient is already disconnected.");
        // //             }
        // //             mainPlayer = null;
        // //         }

        // //         private void Initialize()
        // //         {
        // //             Utility.DebugLogger(this, $"Initialize()");
        // //             foreach (GameObject p in networkObjectPrefabs)
        // //             {
        // //                 int hash = Utility.GetGameObjectHash(p);
        // //                 Debug.Log($"prefab for networkObject: {p.ToString()}, hash: {hash}");
        // //                 networkObjectPrefabsDict.Add(hash, p);
        // //             }
        // //         }

        // //         public void Start()
        // //         {
        // //             // ネットワークオブジェクトのハッシュを取得して辞書に格納する
        // //             Initialize();
        // //             mainContext = SynchronizationContext.Current;
        // //         }

        // //         // public async UniTask Update(){
        // //         public async UniTask Update()
        // //         {
        // //             if (pubsubClient == null)
        // //                 return;

        // //             // ネットワークオブジェクトの処理
        // //             // リクエストキューにメッセージを追加して参加者に送信
        // //             foreach (KeyValuePair<Guid, NetworkObject> pair in networkObjectDict)
        // //             {
        // //                 Guid guid = pair.Key;
        // //                 NetworkObject networkObj = pair.Value;

        // //                 // ローカルのゲームオブジェクトの情報をネットワークのオブジェクトに反映する
        // //                 GameObject n = localGameObjectDict[guid];
        // //                 networkObj.Position = n.transform.position;
        // //                 networkObj.Rotation = n.transform.rotation;

        // //                 // ToDo: 遅いので速くする方法を考えたほうが良い
        // //                 // StarterAssetsInputs controlInput = n.GetComponent<StarterAssetsInputs>();
        // //                 CometInputs controlInput = n.GetComponent<CometInputs>();
        // //                 networkObj.UpdateBehaviour(in controlInput);

        // //                 // ToDo: オブジェクト生成時刻の設定
        // //                 networkObj.DateTime_UpdatedAt = DateTime.Now;
        // //                 // 状態の時刻の設定
        // //                 networkObj.DateTime_UpdatedAt = DateTime.Now;

        // //                 // もしもメッセージがあれば含める
        // //                 networkObj.Message = string.Empty;
        // //                 if (messageQueue.Count > 0)
        // //                 {
        // //                     networkObj.Message = messageQueue.Dequeue();
        // //                     // TextMessageSpawn(n, networkObj.Message);
        // //                     TextMessageSpawn(n, guid.GetHashCode(), networkObj.Name, networkObj.Message);
        // //                 }

        // //                 Message msg = new Message(RoomName, networkObj);
        // //                 msg.Command = MessageCommand.Update;
        // //                 msg.Payload = networkObj;

        // //                 txTotal += (ulong)msg.ToJson().Length;
        // //                 txNum++;

        // //                 pubsubClient.RequestQueue.Enqueue(msg);
        // //             }

        // //             // レスポンスキューを処理
        // //             // 受け取ったリクエストキューを処理する
        // //             while (pubsubClient.ResponseQueueCount() > 0)
        // //             {
        // //                 // Message msg = pubsubClient.ResponseQueue.Dequeue();
        // //                 Message msg = pubsubClient.DequeueResponse();
        // //                 if (msg == null)
        // //                     continue;

        // //                 rxTotal += (ulong)msg.ToJson().Length;
        // //                 rxNum++;
        // //                 NetworkObject networkObj = msg.Payload;
        // //                 // 自分のキャラクターの場合は無視
        // //                 if (networkObj.ObjectGuid.ToString() == mainPlayer.ObjectGuid.ToString())
        // //                 {
        // //                     continue;
        // //                 }

        // //                 switch (msg.Command)
        // //                 {
        // //                     case MessageCommand.Create:
        // //                         MessageCreateObject(networkObj);
        // //                         break;
        // //                     case MessageCommand.Update:
        // //                         MessageUpdateObject(networkObj);
        // //                         break;
        // //                     case MessageCommand.Delete:
        // //                         MessageDeleteObject(networkObj);
        // //                         break;
        // //                     default:
        // //                         break;
        // //                 }
        // // #if DEVELOPMENT_BUILD
        // //                 Utility.DebugLogger(this, $"ResponseQueue: {msg.ToJson()}");
        // // #endif
        // //             }
        // //             // リクエストキューの処理
        // //             // await pubsubClient.Update();
        // //             await pubsubClient.Update();
        // //         }

        // //         private void MessageCreateObject(NetworkObject obj)
        // //         {
        // //             // ネットワークオブジェクトの生成
        // //             string guid = obj.ObjectGuid.ToString();
        // //             int hash = obj.GameObjectHash;
        // //             Utility.DebugLogger(
        // //                 this,
        // //                 $"Time: {obj.DateTime_CreatedAt}: Create NetworkObject[{guid}], PrefabHash: {hash}"
        // //             );
        // //             // プレハブの取得
        // //             GameObject prefab = networkObjectPrefabsDict[hash];
        // //             // オブジェクトの生成
        // //             if (soundSpawn != null)
        // //                 AudioSource.PlayClipAtPoint(soundSpawn, obj.Position, AudioVolume);
        // //             GameObject newObj = Instantiate(prefab, obj.Position, obj.Rotation);
        // //             bool nameOp = newObj.TryGetComponent<PlayerNameOperation>(
        // //                 out PlayerNameOperation _playerNameOperation
        // //             );
        // //             if (nameOp)
        // //                 _playerNameOperation.SetName(obj.Name);
        // //             else
        // //                 Utility.DebugLogger(this, $"PlayerNameOperation is not found.");

        // //             // もしPlayerInputがある場合は無効にする
        // //             if (newObj.TryGetComponent<PlayerInput>(out PlayerInput playerInput))
        // //                 playerInput.enabled = false;

        // //             // 辞書に登録
        // //             localGameObjectDict.Add(obj.ObjectGuid, newObj);
        // //             return;
        // //         }

        // //         private void MessageDeleteObject(NetworkObject obj)
        // //         {
        // //             // ネットワークオブジェクトの削除
        // //             string guid = obj.ObjectGuid.ToString();
        // //             Utility.DebugLogger(
        // //                 this,
        // //                 $"Time: {obj.DateTime_UpdatedAt}, Delete NetworkObject[{guid}]"
        // //             );
        // //             if (localGameObjectDict.ContainsKey(obj.ObjectGuid))
        // //             {
        // //                 GameObject delObj = localGameObjectDict[obj.ObjectGuid];
        // //                 if (soundDespawn != null)
        // //                     AudioSource.PlayClipAtPoint(
        // //                         soundDespawn,
        // //                         delObj.transform.position,
        // //                         AudioVolume
        // //                     );
        // //                 // 辞書から削除
        // //                 localGameObjectDict.Remove(obj.ObjectGuid);
        // //                 // オブジェクトの削除
        // //                 delObj.Destroy();
        // //             }
        // //             else
        // //             {
        // //                 Utility.DebugLogger(this, $"Delete NetworkObject not found[{obj.ObjectGuid}]");
        // //             }
        // //             return;
        // //         }

        // //         private void MessageUpdateObject(NetworkObject obj)
        // //         {
        // //             // ネットワークオブジェクトの更新
        // //             string guid = obj.ObjectGuid.ToString();
        // //             // Utility.DebugLogger(this, $"Update NetworkObject[{guid}]");
        // //             if (localGameObjectDict.ContainsKey(obj.ObjectGuid))
        // //             {
        // //                 GameObject updateObj = localGameObjectDict[obj.ObjectGuid];
        // //                 string dmsg0 = $"Time:{obj.DateTime_UpdatedAt}, Update NetworkObject[{guid}]";
        // //                 string dmsg1 =
        // //                     $"Prev: Pos({updateObj.transform.position}), Rot({updateObj.transform.rotation})";
        // //                 string dmsg2 = $"Curr: Pos({obj.Position}), Rot({obj.Rotation})";
        // //                 string diff_pos = (updateObj.transform.position - obj.Position).ToString();
        // //                 string diff_rot = (
        // //                     updateObj.transform.rotation * Quaternion.Inverse(obj.Rotation)
        // //                 ).ToString();
        // //                 // Utility.DebugLogger(this, $"{dmsg0}, {diff_pos}, {diff_rot}, {dmsg1}, {dmsg2}");

        // //                 // 遅いのでどうにかすると良い
        // //                 // StarterAssetsInputs controlInput = updateObj.GetComponent<StarterAssetsInputs>();
        // //                 CometInputs controlInput = updateObj.GetComponent<CometInputs>();
        // //                 // controlInputにobjの状態を反映
        // //                 obj.UpdateInput(in controlInput);

        // //                 // もし位置が大きくズレていたら補正
        // //                 // 位置と回転の更新
        // //                 if (Vector3.Distance(updateObj.transform.position, obj.Position) > 0.0f)
        // //                 {
        // //                     // Botの場合は位置補正をしない
        // //                     if (obj.Position == Vector3.zero)
        // //                     { }
        // //                     else
        // //                     {
        // //                         mainContext.Post(
        // //                             _ =>
        // //                             {
        // //                                 // メインスレッドでないとtransformは更新できない
        // //                                 updateObj.transform.position = obj.Position;
        // //                                 updateObj.transform.rotation = obj.Rotation;
        // //                             },
        // //                             null
        // //                         );
        // //                         // Utility.DebugLogger(this, $"Update NetworkObject[{guid}], Position corrected");
        // //                     }
        // //                 }

        // //                 // メッセージの処理
        // //                 TextMessageSpawn(updateObj, obj.ObjectGuid.GetHashCode(), obj.Name, obj.Message);
        // //             }
        // //             else
        // //                 Utility.DebugLogger(
        // //                     this,
        // //                     $"Error Update: NetworkObject not found[{obj.ObjectGuid}]"
        // //                 );

        // //             return;
        // //         }

        // //         public ulong getStats(StatType type)
        // //         {
        // //             ulong ret = 0;
        // //             ret = (type == StatType.TXTotal) ? txTotal : ret;
        // //             ret = (type == StatType.RXTotal) ? rxTotal : ret;
        // //             ret = (type == StatType.TXNum) ? txNum : ret;
        // //             ret = (type == StatType.RXNum) ? rxNum : ret;
        // //             return ret;
        // //         }

        // // #if UNITY_EDITOR
        // //         public async void OnApplicationQuit()
        // //         {
        // //             await StopClient();
        // //         }
        // // #endif
    }

    // #if !UNITY_WEBGL || UNITY_EDITOR
    //     public class SocketIOTransport : NativeSocketIOTransport, ISocketIOTransport
    //     {
    //         public SocketIOTransport(string url, string roomName, NetworkObject networkObj)
    //             : base(url, roomName, networkObj) { }
    //     }
    // #endif
    // #if UNITY_WEBGL && !UNITY_EDITOR
    //         public class SocketIOTransport : JSSocketIOTransport, ISocketIOTransport
    //         {
    //             public SocketIOTransport(string url, string roomName, NetworkObject networkObj)
    //                 : base(url, roomName, networkObj) { }
    //         };
    // #endif
}
