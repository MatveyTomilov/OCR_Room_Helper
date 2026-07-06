using System;
using System.IO;

namespace AbyssOverlay;

public static class Logger
{
    private static string _path = string.Empty;

    public static void Init(string path)
    {
        _path = path;
        try
        {
            File.AppendAllText(_path, $"\n--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n");
        }
        catch
        {
        }
    }

    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(_path)) return;
        try
        {
            File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch
        {
        }
    }

    public static void Log(Exception ex, string? context = null)
    {
        if (string.IsNullOrWhiteSpace(_path)) return;
        try
        {
            File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] ERROR {context}: {ex}\n");
        }
        catch
        {
        }
    }
}
