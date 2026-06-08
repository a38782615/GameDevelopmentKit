using ToolbarExtension;
using UnityEngine;

namespace Game.Editor
{
    sealed class ExcelExporterToolBar
    {
        [ToolbarButton(OnGUISide.Right, 99, "ExportExcel", "Export All Excel!")]
        private static void ExportExcel()
        {
            EditorTool.ExcelExporter();
        }
    }
}
