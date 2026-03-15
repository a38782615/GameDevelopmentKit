#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace ET.Client.Editor
{
    [InitializeOnLoad]
    public static class SkillPortIdRuntimeVerifier
    {
        private const string EditTriggerFilePath = "Temp/skill_editor_menu.txt";
        private const string TriggerFilePath = "Temp/skill_port_id_verify.txt";

        private static bool armed;
        private static bool executed;
        private static double playStartTime;
        private static string menuItemPath;

        static SkillPortIdRuntimeVerifier()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    ArmFromTriggerFile();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    ResetState();
                    break;
            }
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying && File.Exists(EditTriggerFilePath))
            {
                string editMenuItemPath = File.ReadAllText(EditTriggerFilePath).Trim();
                File.Delete(EditTriggerFilePath);
                if (!string.IsNullOrEmpty(editMenuItemPath))
                {
                    ExecuteEditCommand(editMenuItemPath);
                }
            }

            if (!EditorApplication.isPlaying || !armed)
            {
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - playStartTime;
            if (!executed && elapsed >= 2.0d)
            {
                executed = true;
                if (!string.IsNullOrEmpty(menuItemPath))
                {
                    EditorApplication.ExecuteMenuItem(menuItemPath);
                }
            }

            if (elapsed >= 8.0d)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void ArmFromTriggerFile()
        {
            ResetState();
            if (!File.Exists(TriggerFilePath))
            {
                return;
            }

            menuItemPath = File.ReadAllText(TriggerFilePath).Trim();
            File.Delete(TriggerFilePath);
            if (string.IsNullOrEmpty(menuItemPath))
            {
                return;
            }

            armed = true;
            playStartTime = EditorApplication.timeSinceStartup;
        }

        private static void ResetState()
        {
            armed = false;
            executed = false;
            playStartTime = 0d;
            menuItemPath = null;
        }

        private static void ExecuteEditCommand(string command)
        {
            switch (command)
            {
                case "skill-export-all":
                    ExportAllSkillGraphsToExcel();
                    return;
                default:
                    EditorApplication.ExecuteMenuItem(command);
                    return;
            }
        }

        private static void ExportAllSkillGraphsToExcel()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SkillAssetTreeView.RootPath });
            var skills = new List<SkillGraphData>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SkillGraphData asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(path);
                if (asset != null)
                {
                    skills.Add(asset);
                }
            }

            if (skills.Count > 0)
            {
                SkillAssetToLubanExporter.ExportToExcel(skills);
            }
        }

        [MenuItem("SkillEditor/Automation/Export All Skill Graphs To Excel")]
        private static void ExportAllSkillGraphsToExcelMenu()
        {
            ExportAllSkillGraphsToExcel();
        }
    }
}
#endif
