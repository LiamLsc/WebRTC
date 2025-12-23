using System.IO;

public static class PathValidator
{
    public static bool IsValidFile(string path, out FileInfo info)
    {
        info = null;
        if (!File.Exists(path)) return false;

        info = new FileInfo(path);
        return true;
    }
}
