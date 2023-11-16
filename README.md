# Extreal.Integration.Multiplay.LiveKit
## Local LiveKit Server Setup
- Step 1: Download LiveKit Server
Download the server binary file for your OS from [LiveKit Installation Guide](https://github.com/livekit/livekit#install).

- Step 2: Start LiveKit Server
Run the server in development mode:
```bash
livekit-server --dev
```
- Step 3: Start AccessToken Server
In the AccessTokenServer directory, launch the token server:
```bash
docker compose up -d
```