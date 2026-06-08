using ToolbarExtension;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class CodexToolBar
    {
        [ToolbarButton(OnGUISide.Right, 97, "CodexAdmin", "Open Windows Terminal admin PowerShell and run codex in the Unity project root.")]
        private static void OpenCodexAdminPowerShell()
        {
            CodexTool.OpenCodexAdminPowerShell();
        }
    }
}
