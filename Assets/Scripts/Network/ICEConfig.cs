using Unity.WebRTC;

public static class ICEConfig
{
    public static RTCConfiguration GetConfig()
    {
        return new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer
                {
                    urls = new[]
                    {
                        "stun:stun.l.google.com:19302"
                    }
                }
                // 如果你未来上 TURN，可在此添加
            }
        };
    }
}
