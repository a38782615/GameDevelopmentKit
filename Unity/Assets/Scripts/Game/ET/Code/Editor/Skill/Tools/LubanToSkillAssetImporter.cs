using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniExcelLibs;

using UnityEditor;
using UnityEngine;

namespace ET.Client.Editor
{
    /// <summary>
    /// Luban Excel 导入到 SkillAsset 工具
    /// 将 #SkillGraph.xlsx 还原为 SkillGraphData ScriptableObject
    /// </summary>
    public class LubanToSkillAssetImporter : EditorWindow
    {
        private const int DATA_START_ROW = 5; // 数据从第5行开始
        private const int HEADER_ROW = 0;     // 第1行：主字段名
        private const int SUB_HEADER_ROW = 2; // 第3行：子字段名

        // Excel 列头名称常量 —— 与 SkillGraph.xlsx 表头对应
        private const string COL_ID = "Id";                         // B列 - 主键ID
        private const string COL_NAME = "Name";                     // C列 - 技能名称
        private const string COL_DESCRIPTION = "Description";       // D列 - 技能描述
        private const string COL_NODES_JSON = "*NodesJsons";        // E列 - 节点列表（复合字段父列）
        private const string COL_NODE_TYPE = "nodeType";            // E列 - 节点类型（子字段）
        private const string COL_NODE_CONTENT = "content";          // F列 - 节点数据JSON（子字段）
        private const string COL_CONNECTIONS_JSON = "*ConnectionsJson"; // G列 - 连接数据JSON

        // NodeType → Type 映射
        private static readonly Dictionary<int, Type> NodeTypeMap = new Dictionary<int, Type>
        {
            { 0, typeof(AbilityNodeData) },           // Ability
            { 1, typeof(DamageEffectNodeData) },      // DamageEffect
            { 2, typeof(HealEffectNodeData) },        // HealEffect
            { 3, typeof(CostEffectNodeData) },        // CostEffect
            { 4, typeof(SearchTargetTaskNodeData) },  // SearchTargetTask
            { 5, typeof(AttributeCompareConditionNodeData) }, // AttributeCompareCondition
            { 6, typeof(ModifyAttributeEffectNodeData) },     // ModifyAttributeEffect
            { 7, typeof(EndAbilityTaskNodeData) },    // EndAbilityTask
            { 8, typeof(ProjectileEffectNodeData) },  // ProjectileEffect
            { 9, typeof(PlacementEffectNodeData) },   // PlacementEffect
            { 10, typeof(CooldownEffectNodeData) },   // CooldownEffect
            { 11, typeof(BuffEffectNodeData) },       // BuffEffect
            { 12, typeof(ParticleCueNodeData) },      // ParticleCue
            { 13, typeof(SoundCueNodeData) },         // SoundCue
            { 14, typeof(FloatingTextCueNodeData) },  // FloatingTextCue
            { 20, typeof(AnimationNodeData) },        // Animation
            { 21, typeof(GenericEffectNodeData) },    // GenericEffect
            { 22, typeof(DisplaceEffectNodeData) },   // DisplaceEffect
        };

        private Vector2 _scrollPosition;
        private List<SkillImportData> _skillsToImport = new List<SkillImportData>();
        private List<bool> _selectedSkills = new List<bool>();
        private bool _selectAll = true;
        private string _lastError = "";

        /// <summary>
        /// 技能导入数据结构
        /// </summary>
        private class SkillImportData
        {
            public int Id;
            public string Name;
            public string Description;
            public List<NodeData> Nodes = new List<NodeData>();
            public List<ConnectionData> Connections = new List<ConnectionData>();
            public bool Exists; // 是否已存在对应的 asset
            public List<string> Errors = new List<string>();
        }

        [MenuItem("SkillEditor/从 Excel 导入技能")]
        public static void ShowWindow()
        {
            var window = GetWindow<LubanToSkillAssetImporter>("技能导入工具");
            window.minSize = new Vector2(500, 600);
            window.LoadExcelData();
        }

