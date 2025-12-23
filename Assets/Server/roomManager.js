const { v4: uuidv4 } = require("uuid");

class RoomManager {
    constructor() {
        this.rooms = new Map();
    }

    createRoom() {
        const roomId = uuidv4();
        this.rooms.set(roomId, new Set());
        return roomId;
    }

    joinRoom(roomId, client) {
        if (!this.rooms.has(roomId)) {
            return false;
        }
        this.rooms.get(roomId).add(client);
        return true;
    }

    leaveRoom(roomId, client) {
        if (!this.rooms.has(roomId)) return;

        const room = this.rooms.get(roomId);
        room.delete(client);

        if (room.size === 0) {
            this.rooms.delete(roomId);
        }
    }

    broadcast(roomId, sender, message) {
        if (!this.rooms.has(roomId)) return;

        for (const client of this.rooms.get(roomId)) {
            if (client !== sender && client.readyState === 1) {
                client.send(JSON.stringify(message));
            }
        }
    }
}

module.exports = RoomManager;
