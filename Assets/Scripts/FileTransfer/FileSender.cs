using System.Collections;
using System.IO;
using UnityEngine;
using Unity.WebRTC;
using System.Net.Sockets;

public class FileSender
{
    const int CHUNK_SIZE = 64 * 1024;
    const ulong MAX_BUFFER = 8 * 1024 * 1024;

    public IEnumerator SendFile(string filePath, RTCDataChannel channel)
    {
        Debug.Log($"FileSender：正在发送文件 {filePath}");
        var info = new FileInfo(filePath);

        // 1️⃣ 发送 Header
        var meta = new FileMeta
        {
            fileName = info.Name,
            fileSize = info.Length,
            chunkSize = CHUNK_SIZE
        };

        Debug.Log($"FileSender：正在发送文件头信息 - 文件名：{meta.fileName}，文件大小：{meta.fileSize} 字节");
        SendPacket(channel, PacketType.FileHeader, JsonUtility.ToJson(meta));

        // 2️⃣ 分片流式读取
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            int index = 0;
            byte[] buffer = new byte[CHUNK_SIZE];
            long totalSent = 0;

            while (true)
            {
                // 检查数据通道是否仍然开放
                if (channel.ReadyState != RTCDataChannelState.Open)
                {
                    Debug.LogError($"FileSender：数据通道未打开，当前状态：{channel.ReadyState}");
                    yield break;
                }

                int read = fs.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;

                // 🚦流控
                while (channel.BufferedAmount > MAX_BUFFER)
                {
                    Debug.Log($"FileSender：缓冲区已满，等待... 已发送 {channel.BufferedAmount} 字节");
                    yield return null;
                }

                SendChunk(channel, index++, buffer, read);
                totalSent += read;
                Debug.Log($"FileSender：已发送分片 {index-1}，大小：{read} 字节，已发送总大小：{totalSent}/{info.Length} 字节");
                yield return null;
            }
            
            Debug.Log($"FileSender：完成读取文件，已发送总大小：{totalSent} 字节");
        }

        // 3️⃣ EOF
        Debug.Log($"FileSender：正在发送文件结束包");
        SendPacket(channel, PacketType.FileEnd, null);
        Debug.Log($"FileSender：文件传输完成");
    }

    void SendPacket(RTCDataChannel channel, PacketType type, string json)
    {
        var payload = json == null ? new byte[0] : System.Text.Encoding.UTF8.GetBytes(json);
        var data = new byte[1 + payload.Length];
        data[0] = (byte)type;
        payload.CopyTo(data, 1);
        
        Debug.Log($"FileSender：正在发送数据包，类型：{type}，大小：{data.Length} 字节");
        try
        {
            channel.Send(data);
            Debug.Log($"FileSender：数据包发送成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FileSender：发送数据包失败：{e.Message}");
        }
    }

    void SendChunk(RTCDataChannel channel, int index, byte[] buffer, int size)
    {
        byte[] data = new byte[1 + 4 + 4 + size];
        data[0] = (byte)PacketType.FileChunk;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, data, 1, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(size), 0, data, 5, 4);
        System.Buffer.BlockCopy(buffer, 0, data, 9, size);

        try
        {
            channel.Send(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FileSender：发送分片 {index} 失败：{e.Message}");
        }
    }
}