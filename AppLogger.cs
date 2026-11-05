using System;
using System.IO;

namespace LocalhostManager
{
    public static class AppLogger
    {
        public static void Log(string msg)
        {
            try
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                File.AppendAllText(Path.Combine(dir, "app_debug.log"), $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }
    }
}
