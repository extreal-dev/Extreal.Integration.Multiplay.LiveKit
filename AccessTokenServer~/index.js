import express from 'express';
import cors from 'cors';
import { AccessToken } from 'livekit-server-sdk';

const app = express();
const port = 3000;
const API_KEY = process.env.API_KEY;
const SECRET_KEY = process.env.SECRET_KEY;

// CORS設定を追加
app.use(cors({
    origin: '*',
    methods: ['GET'],
    credentials: true
}));

app.get('/getToken', async (req, res) => {
    const roomName = req.query.RoomName;
    const participantName = req.query.ParticipantName;

    if (!roomName || !participantName) {
        return res.status(400).json({
            message: "query is not valid.",
        });
    }

    const at = new AccessToken(API_KEY, SECRET_KEY, {
        identity: participantName,
    });
    at.addGrant({ roomJoin: true, room: roomName, canPublish: true, canSubscribe: true });

    const token = at.toJwt();

    res.json({
        RoomName: roomName,
        ParticipantName: participantName,
        AccessToken: token,
    });
});

app.listen(port, () => {
    console.log(`Server listening on port ${port}`);
});
