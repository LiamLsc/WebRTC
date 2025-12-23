const WebSocket = require("ws");
const http = require('http');
const RoomManager = require("./roomManager");

// 使用环境变量端口，Zeabur会自动分配
const PORT = parseInt(process.env.PORT) || 3000;
console.log(`🎯 Attempting to bind to PORT: ${PORT}`);

const roomManager = new RoomManager();

const server = http.createServer((req, res) => {
  // 使用WHATWG URL API替代已弃用的url.parse()
  const parsedUrl = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
  
  if (parsedUrl.pathname === '/health') {
    res.writeHead(200);
    res.end('OK');
    return;
  }
  
  if (parsedUrl.pathname === '/rooms') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      activeRooms: roomManager.getRoomCount ? roomManager.getRoomCount() : 0
    }));
    return;
  }
  
  res.writeHead(404);
  res.end('Not Found');
});

// 创建WebSocket服务器，与HTTP服务器共享同一个端口
const wss = new WebSocket.Server({ 
  server, // 使用现有的HTTP服务器实例
  clientTracking: true,
  perMessageDeflate: {
    zlibDeflateOptions: {
      chunkSize: 1024,
      memLevel: 7,
      level: 3
    },
    zlibInflateOptions: {
      chunkSize: 10 * 1024
    },
    clientNoContextTakeover: true,
    serverNoContextTakeover: true,
    serverMaxWindowBits: 10,
    concurrencyLimit: 10,
    threshold: 1024
  }
});

server.listen(PORT, '0.0.0.0', () => {
  console.log(`🚀 WebRTC Signaling Server running on port ${PORT}`);
  console.log(`🔗 WebSocket URL: ws://0.0.0.0:${PORT}`);
  console.log(`🏥 Health check: http://0.0.0.0:${PORT}/health`);
});

wss.on("connection", (ws, request) => {
  // 添加CORS和连接信息
  const origin = request.headers.origin;
  ws.roomId = null;
  ws.userId = `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  
  console.log(`🔗 New connection: ${ws.userId} from ${origin || 'unknown origin'}`);
  
  // 发送欢迎消息
  ws.send(JSON.stringify({
    type: "welcome",
    userId: ws.userId,
    timestamp: Date.now()
  }));

  ws.on("message", (data) => {
    let msg;
    try {
      msg = JSON.parse(data.toString());
    } catch (error) {
      console.error("❌ JSON parse error:", error);
      ws.send(JSON.stringify({
        type: "error",
        message: "Invalid JSON format"
      }));
      return;
    }

    console.log(`📨 Received ${msg.type} from ${ws.userId}`);

    switch (msg.type) {
      case "create-room": {
        const roomId = roomManager.createRoom();
        ws.roomId = roomId;
        roomManager.joinRoom(roomId, ws);

        ws.send(JSON.stringify({
          type: "room-created",
          roomId,
          userId: ws.userId
        }));
        
        console.log(`✅ Room created: ${roomId} by ${ws.userId}`);
        break;
      }

      case "join-room": {
        if (!msg.roomId) {
          ws.send(JSON.stringify({
            type: "error",
            message: "Room ID is required"
          }));
          return;
        }
        
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
          roomId: msg.roomId,
          userId: ws.userId
        }));

        // 通知房间内的其他用户
        roomManager.broadcast(msg.roomId, ws, {
          type: "peer-joined",
          userId: ws.userId,
          timestamp: Date.now()
        });
        
        console.log(`✅ User ${ws.userId} joined room ${msg.roomId}`);
        break;
      }

      case "offer":
      case "answer":
      case "ice-candidate": {
        if (!ws.roomId) {
          ws.send(JSON.stringify({
            type: "error",
            message: "Not in a room"
          }));
          return;
        }

        // 添加发送者信息
        msg.sender = ws.userId;
        roomManager.broadcast(ws.roomId, ws, msg);
        console.log(`📤 ${msg.type} from ${ws.userId} in room ${ws.roomId}`);
        break;
      }
      
      case "ping": {
        ws.send(JSON.stringify({
          type: "pong",
          timestamp: Date.now()
        }));
        break;
      }
      
      case "leave-room": {
        if (ws.roomId) {
          roomManager.leaveRoom(ws.roomId, ws);
          
          roomManager.broadcast(ws.roomId, ws, {
            type: "peer-left",
            userId: ws.userId,
            timestamp: Date.now()
          });
          
          console.log(`👋 User ${ws.userId} left room ${ws.roomId}`);
          ws.roomId = null;
        }
        break;
      }
    }
  });

  ws.on("close", () => {
    if (ws.roomId) {
      roomManager.leaveRoom(ws.roomId, ws);
      
      // 通知其他用户
      roomManager.broadcast(ws.roomId, ws, {
        type: "peer-left",
        userId: ws.userId,
        timestamp: Date.now()
      });
      
      console.log(`🔌 Connection closed: ${ws.userId} from room ${ws.roomId}`);
    } else {
      console.log(`🔌 Connection closed: ${ws.userId}`);
    }
  });

  ws.on("error", (error) => {
    console.error(`❌ WebSocket error for ${ws.userId}:`, error);
  });
});

// 定期清理空房间
setInterval(() => {
  roomManager.cleanupEmptyRooms && roomManager.cleanupEmptyRooms();
}, 60000); // 每分钟清理一次

process.on('SIGTERM', () => {
  console.log('🛑 Received SIGTERM, shutting down gracefully...');
  wss.close(() => {
    server.close(() => {
      console.log('👋 Server shutdown complete');
      process.exit(0);
    });
  });
});