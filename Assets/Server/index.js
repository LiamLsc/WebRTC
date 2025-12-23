const WebSocket = require("ws");
const RoomManager = require("./roomManager");

const PORT = 3000;
const wss = new WebSocket.Server({ port: PORT });
const roomManager = new RoomManager();

console.log(`WebRTC Signaling Server running on ws://0.0.0.0:${PORT}`);

wss.on("connection", (ws) => {
    ws.roomId = null;

    ws.on("message", (data) => {
        let msg;
        try {
            msg = JSON.parse(data.toString());
        } catch {
            return;
        }

        switch (msg.type) {

            case "create-room": {
                const roomId = roomManager.createRoom();
                ws.roomId = roomId;
                roomManager.joinRoom(roomId, ws);

                ws.send(JSON.stringify({
                    type: "room-created",
                    roomId
                }));
                break;
            }

            case "join-room": {
                const success = roomManager.joinRoom(msg.roomId, ws);
                if (!success) {
                    ws.send(JSON.stringify({
                        type: "error",
                        message: "Room not found"
                    }));
                    return;
                }

                ws.roomId = msg.roomId;

                ws.send(JSON.stringify({
                    type: "room-joined",
                    roomId: msg.roomId
                }));

                roomManager.broadcast(msg.roomId, ws, {
                    type: "peer-joined"
                });
                break;
            }

            case "offer":
            case "answer":
            case "ice-candidate": {
                if (!ws.roomId) return;

                roomManager.broadcast(ws.roomId, ws, msg);
                break;
            }
        }
    });

    ws.on("close", () => {
        if (ws.roomId) {
            roomManager.leaveRoom(ws.roomId, ws);
        }
    });
});
