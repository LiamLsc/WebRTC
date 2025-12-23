using System;
using UnityEngine;

public class ProgressCalculator
{
    public long TotalBytes { get; private set; }
    public long CurrentBytes { get; private set; }

    public float Progress => TotalBytes == 0 ? 0 : (float)CurrentBytes / TotalBytes;
    public float SpeedBytesPerSecond { get; private set; }

    public Action<float, float> OnProgressChanged;
    // 参数：progress(0-1), speed(B/s)

    private float lastTime;
    private long lastBytes;

    public void Reset(long totalBytes)
    {
        TotalBytes = totalBytes;
        CurrentBytes = 0;
        SpeedBytesPerSecond = 0;

        lastTime = Time.time;
        lastBytes = 0;
    }

    public void AddBytes(int bytes)
    {
        CurrentBytes += bytes;

        float now = Time.time;
        float deltaTime = now - lastTime;

        if (deltaTime >= 0.5f)
        {
            long deltaBytes = CurrentBytes - lastBytes;
            SpeedBytesPerSecond = deltaBytes / deltaTime;

            lastBytes = CurrentBytes;
            lastTime = now;

            OnProgressChanged?.Invoke(Progress, SpeedBytesPerSecond);
        }
    }
}
