const { v4: uuidv4 } = require("uuid");

class RoomManager {
    constructor() {
        this.rooms = new Map();
    }

    createRoom() {
        const roomId = uuidv4().substr(0, 8); // 使用简短的房间ID
        this.rooms.set(roomId, {
            clients: new Set(),
            createdAt: Date.now(),
            lastActivity: Date.now()
        });
        console.log(`🏠 Room created: ${roomId}`);
        return roomId;
    }

    joinRoom(roomId, client) {
        if (!this.rooms.has(roomId)) {
            console.log(`❌ Room not found: ${roomId}`);
            return false;
        }
        
        const room = this.rooms.get(roomId);
        room.clients.add(client);
        room.lastActivity = Date.now();
        
        console.log(`✅ User joined room ${roomId}, total clients: ${room.clients.size}`);
        return true;
    }

    leaveRoom(roomId, client) {
        if (!this.rooms.has(roomId)) {
            return;
        }

        const room = this.rooms.get(roomId);
        room.clients.delete(client);
        room.lastActivity = Date.now();

        if (room.clients.size === 0) {
            // 不立即删除，等待清理任务
            console.log(`🏚️  Room ${roomId} is now empty`);
        } else {
            console.log(`👋 User left room ${roomId}, remaining: ${room.clients.size}`);
        }
    }

    broadcast(roomId, sender, message) {
        if (!this.rooms.has(roomId)) {
            console.log(`❌ Cannot broadcast to non-existent room: ${roomId}`);
            return;
        }

        const room = this.rooms.get(roomId);
        room.lastActivity = Date.now();
        
        let sentCount = 0;
        for (const client of room.clients) {
            if (client !== sender && client.readyState === 1) {
                try {
                    client.send(JSON.stringify(message));
                    sentCount++;
                } catch (error) {
                    console.error(`❌ Failed to send message to client:`, error);
                }
            }
        }
        
        if (sentCount > 0) {
            console.log(`📤 Broadcast ${message.type} to ${sentCount} client(s) in room ${roomId}`);
        }
    }
    
    getRoomCount() {
        let activeRooms = 0;
        for (const [roomId, room] of this.rooms) {
            if (room.clients.size > 0) {
                activeRooms++;
            }
        }
        return activeRooms;
    }
    
    getAllRoomsInfo() {
        const roomsInfo = [];
        for (const [roomId, room] of this.rooms) {
            roomsInfo.push({
                roomId,
                clientCount: room.clients.size,
                createdAt: room.createdAt,
                lastActivity: room.lastActivity,
                isActive: room.clients.size > 0
            });
        }
        return roomsInfo;
    }
    
    // 清理空房间（超过5分钟无活动）
    cleanupEmptyRooms() {
        const now = Date.now();
        let cleanedCount = 0;
        
        for (const [roomId, room] of this.rooms) {
            if (room.clients.size === 0 && (now - room.lastActivity) > 5 * 60 * 1000) {
                this.rooms.delete(roomId);
                cleanedCount++;
                console.log(`🧹 Cleaned up empty room: ${roomId}`);
            }
        }
        
        if (cleanedCount > 0) {
            console.log(`🧹 Cleaned up ${cleanedCount} empty room(s)`);
        }
        
        return cleanedCount;
    }
}

module.exports = RoomManager;