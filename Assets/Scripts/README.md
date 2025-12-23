Scripts/
 ├── Network/
 │   ├── SignalingClient.cs        // WebSocket 信令客户端
 │   ├── WebRTCManager.cs          // WebRTC 连接与 DataChannel 管理
 │   └── ICEConfig.cs              // ICE / STUN / TURN 配置
 │
 ├── FileTransfer/
 │   ├── FileSender.cs             // 大文件分片发送
 │   ├── FileReceiver.cs           // 分片接收与重组
 │   └── FileMeta.cs               // 文件元信息
 │
 ├── UI/
 │   ├── SenderUIController.cs     // 发送方界面逻辑
 │   ├── ReceiverUIController.cs   // 接收方界面逻辑
 │   └── ProgressCalculator.cs     // 速度 & 进度计算
 │
 └── Utils/
     ├── ShareLinkUtility.cs       // 分享链接生成与校验
     └── PathValidator.cs          // 本地文件路径校验
