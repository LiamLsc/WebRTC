using System.IO;
using UnityEngine;

public class SignalingDriver : MonoBehaviour
{
    public SignalingClient Client { get; private set; }

    public string signalingUrl;
    public string saveDirectory;

    void Awake()
    {
        if (string.IsNullOrEmpty(signalingUrl))
            signalingUrl = "ws://localhost:3000";

        Client = new SignalingClient();
        Client.Connect(signalingUrl);
    }

    void Start()
    {
        if (string.IsNullOrEmpty(saveDirectory))
            saveDirectory = Application.persistentDataPath;

        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);
    }


    void Update()
    {
        Client.Update();
    }
}