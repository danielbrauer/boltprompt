using System.Text;
using NiceIO;

namespace Shelper;

public static class Logger
{
    public const string Gpt = "GPT";

    private static readonly Dictionary<NPath, FileStream> LogFiles = new();

    static FileStream GetFileStream(NPath path)
    {
        if (LogFiles.TryGetValue(path, out var stream))
            return stream;
        stream = File.Open(path.ToString(), FileMode.Create);
        LogFiles[path] = stream;
        return stream;
    }
    
    public static void Log(string file, string message)
    {
        var logDir = NPath.HomeDirectory.Combine("Library/Logs/Shelper").MakeAbsolute();
        logDir.CreateDirectory();
        var path = logDir.Combine(file);
        var stream = GetFileStream(path);
        stream.Write(Encoding.UTF8.GetBytes(message));
        stream.Flush();
    }
}