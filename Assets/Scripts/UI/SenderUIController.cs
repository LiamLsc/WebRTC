using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SenderUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField filePathInput;
    [SerializeField] private TextMeshProUGUI fileInfoText;
    [SerializeField] private Button generateLinkButton;
    [SerializeField] private TMP_InputField shareLinkInput;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Network")]
    [SerializeField] private SignalingDriver signalingDriver;
    [SerializeField] private WebRTCDriver webRTCDriver;

    private FileSender fileSender;
    private ProgressCalculator progress;

    private string currentRoomId;
    private string currentFilePath;

    void Awake()
    {
        fileSender = new FileSender();
        progress = new ProgressCalculator();

        generateLinkButton.interactable = false;
        shareLinkInput.text = string.Empty;

        progress.OnProgressChanged += UpdateProgressUI;
    }

    void Start()
    {
        filePathInput.onEndEdit.AddListener(OnFilePathChanged);
        generateLinkButton.onClick.AddListener(OnGenerateLinkClicked);

        var signaling = signalingDriver.Client;
        var rtc = webRTCDriver.Manager;

        signaling.OnRoomCreated += OnRoomCreated;
        signaling.OnPeerJoined += () =>
        {
            Debug.Log($"Sender：OnPeerJoined 事件已接收，开始创建和发送 Offer");
            StartCoroutine(CreateAndSendOffer());
        };

        // 添加数据通道就绪日志
        rtc.OnDataChannelReady += () =>
        {
            Debug.Log($"Sender：OnDataChannelReady 事件已接收，开始发送文件 {currentFilePath}");
            StartCoroutine(fileSender.SendFile(currentFilePath, rtc.DataChannel));
        };
        
        // 添加ICE连接状态变化日志
        webRTCDriver.Manager.Peer.OnIceConnectionChange += (state) => {
            Debug.Log($"Sender：ICE连接状态改变为: {state}");
        };
        
        // 添加连接状态变化日志
        webRTCDriver.Manager.Peer.OnConnectionStateChange += (state) => {
            Debug.Log($"Sender：连接状态改变为: {state}");
        };
    }

    private void OnFilePathChanged(string path)
    {
        Debug.Log($"Sender：文件路径已更改为 {path}");
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            fileInfoText.text =
                $"{info.Name}\n{FormatBytes(info.Length)}";
            generateLinkButton.interactable = true;
            currentFilePath = path;
            Debug.Log($"Sender：文件已验证 - {info.Name} ({info.Length} bytes)");
        }
        else
        {
            fileInfoText.text = "文件路径无效";
            generateLinkButton.interactable = false;
            Debug.Log($"Sender：文件路径无效 - {path}");
        }
    }

    private void OnGenerateLinkClicked()
    {
        Debug.Log($"Sender：生成链接按钮已点击");
        Debug.Log($"Sender：尝试使用 signaling URL 创建房间：{signalingDriver.signalingUrl}");
        signalingDriver.Client.CreateRoom();
        Debug.Log($"Sender：按钮已点击，房间 ID：{currentRoomId}");
    }

    private void OnRoomCreated(string roomId)
    {
        Debug.Log($"Sender：OnRoomCreated 事件已接收，房间 ID：{roomId}");
        currentRoomId = roomId;

        // 创建WebRTC对等连接
        webRTCDriver.Manager.CreatePeer(true); // true表示发送方
        
        // 添加ICE连接状态变化监听（在Peer创建后）
        webRTCDriver.Manager.Peer.OnIceConnectionChange += (state) =>
        {
            Debug.Log($"Sender：ICE连接状态改变为: {state}");
        };
        
        var info = new FileInfo(currentFilePath);
        shareLinkInput.text =
            ShareLinkUtility.Generate(roomId, info.Name, info.Length);
        Debug.Log($"Sender：分享链接已生成：{shareLinkInput.text}");
    }

    private IEnumerator CreateAndSendOffer()
    {
        Debug.Log($"Sender：正在创建和发送 Offer");
        var rtc = webRTCDriver.Manager;

        if (rtc.Peer == null)
        {
            Debug.LogError($"Sender：WebRTC 对等连接未初始化！");
            yield break;
        }

        var offerOp = rtc.Peer.CreateOffer();
        yield return offerOp;

        if (offerOp.IsError)
        {
            Debug.LogError($"Sender：创建 Offer 失败：{offerOp.Error.message}");
            yield break;
        }

        var desc = offerOp.Desc;
        var setLocalOp = rtc.Peer.SetLocalDescription(ref desc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            Debug.LogError($"Sender：设置本地描述失败：{setLocalOp.Error.message}");
            yield break;
        }

        signalingDriver.Client.SendOffer(desc.sdp);
        Debug.Log($"Sender：Offer 已发送");
    }

    private void UpdateProgressUI(float progressValue, float speed)
    {
        progressSlider.value = progressValue;
        progressText.text = $"{progressValue * 100f:F1}%";
        speedText.text = $"{FormatBytes(speed)}/s";
        Debug.Log($"Sender：进度已更新 - {progressValue * 100f:F1}%, 速度：{FormatBytes(speed)}/s");
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