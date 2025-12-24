using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;

public class ReceiverUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField shareLinkInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text speedText;

    [Header("Network")]
    [SerializeField] private SignalingDriver signalingDriver;
    [SerializeField] private WebRTCDriver webRTCDriver;

    private FileReceiver receiver;
    private ProgressCalculator progress;

    private string roomId;

    void Awake()
    {
        receiver = new FileReceiver();
        progress = new ProgressCalculator();

        confirmButton.interactable = false;

        progress.OnProgressChanged += UpdateProgressUI;
    }

    void Start()
    {
        shareLinkInput.onValueChanged.AddListener(OnShareLinkChanged);
        confirmButton.onClick.AddListener(OnConfirmClicked);

        var signaling = signalingDriver.Client;
        var rtc = webRTCDriver.Manager;

        signaling.OnRoomJoined += () => 
        {
            Debug.Log($"Receiver：成功加入房间 {roomId}，初始化 WebRTC 连接（作为接收方）");
            // 加入房间成功后，初始化WebRTC连接（作为接收方）
            rtc.CreatePeer(false); // false表示接收方
            
            // 添加ICE连接状态变化监听（在Peer创建后）
            webRTCDriver.Manager.Peer.OnIceConnectionChange += (state) => {
                Debug.Log($"Receiver：ICE连接状态改变为: {state}");
            };
        };
        
        signaling.OnOffer += OnOfferReceived;
        signaling.OnIceCandidate += (string candidateJson) =>
        {
            Debug.Log($"Receiver：收到ICE候选: {candidateJson}");
            var init = JsonUtility.FromJson<RTCIceCandidateInit>(candidateJson);
            var candidate = new RTCIceCandidate(init);
            rtc.Peer.AddIceCandidate(candidate);
        };

        // 添加WebRTC连接状态日志
        rtc.OnDataChannelReady += () => {
            Debug.Log("Receiver：数据通道已就绪，可以开始接收文件");
        };
        
        rtc.OnDataReceived += OnDataReceived;
    }

    private void OnShareLinkChanged(string link)
    {
        confirmButton.interactable =
            ShareLinkUtility.Parse(link, out roomId, out _, out _);
    }

    private void OnConfirmClicked()
    {
        signalingDriver.Client.JoinRoom(roomId);
    }

    private void OnOfferReceived(string sdp)
    {
        Debug.Log($"Receiver：已接收 Offer，正在创建 Answer");
        StartCoroutine(WebRTCAnswerRoutine(sdp));
    }

    private IEnumerator WebRTCAnswerRoutine(string sdp)
    {
        var rtc = webRTCDriver.Manager;

        var offer = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = sdp
        };

        var setRemoteOp = rtc.Peer.SetRemoteDescription(ref offer);
        yield return setRemoteOp;

        if (setRemoteOp.IsError)
        {
            Debug.LogError($"Receiver：设置远程描述失败：{setRemoteOp.Error.message}");
            yield break;
        }

        var answerOp = rtc.Peer.CreateAnswer();
        yield return answerOp;

        if (answerOp.IsError)
        {
            Debug.LogError($"Receiver：创建 Answer 失败：{answerOp.Error.message}");
            yield break;
        }

        var desc = answerOp.Desc;
        var setLocalOp = rtc.Peer.SetLocalDescription(ref desc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            Debug.LogError($"Receiver：设置本地描述失败：{setLocalOp.Error.message}");
            yield break;
        }

        signalingDriver.Client.SendAnswer(desc.sdp);
        Debug.Log($"Receiver：Answer 已发送");
    }

    private void OnDataReceived(byte[] data)
    {
        Debug.Log($"Receiver：已接收数据，大小：{data.Length} 字节");
        receiver.OnData(data, signalingDriver.saveDirectory);

        if (receiver.TotalBytes > 0)
        {
            if (progress.TotalBytes == 0)
            {
                Debug.Log($"Receiver：初始化进度跟踪器，总字节数：{receiver.TotalBytes}");
                progress.Reset(receiver.TotalBytes);
            }

            progress.AddBytes(data.Length);
        }
        else
        {
            Debug.Log($"Receiver：总字节数未设置，跳过进度更新");
        }
    }

    private void UpdateProgressUI(float progressValue, float speed)
    {
        progressSlider.value = progressValue;
        progressText.text = $"{progressValue * 100f:F1}%";
        speedText.text = $"{FormatBytes(speed)}/s";
        Debug.Log($"Receiver：进度更新 - {progressValue * 100f:F1}%, 速度：{FormatBytes(speed)}/s");
    }

    private string FormatBytes(float bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / 1024f / 1024f:F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024f:F2} KB";
        return $"{bytes:F0} B";
    }
}