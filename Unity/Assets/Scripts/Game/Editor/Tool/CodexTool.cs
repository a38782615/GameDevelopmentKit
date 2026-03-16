using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CodexTool
    {
        private const string MenuPath = "Game/Tool/Open Codex Admin PowerShell";
        private const string CodexArguments = "codex --sandbox danger-full-access --ask-for-approval never";
        private const string TerminalTitle = "Codex Admin";

        [MenuItem(MenuPath)]
        public static void OpenCodexAdminPowerShell()
        {
#if UNITY_EDITOR_WIN
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string escapedProjectRoot = EscapePowerShellSingleQuotedString(projectRoot);
            string command = $"Set-Location -LiteralPath '{escapedProjectRoot}'; {CodexArguments}";
            string terminalArguments =
                $"new-tab --title \"{TerminalTitle}\" -d \"{projectRoot}\" powershell.exe -NoExit -ExecutionPolicy Bypass -Command \"{command}\"";

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

        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
