import express from 'express';
import { createServer } from 'http';
import cors from 'cors';
import 'dotenv/config';

import { Server, Socket } from 'socket.io';
import { createAdapter } from '@socket.io/redis-adapter';
import { createClient } from 'redis';
import * as promClient from 'prom-client';

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

const app = express();
app.use(express.json());
// CROS対応
app.use(cors());
app.get('/', (req, res) => {
    res.send('OK');
});
app.get('/metrics', async (req, res) => {
    res.setHeader('Content-Type', register.contentType);
    const metrics = await register.metrics();
    res.send(metrics);
});

const httpServer = createServer(app);

class Vector3 {
    X: number;
    Y: number;
    Z: number;
    constructor(X: number, Y: number, Z: number) {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
}

class Vector2 {
    X: number;
    Y: number;
    constructor(X: number, Y: number) {
        this.X = X;
        this.Y = Y;
    }
}

class Quaternion {
    X: number;
    Y: number;
    Z: number;
    W: number;
    constructor(X: number, Y: number, Z: number, W: number) {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.W = W;
    }
}

class NetworkObject {
    ObjectId: string;
    GameObjectHash: number;
    Position: Vector3;
    Rotation: Quaternion;
    PlayerInput_Move: Vector2;
    PlayerInput_Look: Vector2;
    PlayerInput_Jump: boolean;
    PlayerInput_Sprint: boolean;
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

type MessageCommand = (typeof MessageCommand)[keyof typeof MessageCommand];

class Message {
    Topic: string;
    Command: MessageCommand;
    Payload: NetworkObject;
    constructor(Topic: string, Payload: NetworkObject) {
        this.Topic = Topic;
        this.Command = 0;
        this.Payload = Payload;
    }
    ToJson() {
        return JSON.stringify(this);
    }
}

const io = new Server(httpServer, {
    allowEIO3: true,
    cors: {
        origin: '*'
    }
});
// ioMiddleware = require('socketio-wildcard')();
// io.use(ioMiddleware);

const redisUrl = `redis://${redisHost}:${redisPort}`;
const pubClient = createClient({ url: redisUrl }).on(
    'error',
    (err) => {
        console.error('Redis pubClient Error:%o', err);
        process.exit(1);
    }
);
app.listen(apiPort, () => {
    console.log(`Start on port ${apiPort}`);
});
const subClient = pubClient.duplicate();
subClient.on('error', (err) => {
    console.log('Redis subClient Error', err);
});
io.adapter(createAdapter(pubClient, subClient)); // redis-adapter

io.on('connection', async (socket: Socket) => {
    // messageイベントリスナ
    socket.on('RoomMessage', async (msg: string) => {
        const msgObj: Message = JSON.parse(msg) as Message;
        // console.log('RoomMessage: %o', msgObj);
        // ルームに加入する場合(初回の接続)
        if (msgObj.Command === MessageCommand.Create) {
            const roomName = msgObj.Topic;
            console.log('Join[%s] Room: %s', socket.id, roomName);

            await socket.join(roomName); // ルームへ加入
            // 状態を保存
            await redisClient.set(socket.id.toString(), msg);
            // ルームにいるクライアントにユーザが参加したことを通知
            io.to(roomName).emit('onRoomMessage', msg);

            // すでにルームへ参加しているユーザの生成情報を，接続したクライアントに通知
            const clients = io.sockets.adapter.rooms.get(roomName);
            if (clients !== undefined) {
                for (const c of clients) {
                    const v = await redisClient.get(c);
                    console.log('Notify Spawn existing client: %o, %o', c, v);
                    if (v !== null) {
                        const newObj: Message = JSON.parse(v) as Message;
                        newObj.Command = MessageCommand.Create;
                        const jsonStr = JSON.stringify(newObj);
                        io.to(socket.id).emit('onRoomMessage', jsonStr);
                    }
                }
            } else {
                console.log('No clients');
            }
        } else if (msgObj.Command === MessageCommand.Update) {
            // ルーム内の他の参加者へメッセージを送信
            await redisClient.set(socket.id.toString(), msg);
            io.to(msgObj.Topic).emit('onRoomMessage', msg);
        } else if (msgObj.Command === MessageCommand.Delete) {
            // ルームから離脱
        } else {
            console.log('Unknown Command: %o', msgObj.Command);
            return;
        }
    });

    // 切断前に，ルームから退出したことを他の参加者に通知
    socket.on('disconnecting', async () => {
        const rooms = socket.rooms;
        console.log(`disconnecting[${socket.id}]`);
        // the Set contains at least the socket ID

        const msg = await redisClient.get(socket.id.toString());
        if (msg === null) return;
        const newObj: Message = JSON.parse(msg) as Message;
        newObj.Command = MessageCommand.Delete;
        const jsonStr = JSON.stringify(newObj);

        console.log('disconnect with socket status %o to %o', jsonStr, rooms);

        await Promise.all(
            [...rooms].map((room: string) => {
                if (room === socket.id) {
                    // 自分自身のルームは除外．disconnect時に自動的に削除される
                    console.log("[%o] don't leave self room.", socket.id);
                } else {
                    // ルームから退出したことを他の参加者に通知
                    console.log(
                        '[%o] leave room [%o]',
                        socket.id.toString(),
                        room
                    );
                    io.to(room).emit('onRoomMessage', jsonStr);
                }
            })
        );

        await redisClient.del(socket.id.toString());
        await redisClient.quit();
    });

    // 切断
    socket.on('disconnect', () => {
        console.log(`disconnected[${socket.id}]`);
    });

    // 接続開始時の処理
    const redisClient = createClient({ url: redisUrl }).on(
        'error',
        (err) => {
            console.error('Redis Client Error:%o', err);
            process.exit(1);
        }
    );
    await redisClient.connect();
    console.log(`worker: connected id: ${socket.id}`);
    // ルームを作成: 自分のルームが自動的に作られる
    await socket.join(socket.id);
});

Promise.all([pubClient.connect(), subClient.connect()])
    .then(() => {
        io.adapter(createAdapter(pubClient, subClient));
        io.listen(appPort);
    })
    .catch((err) => {
        console.error('Socket.io Listen Error: %o', err);
    })
    .finally(() => {
        console.log(`Socket.io Listen: ${appPort}`);
    });