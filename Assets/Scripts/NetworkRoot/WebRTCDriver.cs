using UnityEngine;

public class WebRTCDriver : MonoBehaviour
{
    public WebRTCManager Manager { get; private set; }

    void Awake()
    {
        Manager = new WebRTCManager();
    }
}
