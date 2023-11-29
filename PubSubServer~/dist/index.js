"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = __importDefault(require("express"));
const http_1 = require("http");
const cors_1 = __importDefault(require("cors"));
require("dotenv/config");
const socket_io_1 = require("socket.io");
const redis_adapter_1 = require("@socket.io/redis-adapter");
const redis_1 = require("redis");
const promClient = __importStar(require("prom-client"));
const appPort = Number(process.env.APP_PORT) || 3000;
const apiPort = Number(process.env.API_PORT) || 3001;
const redisHost = process.env.REDIS_HOST || 'localhost';
const redisPort = Number(process.env.REDIS_PORT) || 6379;
// const promClient = require('prom-client');
const register = new promClient.Registry();
// - Default metrics are collected on scrape of metrics endpoint, not on an
//  interval. The `timeout` option to `collectDefaultMetrics(conf)` is no longer
//  supported or needed, and the function no longer returns a `Timeout` object.
promClient.collectDefaultMetrics({ register: register });
const app = (0, express_1.default)();
app.use(express_1.default.json());
// CROS対応
app.use((0, cors_1.default)());
app.get('/', (req, res) => {
    res.send('OK');
});
app.get('/metrics', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    res.setHeader('Content-Type', register.contentType);
    const metrics = yield register.metrics();
    res.send(metrics);
}));
const httpServer = (0, http_1.createServer)(app);
class Vector3 {
    constructor(X, Y, Z) {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
}
class Vector2 {
    constructor(X, Y) {
        this.X = X;
        this.Y = Y;
    }
}
class Quaternion {
    constructor(X, Y, Z, W) {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.W = W;
    }
}
class NetworkObject {
    constructor() {
        this.ObjectId = '';
        this.GameObjectHash = -1;
        this.Position = new Vector3(0.0, 0.0, 0.0);
        this.Rotation = new Quaternion(0.0, 0.0, 0.0, 0.0);
        this.PlayerInput_Move = new Vector2(0.0, 0.0);
        this.PlayerInput_Look = new Vector2(0.0, 0.0);
        this.PlayerInput_Jump = false;
        this.PlayerInput_Sprint = false;
    }
}
const MessageCommand = {
    None: 0,
    Create: 1,
    Update: 2,
    Delete: 3
};
class Message {
    constructor(Topic, Payload) {
        this.Topic = Topic;
        this.Command = 0;
        this.Payload = Payload;
    }
    ToJson() {
        return JSON.stringify(this);
    }
}
const io = new socket_io_1.Server(httpServer, {
    allowEIO3: true,
    cors: {
        origin: '*'
    }
});
// ioMiddleware = require('socketio-wildcard')();
// io.use(ioMiddleware);
const redisUrl = `redis://${redisHost}:${redisPort}`;
const pubClient = (0, redis_1.createClient)({ url: redisUrl }).on('error', (err) => {
    console.error('Redis pubClient Error:%o', err);
    process.exit(1);
});
app.listen(apiPort, () => {
    console.log(`Start on port ${apiPort}`);
});
const subClient = pubClient.duplicate();
subClient.on('error', (err) => {
    console.log('Redis subClient Error', err);
});
io.adapter((0, redis_adapter_1.createAdapter)(pubClient, subClient)); // redis-adapter
io.on('connection', (socket) => __awaiter(void 0, void 0, void 0, function* () {
    // messageイベントリスナ
    socket.on('RoomMessage', (msg) => __awaiter(void 0, void 0, void 0, function* () {
        const msgObj = JSON.parse(msg);
        // console.log('RoomMessage: %o', msgObj);
        // ルームに加入する場合(初回の接続)
        if (msgObj.Command === MessageCommand.Create) {
            const roomName = msgObj.Topic;
            console.log('Join[%s] Room: %s', socket.id, roomName);
            yield socket.join(roomName); // ルームへ加入
            // 状態を保存
            yield redisClient.set(socket.id.toString(), msg);
            // ルームにいるクライアントにユーザが参加したことを通知
            io.to(roomName).emit('onRoomMessage', msg);
            // すでにルームへ参加しているユーザの生成情報を，接続したクライアントに通知
            const clients = io.sockets.adapter.rooms.get(roomName);
            if (clients !== undefined) {
                for (const c of clients) {
                    const v = yield redisClient.get(c);
                    console.log('Notify Spawn existing client: %o, %o', c, v);
                    if (v !== null) {
                        const newObj = JSON.parse(v);
                        newObj.Command = MessageCommand.Create;
                        const jsonStr = JSON.stringify(newObj);
                        io.to(socket.id).emit('onRoomMessage', jsonStr);
                    }
                }
            }
            else {
                console.log('No clients');
            }
        }
        else if (msgObj.Command === MessageCommand.Update) {
            // ルーム内の他の参加者へメッセージを送信
            yield redisClient.set(socket.id.toString(), msg);
            io.to(msgObj.Topic).emit('onRoomMessage', msg);
        }
        else if (msgObj.Command === MessageCommand.Delete) {
            // ルームから離脱
        }
        else {
            console.log('Unknown Command: %o', msgObj.Command);
            return;
        }
    }));
    // 切断前に，ルームから退出したことを他の参加者に通知
    socket.on('disconnecting', () => __awaiter(void 0, void 0, void 0, function* () {
        const rooms = socket.rooms;
        console.log(`disconnecting[${socket.id}]`);
        // the Set contains at least the socket ID
        const msg = yield redisClient.get(socket.id.toString());
        if (msg === null)
            return;
        const newObj = JSON.parse(msg);
        newObj.Command = MessageCommand.Delete;
        const jsonStr = JSON.stringify(newObj);
        console.log('disconnect with socket status %o to %o', jsonStr, rooms);
        yield Promise.all([...rooms].map((room) => {
            if (room === socket.id) {
                // 自分自身のルームは除外．disconnect時に自動的に削除される
                console.log("[%o] don't leave self room.", socket.id);
            }
            else {
                // ルームから退出したことを他の参加者に通知
                console.log('[%o] leave room [%o]', socket.id.toString(), room);
                io.to(room).emit('onRoomMessage', jsonStr);
            }
        }));
        yield redisClient.del(socket.id.toString());
        yield redisClient.quit();
    }));
    // 切断
    socket.on('disconnect', () => {
        console.log(`disconnected[${socket.id}]`);
    });
    // 接続開始時の処理
    const redisClient = (0, redis_1.createClient)({ url: redisUrl }).on('error', (err) => {
        console.error('Redis Client Error:%o', err);
        process.exit(1);
    });
    yield redisClient.connect();
    console.log(`worker: connected id: ${socket.id}`);
    // ルームを作成: 自分のルームが自動的に作られる
    yield socket.join(socket.id);
}));
Promise.all([pubClient.connect(), subClient.connect()])
    .then(() => {
    io.adapter((0, redis_adapter_1.createAdapter)(pubClient, subClient));
    io.listen(appPort);
})
    .catch((err) => {
    console.error('Socket.io Listen Error: %o', err);
})
    .finally(() => {
    console.log(`Socket.io Listen: ${appPort}`);
});
//# sourceMappingURL=index.js.map