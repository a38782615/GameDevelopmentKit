using System;
using System.Diagnostics;
using System.IO;

namespace ET.Client
{
    [EnableClass]
    public static class SkillDiagFileLogger
    {
#if UNITY_EDITOR
        [StaticField]
        private static readonly object SyncRoot = new object();
        [StaticField]
        private static string currentLogFilePath;
        [StaticField]
        private static bool wasPlaying;
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            lock (SyncRoot)
            {
                EnsureLogFile();
                File.AppendAllText(currentLogFilePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
#endif
        }

#if UNITY_EDITOR
        private static void EnsureLogFile()
        {
            bool isPlaying = UnityEditor.EditorApplication.isPlaying;
            if (string.IsNullOrEmpty(currentLogFilePath) || (isPlaying && !wasPlaying))
            {
                string logDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Temp", "SkillDiagLogs"));
                Directory.CreateDirectory(logDirectory);

                string runType = isPlaying ? "play" : "editor";
                currentLogFilePath = Path.Combine(logDirectory, $"skill_diag_{runType}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
            }

            wasPlaying = isPlaying;
        }
#endif
    }
}
