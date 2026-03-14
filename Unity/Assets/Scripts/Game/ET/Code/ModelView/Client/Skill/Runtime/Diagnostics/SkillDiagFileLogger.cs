using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    [EnableClass]
    public static class SkillDiagFileLogger
    {
#if UNITY_EDITOR
        [StaticField]
        private static readonly object syncRoot = new object();

        [StaticField]
        private static bool initialized;

        [StaticField]
        private static string logFilePath;

        public static string LogFilePath
        {
            get
            {
                EnsureInitialized();
                return logFilePath;
            }
        }

        public static void Log(string message)
        {
            EnsureInitialized();

            string sceneName = SceneManager.GetActiveScene().name;
            string line = $"[{DateTime.Now:HH:mm:ss.fff}][frame:{Time.frameCount}][scene:{sceneName}] {message}";

            lock (syncRoot)
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }

            Debug.LogWarning(line);
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            lock (syncRoot)
            {
                if (initialized)
                {
                    return;
                }

                string rootPath = GetLogDirectory();
                Directory.CreateDirectory(rootPath);

                logFilePath = Path.Combine(rootPath, $"skill_diag_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
                File.WriteAllText(logFilePath, $"# Skill diag session {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}");

                initialized = true;
                Debug.LogWarning($"[SkillDiagFileLogger] logPath={logFilePath}");
            }
        }

        private static string GetLogDirectory()
        {
            string projectRoot = null;
            try
            {
                projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            }
            catch (Exception)
            {
                projectRoot = null;
            }

            if (string.IsNullOrEmpty(projectRoot))
            {
                return Path.Combine(Application.persistentDataPath, "SkillDiagLogs");
            }

            return Path.Combine(projectRoot, "Temp", "SkillDiagLogs");
        }
#else
        public static string LogFilePath => string.Empty;

        public static void Log(string message)
        {
        }
#endif
    }
}
