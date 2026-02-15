using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using MiniExcelLibs;

using UnityEditor;
using UnityEngine;

namespace ET.Editor.Tools
{
    /// <summary>
    /// SkillAsset 导出到 Luban Excel 工具
    /// 只修改数据区域（从第5行开始），保持表头格式不变
    /// </summary>
    public class SkillAssetToLubanExporter : EditorWindow
    {
        private const string SKILL_ASSET_PATH = "Assets/Unity/Resources/ScriptObject/SkillAsset";
        private const string EXCEL_PATH = "Luban/MiniTemplate/Datas/#SkillGraph.xlsx";
        private const int DATA_START_ROW = 5; // 数据从第5行开始

        private Vector2 _scrollPosition;
        private List<SkillGraphData> _skillAssets = new List<SkillGraphData>();
        private List<bool> _selectedAssets = new List<bool>();
        private bool _selectAll = true;

        [MenuItem("SkillEditor/导出技能到 Luban Excel")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillAssetToLubanExporter>("技能导出工具");
            window.minSize = new Vector2(400, 500);
            window.LoadSkillAssets();
        }

        [MenuItem("SkillEditor/快速导出所有技能到 Excel")]
        public static void QuickExportAll()
        {
            var guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SKILL_ASSET_PATH });
            var skills = new List<SkillGraphData>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(path);
                if (asset != null)
                    skills.Add(asset);
            }

            if (skills.Count > 0)
            {
                ExportToExcel(skills);
                EditorUtility.DisplayDialog("导出成功", $"已导出 {skills.Count} 个技能到 Excel", "确定");
            }
            else
            {
                Debug.LogWarning("[SkillExporter] 未找到任何技能资源");
            }
        }

        private void LoadSkillAssets()
        {
            _skillAssets.Clear();
            _selectedAssets.Clear();

            var guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SKILL_ASSET_PATH });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(path);
                if (asset != null)
                {
                    _skillAssets.Add(asset);
                    _selectedAssets.Add(true);
                }
            }

            Debug.Log($"[SkillExporter] 找到 {_skillAssets.Count} 个技能资源");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("技能资源导出工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("将 SkillAsset 导出到 Luban Excel 表 (#SkillGraph.xlsx)\n从第5行开始写入数据，保持表头不变", MessageType.Info);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("刷新技能列表", GUILayout.Height(25)))
                LoadSkillAssets();

            EditorGUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            _selectAll = EditorGUILayout.Toggle("全选", _selectAll);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < _selectedAssets.Count; i++)
                    _selectedAssets[i] = _selectAll;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"技能列表 ({_skillAssets.Count} 个)", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            for (int i = 0; i < _skillAssets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _selectedAssets[i] = EditorGUILayout.Toggle(_selectedAssets[i], GUILayout.Width(20));
                EditorGUILayout.ObjectField(_skillAssets[i], typeof(SkillGraphData), false);
                EditorGUILayout.LabelField($"ID: {_skillAssets[i].SkillId}", GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("导出选中的技能到 Excel", GUILayout.Height(40)))
                ExportSelectedSkills();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            var outputPath = Path.Combine(Application.dataPath, "..", EXCEL_PATH);
            EditorGUILayout.LabelField("输出路径:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(outputPath, EditorStyles.textField, GUILayout.Height(20));

            if (GUILayout.Button("打开 Excel 文件"))
            {
                if (File.Exists(outputPath))
                    System.Diagnostics.Process.Start(outputPath);
            }
        }

        private void ExportSelectedSkills()
        {
            var selectedSkills = new List<SkillGraphData>();
            for (int i = 0; i < _skillAssets.Count; i++)
            {
                if (_selectedAssets[i])
                    selectedSkills.Add(_skillAssets[i]);
            }

            if (selectedSkills.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请至少选择一个技能", "确定");
                return;
            }

            ExportToExcel(selectedSkills);
            EditorUtility.DisplayDialog("导出成功", $"已导出 {selectedSkills.Count} 个技能到 Excel", "确定");
        }

        /// <summary>
        /// 导出技能到 Excel - 直接修改 xlsx 文件的 sheet1.xml
        /// </summary>
        public static void ExportToExcel(List<SkillGraphData> skills)
        {
            try
            {
                var excelPath = Path.Combine(Application.dataPath, "..", EXCEL_PATH);

                // 生成数据行
                var dataRows = new List<string[]>();

                foreach (var skill in skills)
                {
                    int nodeCount = skill.nodes?.Count ?? 0;
                    int connCount = skill.connections?.Count ?? 0;
                    int maxRows = Math.Max(Math.Max(nodeCount, connCount), 1);

                    for (int i = 0; i < maxRows; i++)
                    {
                        var row = new string[8];

                        // A列: 空
                        row[0] = "";

                        // 第一行写基础信息
                        if (i == 0)
                        {
                            row[1] = skill.SkillId;         // B: Id
                            row[2] = skill.name ?? "";           // D: Name
                            row[3] = "";                         // E: Description
                        }
                        else
                        {
                            row[1] = "";
                            row[2] = "";
                            row[3] = "";
                        }

                        // F: nodeType, G: content
                        if (i < nodeCount && skill.nodes[i] != null)
                        {
                            var node = skill.nodes[i];
                            row[4] = ((int)node.nodeType).ToString();
                            row[5] = JsonUtility.ToJson(node);
                        }
                        else
                        {
                            row[4] = "";
                            row[5] = "";
                        }

                        // H: ConnectionsJson
                        if (i < connCount && skill.connections[i] != null)
                        {
                            row[6] = JsonUtility.ToJson(skill.connections[i]);
                        }
                        else
                        {
                            row[6] = "";
                        }

                        dataRows.Add(row);
                    }
                }

                // 直接修改 xlsx 文件
                UpdateExcelData(excelPath, dataRows, DATA_START_ROW);

                Debug.Log($"[SkillExporter] 导出成功！共 {skills.Count} 个技能，{dataRows.Count} 行数据");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillExporter] 导出失败: {e}");
                throw;
            }
        }

        /// <summary>
        /// 直接修改 xlsx 文件的 sheet1.xml，只更新数据区域
        /// </summary>
        private static void UpdateExcelData(string excelPath, List<string[]> dataRows, int startRow)
        {
            // xlsx 是 zip 格式，直接操作 xml
            var tempPath = excelPath + ".tmp";

            using (var originalZip = ZipFile.Open(excelPath, ZipArchiveMode.Read))
            using (var newZip = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                foreach (var entry in originalZip.Entries)
                {
                    if (entry.FullName == "xl/worksheets/sheet1.xml")
                    {
                        // 修改 sheet1.xml
                        var newEntry = newZip.CreateEntry(entry.FullName);
                        using (var reader = new StreamReader(entry.Open()))
                        using (var writer = new StreamWriter(newEntry.Open()))
                        {
                            var xml = reader.ReadToEnd();
                            xml = UpdateSheetXml(xml, dataRows, startRow);
                            writer.Write(xml);
                        }
                    }
                    else if (entry.FullName == "xl/sharedStrings.xml")
                    {
                        // 更新 sharedStrings.xml
                        var newEntry = newZip.CreateEntry(entry.FullName);
                        using (var reader = new StreamReader(entry.Open()))
                        using (var writer = new StreamWriter(newEntry.Open()))
                        {
                            var xml = reader.ReadToEnd();
                            xml = UpdateSharedStrings(xml, dataRows);
                            writer.Write(xml);
                        }
                    }
                    else
                    {
                        // 复制其他文件
                        var newEntry = newZip.CreateEntry(entry.FullName);
                        using (var source = entry.Open())
                        using (var dest = newEntry.Open())
                        {
                            source.CopyTo(dest);
                        }
                    }
                }
            }

            // 替换原文件
            File.Delete(excelPath);
            File.Move(tempPath, excelPath);
        }

        private static string UpdateSheetXml(string xml, List<string[]> dataRows, int startRow)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

            var sheetData = doc.SelectSingleNode("//x:sheetData", nsManager);
            if (sheetData == null) return xml;

            // 删除从 startRow 开始的所有行
            var rowsToRemove = new List<XmlNode>();
            foreach (XmlNode row in sheetData.SelectNodes($"x:row[@r >= {startRow}]", nsManager))
            {
                rowsToRemove.Add(row);
            }
            foreach (var row in rowsToRemove)
            {
                sheetData.RemoveChild(row);
            }

            // 添加新数据行
            for (int i = 0; i < dataRows.Count; i++)
            {
                var rowNum = startRow + i;
                var rowElement = doc.CreateElement("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                rowElement.SetAttribute("r", rowNum.ToString());

                var rowData = dataRows[i];
                for (int col = 0; col < rowData.Length; col++)
                {
                    var cellRef = GetCellReference(col, rowNum);
                    var cellElement = doc.CreateElement("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                    cellElement.SetAttribute("r", cellRef);

                    var value = rowData[col];
                    if (!string.IsNullOrEmpty(value))
                    {
                        // 检查是否为数字
                        if (int.TryParse(value, out _))
                        {
                            var vElement = doc.CreateElement("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                            vElement.InnerText = value;
                            cellElement.AppendChild(vElement);
                        }
                        else
                        {
                            // 字符串类型，使用内联字符串
                            cellElement.SetAttribute("t", "inlineStr");
                            var isElement = doc.CreateElement("is", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                            var tElement = doc.CreateElement("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                            tElement.InnerText = value;
                            isElement.AppendChild(tElement);
                            cellElement.AppendChild(isElement);
                        }
                    }

                    rowElement.AppendChild(cellElement);
                }

                sheetData.AppendChild(rowElement);
            }

            return doc.OuterXml;
        }

        private static string UpdateSharedStrings(string xml, List<string[]> dataRows)
        {
            // 保持原有的 sharedStrings 不变，因为我们使用 inlineStr
            return xml;
        }

        private static string GetCellReference(int colIndex, int rowNum)
        {
            return $"{(char)('A' + colIndex)}{rowNum}";
        }
    }
}
