using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class SignalingClient
{
    private NativeWebSocket.WebSocket websocket;

    public Action<string> OnRoomCreated;
    public Action OnRoomJoined; // 新增事件
    public Action OnPeerJoined;
    public Action<string> OnOffer;
    public Action<string> OnAnswer;
    public Action<string> OnIceCandidate;

    public async void Connect(string url)
    {
        Debug.Log($"SignalingClient：正在连接到 {url}");
        websocket = new NativeWebSocket.WebSocket(url);

        websocket.OnMessage += OnMessageReceived;
        websocket.OnOpen += () =>
        {
            Debug.Log("SignalingClient：WebSocket 连接已打开");
        };
        websocket.OnError += (error) =>
        {
            Debug.LogError($"SignalingClient：WebSocket 错误 - {error}");
        };
        websocket.OnClose += (closeCode) =>
        {
            Debug.Log($"SignalingClient：使用代码关闭 WebSocket 连接 {closeCode}");
        };

        await websocket.Connect();
    }

    public void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    public async void CreateRoom()
    {
        Debug.Log("SignalingClient：正在创建房间");
        await Send(new { type = "create-room" });
    }

    public async void JoinRoom(string roomId)
    {
        Debug.Log($"SignalingClient：正在加入房间 {roomId}");
        await Send(new { type = "join-room", roomId });
    }

    public async void SendOffer(string sdp)
    {
        Debug.Log("SignalingClient：正在发送 Offer");
        await Send(new { type = "offer", sdp });
    }

    public async void SendAnswer(string sdp)
    {
        Debug.Log("SignalingClient：正在发送 Answer");
        await Send(new { type = "answer", sdp });
    }

    public async void SendIceCandidate(string candidate)
    {
        Debug.Log("SignalingClient：正在发送 ICE 候选者");
        await Send(new { type = "ice-candidate", candidate });
    }

    private async System.Threading.Tasks.Task Send(object obj)
    {
        if (websocket == null) 
        {
            Debug.LogError("SignalingClient：WebSocket 为 null");
            return;
        }

        string json = JsonConvert.SerializeObject(obj);
        Debug.Log($"SignalingClient：正在发送消息 - {json}");
        await websocket.SendText(json);
    }

    private void OnMessageReceived(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
        Debug.Log($"SignalingClient：收到消息 - {json}");
        
        SignalMessage msg;
        try 
        {
            msg = JsonConvert.DeserializeObject<SignalMessage>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SignalingClient：失败 - 无法反序列化消息：{e.Message}");
            return;
        }

        if (string.IsNullOrEmpty(msg.type))
        {
            Debug.LogWarning("SignalingClient：收到消息 - 没有类型字段");
            return;
        }

        switch (msg.type)
        {
            case "room-created":
                Debug.Log($"SignalingClient：房间已创建，ID 为 {msg.roomId}");
                OnRoomCreated?.Invoke(msg.roomId);
                break;
            case "room-joined":
                Debug.Log($"SignalingClient：加入房间 {msg.roomId}");
                OnRoomJoined?.Invoke(); // 新增事件触发
                break;
            case "peer-joined":
                Debug.Log($"SignalingClient：其他对等体加入房间 {msg.roomId}");
                OnPeerJoined?.Invoke();
                break;
            case "offer":
                Debug.Log($"SignalingClient：收到 Offer - {msg.sdp}");
                OnOffer?.Invoke(msg.sdp);
                break;
            case "answer":
                Debug.Log($"SignalingClient：收到 Answer - {msg.sdp}");
                OnAnswer?.Invoke(msg.sdp);
                break;
            case "ice-candidate":
                Debug.Log($"SignalingClient：收到 ICE 候选者 - {msg.candidate}");
                OnIceCandidate?.Invoke(msg.candidate);
                break;
            default:
                Debug.LogWarning($"SignalingClient：未知消息类型 '{msg.type}'");
                break;
        }
    }

    [Serializable]
    private class SignalMessage
    {
        public string type;
        public string roomId;
        public string sdp;
        public string candidate;
    }
}