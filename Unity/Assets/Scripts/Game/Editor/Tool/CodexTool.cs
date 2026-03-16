using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CodexTool
    {
        private const string MenuPath = "Game/Tool/Open Codex Admin PowerShell";
        private const string TerminalTitle = "Codex Admin";
        private static readonly string[] s_CodexFileNames = { "codex.ps1", "codex.cmd", "codex.exe", "codex.bat" };

        [MenuItem(MenuPath)]
        public static void OpenCodexAdminPowerShell()
        {
#if UNITY_EDITOR_WIN
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string codexCommandPath = FindCodexCommandPath();
            string powerShellArguments = BuildPowerShellArguments(codexCommandPath);
            string terminalArguments =
                $"new-tab --title \"{TerminalTitle}\" -d \"{projectRoot}\" -- powershell.exe {powerShellArguments}";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = terminalArguments,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = projectRoot,
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"Failed to open Codex admin PowerShell. {exception}");
            }
#else
            UnityEngine.Debug.LogError("Open Codex Admin PowerShell only supports Windows Editor.");
#endif
        }

        private static string BuildPowerShellArguments(string codexCommandPath)
        {
            if (!string.IsNullOrEmpty(codexCommandPath) &&
                string.Equals(Path.GetExtension(codexCommandPath), ".ps1", StringComparison.OrdinalIgnoreCase))
            {
                return $"-NoExit -ExecutionPolicy Bypass -File \"{codexCommandPath}\" --sandbox danger-full-access --ask-for-approval never";
            }

            string command = !string.IsNullOrEmpty(codexCommandPath)
                ? $"& '{EscapePowerShellSingleQuotedString(codexCommandPath)}' --sandbox danger-full-access --ask-for-approval never"
                : "codex --sandbox danger-full-access --ask-for-approval never";
            return $"-NoExit -ExecutionPolicy Bypass -Command \"{command}\"";
        }

        private static string FindCodexCommandPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string npmDirectory = Path.Combine(appData, "npm");
            if (TryFindCodexCommandInDirectory(npmDirectory, out string npmCodexPath))
            {
                return npmCodexPath;
            }

            string pathEnvironment = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            IEnumerable<string> directories = pathEnvironment
                .Split(Path.PathSeparator)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim());
            foreach (string directory in directories)
            {
                if (TryFindCodexCommandInDirectory(directory, out string codexPath))
                {
                    return codexPath;
                }
            }

            return null;
        }

        private static bool TryFindCodexCommandInDirectory(string directory, out string codexPath)
        {
            codexPath = null;
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            foreach (string fileName in s_CodexFileNames)
            {
                string candidatePath = Path.Combine(directory, fileName);
                if (File.Exists(candidatePath))
                {
                    codexPath = candidatePath;
                    return true;
                }
            }

            return false;
        }

        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
