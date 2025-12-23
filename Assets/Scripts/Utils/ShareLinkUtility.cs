using System;

public static class ShareLinkUtility
{
    public static string Generate(string roomId, string fileName, long size)
    {
        return $"webrtc://{roomId}|{fileName}|{size}";
    }

    public static bool Parse(string link, out string roomId, out string fileName, out long size)
    {
        roomId = fileName = "";
        size = 0;

        if (!link.StartsWith("webrtc://")) return false;

        var parts = link.Replace("webrtc://", "").Split('|');
        if (parts.Length != 3) return false;

        roomId = parts[0];
        fileName = parts[1];
        return long.TryParse(parts[2], out size);
    }
}
