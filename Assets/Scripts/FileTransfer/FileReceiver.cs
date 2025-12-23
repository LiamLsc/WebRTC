using System.IO;
using UnityEngine;

public class FileReceiver
{
    private FileStream stream;
    private FileMeta meta;
    private long receivedBytes;

    public void OnData(byte[] data, string saveDir)
    {
        var type = (PacketType)data[0];
        Debug.Log($"FileReceiver：收到类型为 {type} 的数据包，大小：{data.Length} 字节");

        switch (type)
        {
            case PacketType.FileHeader:
                HandleHeader(data, saveDir);
                break;

            case PacketType.FileChunk:
                HandleChunk(data);
                break;

            case PacketType.FileEnd:
                Finish();
                break;
        }
    }

    void HandleHeader(byte[] data, string dir)
    {
        string json = System.Text.Encoding.UTF8.GetString(data, 1, data.Length - 1);
        Debug.Log($"FileReceiver：正在处理头信息 - {json}");
        meta = JsonUtility.FromJson<FileMeta>(json);

        string path = Path.Combine(dir, meta.fileName);
        Debug.Log($"FileReceiver：正在创建文件 {path}，预期大小 {meta.fileSize} 字节");
        stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        receivedBytes = 0;
    }

    void HandleChunk(byte[] data)
    {
        int size = System.BitConverter.ToInt32(data, 5);
        Debug.Log($"FileReceiver：正在写入数据块，大小 {size} 字节，已接收 {receivedBytes + size} 字节");
        stream.Write(data, 9, size);
        receivedBytes += size;
    }

    void Finish()
    {
        Debug.Log($"FileReceiver：完成文件接收，已接收 {receivedBytes} 字节");
        stream?.Close();
        Debug.Log($"FileReceiver：文件已保存至 {stream.Name}");
        Application.OpenURL(stream.Name);
    }

    public long ReceivedBytes => receivedBytes;
    public long TotalBytes => meta?.fileSize ?? 0;
}