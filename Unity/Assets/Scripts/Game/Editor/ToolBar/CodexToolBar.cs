using ToolbarExtension;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class CodexToolBar
    {
        private static readonly GUIContent s_ButtonGUIContent = new GUIContent("CodexAdmin", "Open Windows Terminal admin PowerShell and run codex in the Unity project root.");

        [Toolbar(OnGUISide.Right, 97)]
        private static void OnToolbarGUI()
        {
            if (GUILayout.Button(s_ButtonGUIContent))
            {
                CodexTool.OpenCodexAdminPowerShell();
            }
        }
    }
}
