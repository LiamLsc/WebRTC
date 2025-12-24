using System;
using UnityEngine;
using Unity.WebRTC;

public class WebRTCManager
{
    public RTCPeerConnection Peer;
    public RTCDataChannel DataChannel;

    public Action OnDataChannelReady;
    public Action<byte[]> OnDataReceived;
    public Action<string> OnIceCandidate;

    public void CreatePeer(bool isSender)
    {
        var config = ICEConfig.GetConfig();
        Peer = new RTCPeerConnection(ref config);

        Peer.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                Debug.Log($"WebRTCManager: 发现新的ICE候选: {candidate.Candidate}");
                OnIceCandidate?.Invoke(JsonUtility.ToJson(candidate));
            }
        };

        // 添加ICE连接状态变化监听
        Peer.OnIceConnectionChange = state =>
        {
            Debug.Log($"WebRTCManager: ICE连接状态改变为 {state}");
        };

        // 添加连接状态变化监听
        Peer.OnConnectionStateChange = state =>
        {
            Debug.Log($"WebRTCManager: 连接状态改变为 {state}");
        };

        if (isSender)
        {
            var init = new RTCDataChannelInit { ordered = true };
            DataChannel = Peer.CreateDataChannel("file", init);
            Debug.Log("WebRTCManager: 发送方创建数据通道");
            RegisterDataChannel(DataChannel);
        }
        else
        {
            Peer.OnDataChannel = channel =>
            {
                DataChannel = channel;
                Debug.Log("WebRTCManager: 接收方收到数据通道");
                RegisterDataChannel(channel);
            };
        }
    }

    private void RegisterDataChannel(RTCDataChannel channel)
    {
        channel.OnOpen = () => {
            Debug.Log("WebRTCManager: 数据通道已打开，触发OnDataChannelReady事件");
            OnDataChannelReady?.Invoke();
        };
        channel.OnMessage = bytes => OnDataReceived?.Invoke(bytes);
        
        channel.OnClose = () => {
            Debug.Log("WebRTCManager: 数据通道已关闭");
        };
        
        channel.OnError = err => {
            Debug.LogError($"WebRTCManager: 数据通道错误: {err}");
        };
    }
}