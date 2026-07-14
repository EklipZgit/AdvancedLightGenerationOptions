using System;
using System.IO;
using System.Reflection;

namespace AdvancedLightGenerationOptions
{
    public static class PluginLogger
    {
        public static bool DebugEnabled = true;

        private static readonly string LogPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "AdvancedLightGenerationOptions.log");

        static PluginLogger()
        {
            try
            {
                File.WriteAllText(LogPath, $"AdvancedLightGenerationOptions log started at {DateTime.Now:O}\n");
            }
            catch
            {
                // ignored
            }
        }

        public static void Log(string message)
        {
            if (!DebugEnabled) return;

            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {message}\n");
            }
            catch
            {
                // ignored
            }
        }
    }
}
