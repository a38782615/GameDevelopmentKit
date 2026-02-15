using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace ET.Editor.Tools
{
    /// <summary>
    /// 技能资源重命名工具
    /// 将中文技能名重命名为 CD Tag 对应的英文名
    /// </summary>
    public class SkillAssetRenamer : EditorWindow
    {
        private const string SKILL_ASSET_PATH = "Assets/Unity/Resources/ScriptObject/SkillAsset";

        /// <summary>
        /// 中文技能名 -> CD Tag 英文名 映射
        /// </summary>
        private static readonly Dictionary<string, string> SkillNameMapping = new Dictionary<string, string>
        {
            { "横扫", "Sweep" },
            { "流血", "Blood" },
            { "旋风斩", "FireCircle" },
            { "践踏", "Wind" },
            { "回血", "RecBlood" },
            { "万象天引", "Wan" },
            { "神罗天正", "God" },
            { "被动回血", "BeRecBlood" },
            { "三火球", "ThreeFire" },
            { "急速", "SpeedUp" },
            { "火球术", "RuFood" },
        };

        [MenuItem("SkillEditor/重命名技能资源为英文")]
        public static void RenameSkillAssets()
        {
            var guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SKILL_ASSET_PATH });
            int renamedCount = 0;
            var renamedList = new List<string>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(assetPath);

                if (SkillNameMapping.TryGetValue(fileName, out var englishName))
                {
                    var newPath = Path.Combine(Path.GetDirectoryName(assetPath), englishName + ".asset");

                    // 检查目标文件是否已存在
                    if (File.Exists(newPath))
                    {
                        Debug.LogWarning($"[SkillRenamer] 跳过 {fileName}: 目标文件 {englishName}.asset 已存在");
                        continue;
                    }

                    var error = AssetDatabase.RenameAsset(assetPath, englishName);
                    if (string.IsNullOrEmpty(error))
                    {
                        renamedList.Add($"{fileName} -> {englishName}");
                        renamedCount++;
                        Debug.Log($"[SkillRenamer] 重命名: {fileName} -> {englishName}");
                    }
                    else
                    {
                        Debug.LogError($"[SkillRenamer] 重命名失败 {fileName}: {error}");
                    }
                }
                else
                {
                    Debug.Log($"[SkillRenamer] 跳过 {fileName}: 无对应英文名映射");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = renamedCount > 0
                ? $"已重命名 {renamedCount} 个技能:\n" + string.Join("\n", renamedList)
                : "没有需要重命名的技能";

            EditorUtility.DisplayDialog("重命名完成", message, "确定");
        }

        [MenuItem("SkillEditor/更新技能 SkillId 字段")]
        public static void UpdateSkillIds()
        {
            var guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SKILL_ASSET_PATH });
            int updatedCount = 0;

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath);

                if (asset != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(assetPath);
                    var expectedSkillId = $"Skill_{fileName}";

                    if (asset.SkillId != expectedSkillId)
                    {
                        asset.SkillId = expectedSkillId;
                        EditorUtility.SetDirty(asset);
                        updatedCount++;
                        Debug.Log($"[SkillRenamer] 更新 SkillId: {fileName} -> {expectedSkillId}");
                    }
                }
            }

            AssetDatabase.SaveAssets();

            var message = updatedCount > 0
                ? $"已更新 {updatedCount} 个技能的 SkillId"
                : "所有技能的 SkillId 已是最新";

            EditorUtility.DisplayDialog("更新完成", message, "确定");
        }
    }
}