        [MenuItem("SkillEditor/快速导入所有技能")]
        public static void QuickImportAll()
        {
            var skills = ParseExcelData();
            if (skills.Count > 0)
            {
                int imported = 0;
                foreach (var skill in skills)
                {
                    if (skill.Errors.Count == 0)
                    {
                        ImportSkill(skill);
                        imported++;
                    }
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("导入完成", $"已导入 {imported} 个技能", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("导入失败", "未找到可导入的技能数据", "确定");
            }
        }

        private void LoadExcelData()
        {
            _skillsToImport = ParseExcelData();
            _selectedSkills = _skillsToImport.Select(_ => true).ToList();

            if (_skillsToImport.Count == 0)
            {
                _lastError = "未找到技能数据";
            }
            else
            {
                _lastError = "";
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Excel 导入 SkillAsset 工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("从 SkillGraph.xlsx 导入技能数据到 SkillAsset", MessageType.Info);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("刷新 Excel 数据", GUILayout.Height(25)))
                LoadExcelData();

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }

            EditorGUILayout.Space(10);

            // 全选
            EditorGUI.BeginChangeCheck();
            _selectAll = EditorGUILayout.Toggle("全选", _selectAll);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < _selectedSkills.Count; i++)
                    _selectedSkills[i] = _selectAll;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"技能列表 ({_skillsToImport.Count} 个)", EditorStyles.boldLabel);

