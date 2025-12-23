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
                OnIceCandidate?.Invoke(JsonUtility.ToJson(candidate));
            }
        };

        if (isSender)
        {
            var init = new RTCDataChannelInit { ordered = true };
            DataChannel = Peer.CreateDataChannel("file", init);
            RegisterDataChannel(DataChannel);
        }
        else
        {
            Peer.OnDataChannel = channel =>
            {
                DataChannel = channel;
                RegisterDataChannel(channel);
            };
        }
    }

    private void RegisterDataChannel(RTCDataChannel channel)
    {
        channel.OnOpen = () => OnDataChannelReady?.Invoke();
        channel.OnMessage = bytes => OnDataReceived?.Invoke(bytes);
    }
}
