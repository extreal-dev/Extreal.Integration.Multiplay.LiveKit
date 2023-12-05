import express from 'express';
import { createServer } from 'http';
import cors from 'cors';
import 'dotenv/config';

import { Server, Socket } from 'socket.io';
import { createAdapter } from '@socket.io/redis-adapter';
import { createClient } from 'redis';
import * as promClient from 'prom-client';
import { Interface } from 'readline';

const appPort = Number(process.env.APP_PORT) || 3030;
const apiPort = Number(process.env.API_PORT) || 3031;
const redisHost = process.env.REDIS_HOST || 'localhost';
const redisPort = Number(process.env.REDIS_PORT) || 7379;

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

type Message = {
    toUserIdentity: string;
}

type ListHostsResponse = {
    rooms: Room[];
};

type Room = {
    id: string;
    name: string;
};

const httpServer = createServer(app);

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

const rooms = (): Map<string, Set<string>> => {
    // @ts-ignore See https://socket.io/docs/v4/rooms/#implementation-details
    return io.sockets.adapter.rooms;
};

io.on('connection', async (socket: Socket) => {
    let roomName: string = "";
    let userIdentity: string = "";

    socket.on('join', async (receivedUserIdentity: string, receivedRoomName: string) => {
        console.log("!!!: " + receivedRoomName);
        roomName = receivedRoomName;
        userIdentity = receivedUserIdentity;
        // ルームに加入する場合(初回の接続)
        console.log('---Join         , id[%s] : Room[%s]', userIdentity, roomName);
        await redisClient.set(receivedUserIdentity, socket.id.toString());
        await socket.join(roomName); // ルームへ加入
        // ルームにいる他のクライアントにユーザが参加したことを通知
        socket.to(roomName).emit('user connected', userIdentity);

        return;
    });

    socket.on('message', async (msg: string) => {
        const msgJson: Message = JSON.parse(msg);
        if (msgJson.toUserIdentity) {
            const socketId = await redisClient.get(msgJson.toUserIdentity);
            if (socketId) {
                socket.to(socketId).emit('message', msg)
            }
        }
        if (roomName) {
            socket.to(roomName).emit('message', msg);
        }
    });

    socket.on('list rooms',  (callback: (response: ListHostsResponse) => void) => {
        callback({
            rooms: [...rooms().entries()]
                .filter((entry) => !entry[1].has(entry[0]))
                .map((entry) => ({ name: entry[0], id: [...entry[1]][0] })),
        });
        console.log("!!! list rooms" + JSON.stringify({
            rooms: [...rooms().entries()]
                .filter((entry) => !entry[1].has(entry[0]))
                .map((entry) => ({ name: entry[0], id: [...entry[1]][0] })),
        }));
    });

    // 切断
    socket.on('disconnect', () => {
        if (roomName) {
            socket.to(roomName).emit('user disconnecting', userIdentity);
            socket.leave(roomName);
            console.log(`disconnected[${socket.id}]`);
        }
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
        console.log(`=================================Restarted======================================`);
    });