            // 技能列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(350));
            for (int i = 0; i < _skillsToImport.Count; i++)
            {
                var skill = _skillsToImport[i];
                EditorGUILayout.BeginHorizontal();

                _selectedSkills[i] = EditorGUILayout.Toggle(_selectedSkills[i], GUILayout.Width(20));

                // 状态图标
                if (skill.Errors.Count > 0)
                {
                    EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
                }
                else if (skill.Exists)
                {
                    EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                }
                else
                {
                    EditorGUILayout.LabelField("✚", GUILayout.Width(20));
                }

                EditorGUILayout.LabelField(skill.Name, GUILayout.Width(120));
                EditorGUILayout.LabelField($"节点:{skill.Nodes.Count} 连接:{skill.Connections.Count}", GUILayout.Width(120));

                if (skill.Exists)
                {
                    EditorGUILayout.LabelField("(已存在)", GUILayout.Width(60));
                }
                else
                {
                    EditorGUILayout.LabelField("(新建)", GUILayout.Width(60));
                }

                EditorGUILayout.EndHorizontal();

                // 显示错误
                if (skill.Errors.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    foreach (var error in skill.Errors)
                    {
                        EditorGUILayout.LabelField(error, EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 图例
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图例: ✚ 新建  ✓ 更新  ⚠ 有错误", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 导入按钮
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("导入选中的技能", GUILayout.Height(40)))
                ImportSelectedSkills();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // 路径信息
            var excelPath = SkillAssetToLubanExporter.GetExcelPath();
            EditorGUILayout.LabelField("Excel 路径:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(excelPath, EditorStyles.textField, GUILayout.Height(20));

            EditorGUILayout.LabelField("输出路径:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(SkillAssetTreeView.RootPath, EditorStyles.textField, GUILayout.Height(20));
        }
        private void ImportSelectedSkills()
        {
            int imported = 0;
            int skipped = 0;
            var errors = new List<string>();

            for (int i = 0; i < _skillsToImport.Count; i++)
            {
                if (!_selectedSkills[i]) continue;

                var skill = _skillsToImport[i];
                if (skill.Errors.Count > 0)
                {
                    skipped++;
                    errors.Add($"{skill.Name}: {string.Join(", ", skill.Errors)}");
                    continue;
                }

                try
                {
                    ImportSkill(skill);
                    imported++;
                }
                catch (Exception e)
                {
                    skipped++;
                    errors.Add($"{skill.Name}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"导入完成！\n成功: {imported}\n跳过: {skipped}";
            if (errors.Count > 0)
            {
                message += $"\n\n错误:\n{string.Join("\n", errors.Take(5))}";
                if (errors.Count > 5)
                    message += $"\n... 还有 {errors.Count - 5} 个错误";
            }

            EditorUtility.DisplayDialog("导入结果", message, "确定");
            LoadExcelData(); // 刷新列表
        }

        /// <summary>
        /// 从表头行构建 列头名称 → 列字母 的映射
        /// Luban Excel 结构：第1行主字段名，第3行子字段名
        /// </summary>
        private static Dictionary<string, string> BuildColumnMap(IList<dynamic> rows)
        {
            var columnMap = new Dictionary<string, string>();

            // 第1行（索引0）：主字段名 —— Id, Name, Description, *NodesJsons, *ConnectionsJson
            var headerRow = rows[HEADER_ROW] as IDictionary<string, object>;
            if (headerRow != null)
            {
                foreach (var kvp in headerRow)
                {
                    var headerName = kvp.Value?.ToString();
                    if (!string.IsNullOrEmpty(headerName) && !headerName.StartsWith("##"))
                    {
                        columnMap[headerName] = kvp.Key;
                    }
                }
            }

            // 第3行（索引2）：子字段名 —— nodeType, content（*NodesJsons 的子列）
            var subHeaderRow = rows[SUB_HEADER_ROW] as IDictionary<string, object>;
            if (subHeaderRow != null)
            {
                foreach (var kvp in subHeaderRow)
                {
                    var subName = kvp.Value?.ToString();
                    if (!string.IsNullOrEmpty(subName) && !subName.StartsWith("##"))
                    {
                        columnMap[subName] = kvp.Key;
                    }
                }
            }

            return columnMap;
        }

        /// <summary>
        /// 通过列头名称获取单元格值
        /// </summary>
        private static string GetCellByHeader(IDictionary<string, object> row, Dictionary<string, string> columnMap, string headerName)
        {
            if (columnMap.TryGetValue(headerName, out var colLetter))
            {
                return GetCellValue(row, colLetter);
            }
            return null;
        }

        /// <summary>
        /// 解析 Excel 数据
        /// </summary>
        private static List<SkillImportData> ParseExcelData()
        {
            var result = new List<SkillImportData>();
            var excelPath = SkillAssetToLubanExporter.GetExcelPath();

            if (!File.Exists(excelPath))
            {
                Debug.LogError($"[SkillImporter] Excel 文件不存在: {excelPath}");
                return result;
            }

            try
            {
                using (var stream = File.OpenRead(excelPath))
                {
                    var rows = MiniExcel.Query(stream, useHeaderRow: false, sheetName: "Sheet1").ToList();

                    if (rows.Count < DATA_START_ROW)
                    {
                        Debug.LogError("[SkillImporter] Excel 行数不足，缺少表头或数据");
                        return result;
                    }

                    // 从表头行构建列映射
                    var columnMap = BuildColumnMap(rows);

                    // 验证必要的列头是否存在
                    var requiredHeaders = new[] { COL_ID, COL_NAME, COL_NODE_TYPE, COL_NODE_CONTENT, COL_CONNECTIONS_JSON };
                    var missingHeaders = requiredHeaders.Where(h => !columnMap.ContainsKey(h)).ToList();
                    if (missingHeaders.Count > 0)
                    {
                        Debug.LogError($"[SkillImporter] Excel 缺少必要列头: {string.Join(", ", missingHeaders)}");
                        return result;
                    }

                    SkillImportData currentSkill = null;

                    // 从第5行开始（索引4）
                    for (int i = DATA_START_ROW - 1; i < rows.Count; i++)
                    {
                        var row = rows[i] as IDictionary<string, object>;
                        if (row == null) continue;

                        // 检查是否是新技能（Id 列有值）
                        var idValue = GetCellByHeader(row, columnMap, COL_ID);
                        if (!string.IsNullOrEmpty(idValue) && int.TryParse(idValue, out int id))
                        {
                            // 保存上一个技能
                            if (currentSkill != null)
                            {
                                result.Add(currentSkill);
                            }

                            var skillName = GetCellByHeader(row, columnMap, COL_NAME) ?? "";

                            // 创建新技能
                            currentSkill = new SkillImportData
                            {
                                Id = id,
                                Name = skillName,
                                Description = GetCellByHeader(row, columnMap, COL_DESCRIPTION) ?? "",
                                Exists = CheckAssetExists(skillName)
                            };
                        }

                        if (currentSkill == null) continue;

                        // 解析节点数据（nodeType + content 子字段）
                        var nodeTypeStr = GetCellByHeader(row, columnMap, COL_NODE_TYPE);
                        var nodeContent = GetCellByHeader(row, columnMap, COL_NODE_CONTENT);

                        if (!string.IsNullOrEmpty(nodeTypeStr) && !string.IsNullOrEmpty(nodeContent))
                        {
                            if (int.TryParse(nodeTypeStr, out int nodeType))
                            {
                                var node = ParseNode(nodeType, nodeContent);
                                if (node != null)
                                {
                                    currentSkill.Nodes.Add(node);
                                }
                                else
                                {
                                    currentSkill.Errors.Add($"无法解析节点类型 {nodeType}");
                                }
                            }
                        }

                        // 解析连接数据（*ConnectionsJson 列）
                        var connectionJson = GetCellByHeader(row, columnMap, COL_CONNECTIONS_JSON);
                        if (!string.IsNullOrEmpty(connectionJson))
                        {
                            try
                            {
                                var connection = JsonUtility.FromJson<ConnectionData>(connectionJson);
                                if (connection != null)
                                {
                                    currentSkill.Connections.Add(connection);
                                }
                            }
                            catch (Exception e)
                            {
                                currentSkill.Errors.Add($"连接解析失败: {e.Message}");
                            }
                        }
                    }

                    // 添加最后一个技能
                    if (currentSkill != null)
                    {
                        result.Add(currentSkill);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillImporter] 读取 Excel 失败: {e}");
            }

            return result;
        }

        /// <summary>
        /// 解析单个节点
        /// </summary>
        private static NodeData ParseNode(int nodeType, string json)
        {
            if (!NodeTypeMap.TryGetValue(nodeType, out Type targetType))
            {
                Debug.LogWarning($"[SkillImporter] 未知节点类型: {nodeType}");
                return null;
            }

            try
            {
                NodeData nodeData = (NodeData)JsonUtility.FromJson(json, targetType);
                SkillNodeAssetPathUtility.RestoreEditorAssetReferences(nodeData);
                return nodeData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillImporter] 节点解析失败 (type={nodeType}): {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 导入单个技能
        /// </summary>
        private static void ImportSkill(SkillImportData skillData)
        {
            var assetPath = $"{SkillAssetTreeView.RootPath}/{skillData.Name}.asset";

            foreach (NodeData node in skillData.Nodes)
            {
                SkillNodeAssetPathUtility.SyncSerializedAssetPath(node);
            }

            // 检查是否已存在
            SkillConnectionPortIdUtility.NormalizeNodePortIds(skillData.Nodes);
            SkillConnectionPortIdUtility.NormalizeConnections(skillData.Nodes, skillData.Connections);
            var existingAsset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath);

            if (existingAsset != null)
            {
                // 更新现有资源
                existingAsset.SkillId = skillData.Id.ToString();
                existingAsset.nodes = skillData.Nodes;
                existingAsset.connections = skillData.Connections;
                EditorUtility.SetDirty(existingAsset);
                Debug.Log($"[SkillImporter] 更新技能: {skillData.Name}");
            }
            else
            {
                // 创建新资源
                var newAsset = ScriptableObject.CreateInstance<SkillGraphData>();
                newAsset.SkillId = skillData.Id.ToString();
                newAsset.nodes = skillData.Nodes;
                newAsset.connections = skillData.Connections;

                // 确保目录存在
                if (!AssetDatabase.IsValidFolder(SkillAssetTreeView.RootPath))
                {
                    var folders = SkillAssetTreeView.RootPath.Split('/');
                    var currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        var nextPath = $"{currentPath}/{folders[i]}";
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = nextPath;
                    }
                }

                AssetDatabase.CreateAsset(newAsset, assetPath);
                Debug.Log($"[SkillImporter] 创建技能: {skillData.Name}");
            }
        }

        /// <summary>
        /// 检查资源是否已存在
        /// </summary>
        private static bool CheckAssetExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var assetPath = $"{SkillAssetTreeView.RootPath}/{name}.asset";
            return AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath) != null;
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private static string GetCellValue(IDictionary<string, object> row, string column)
        {
            if (row.TryGetValue(column, out var value) && value != null)
            {
                return value.ToString();
            }
            return null;
        }
    }
}
