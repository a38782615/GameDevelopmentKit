using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ET.Client
{
    /// <summary>
    /// 技能数据转换器 - 将 Luban 表数据转换为 SkillData
    /// </summary>
    public static class SkillDataConverter
    {
        [StaticField]
        // 节点类型映射表
        private static readonly Dictionary<string, Type> NodeTypeMap = new Dictionary<string, Type>
        {
            { "AbilityNodeData", typeof(AbilityNodeData) },
            { "DamageEffectNodeData", typeof(DamageEffectNodeData) },
            { "HealEffectNodeData", typeof(HealEffectNodeData) },
            { "CostEffectNodeData", typeof(CostEffectNodeData) },
            { "ModifyAttributeEffectNodeData", typeof(ModifyAttributeEffectNodeData) },
            { "ProjectileEffectNodeData", typeof(ProjectileEffectNodeData) },
            { "PlacementEffectNodeData", typeof(PlacementEffectNodeData) },
            { "CooldownEffectNodeData", typeof(CooldownEffectNodeData) },
            { "BuffEffectNodeData", typeof(BuffEffectNodeData) },
            { "GenericEffectNodeData", typeof(GenericEffectNodeData) },
            { "DisplaceEffectNodeData", typeof(DisplaceEffectNodeData) },
            { "ParticleCueNodeData", typeof(ParticleCueNodeData) },
            { "SoundCueNodeData", typeof(SoundCueNodeData) },
            { "FloatingTextCueNodeData", typeof(FloatingTextCueNodeData) },
            { "SearchTargetTaskNodeData", typeof(SearchTargetTaskNodeData) },
            { "EndAbilityTaskNodeData", typeof(EndAbilityTaskNodeData) },
            { "AnimationNodeData", typeof(AnimationNodeData) },
            { "AttributeCompareConditionNodeData", typeof(AttributeCompareConditionNodeData) },
        };

        /// <summary>
        /// 将 TableSkillGraph 转换为 SkillData
        /// </summary>
        public static SkillData Convert(ET.DRSkillGraph tableData)
        {
            if (tableData == null)
                return null;

            var skillData = new SkillData
            {
                SkillId = tableData.Id.ToString(),
                nodes = ParseNodes(tableData.NodesJsons),
                connections = ParseConnections(tableData.ConnectionsJson)
            };

            return skillData;
        }

        /// <summary>
        /// 批量转换并注册到 SkillDataCenter
        /// </summary>
        public static void ConvertAndRegisterAll(DTSkillGraph tbSkillGraph)
        {
            if (tbSkillGraph == null)
                return;

            foreach (var tableData in tbSkillGraph.DataList)
            {
                var skillData = Convert(tableData);
                if (skillData != null)
                {
                    SkillDataCenter.Instance.RegisterSkillGraph(skillData);
                }
            }
        }

        /// <summary>
        /// 解析节点 JSON 数据（支持多态）
        /// JSON 格式: [{"$type":"AbilityNodeData", "guid":"xxx", ...}, ...]
        /// </summary>
        private static List<NodeData> ParseNodes(List<NodeJson> nodesJson)
        {
            var result = new List<NodeData>();

            if (nodesJson == null || nodesJson.Count == 0)
                return result;

            try
            {
                // 简单解析 JSON 数组，提取每个对象

                foreach (var node in nodesJson)
                {
                    var nodeData = ParseSingleNode(node.Content);
                    if (nodeData != null)
                    {
                        result.Add(nodeData);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SkillDataConverter] Failed to parse nodes JSON: {e.Message}\nJSON: {nodesJson}");
            }

            return result;
        }

        /// <summary>
        /// 解析单个节点
        /// </summary>
        private static NodeData ParseSingleNode(string nodeJson)
        {
            // 提取 $type 字段
            var typeMatch = Regex.Match(nodeJson, @"\$type""\s*:\s*""(\w+)""");
            if (!typeMatch.Success)
            {
                // 尝试使用 nodeType 字段推断类型
                var nodeTypeMatch = Regex.Match(nodeJson, @"""nodeType""\s*:\s*(\d+)");
                if (nodeTypeMatch.Success && int.TryParse(nodeTypeMatch.Groups[1].Value, out int nodeTypeValue))
                {
                    var nodeType = (NodeType)nodeTypeValue;
                    return ParseNodeByType(nodeJson, nodeType);
                }

                Log.Warning($"[SkillDataConverter] Node missing $type field: {nodeJson}");
                return null;
            }

            string typeName = typeMatch.Groups[1].Value;

            if (!NodeTypeMap.TryGetValue(typeName, out Type nodeDataType))
            {
                Log.Warning($"[SkillDataConverter] Unknown node type: {typeName}");
                return null;
            }

            try
            {
                // 移除 $type 字段后使用 JsonUtility 解析
                string cleanJson = Regex.Replace(nodeJson, @",?\s*""\$type""\s*:\s*""[^""]*""", "");
                cleanJson = Regex.Replace(cleanJson, @"{\s*,", "{"); // 清理开头的逗号

                var nodeData = (NodeData)Newtonsoft.Json.JsonConvert.DeserializeObject(cleanJson, nodeDataType, CreateJsonSerializerSettings());

                return nodeData;
            }
            catch (Exception e)
            {
                Log.Error($"[SkillDataConverter] Failed to parse node of type {typeName}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据 NodeType 枚举解析节点
        /// </summary>
        private static NodeData ParseNodeByType(string nodeJson, NodeType nodeType)
        {
            Type targetType = nodeType switch
            {
                NodeType.Ability => typeof(AbilityNodeData),
                NodeType.DamageEffect => typeof(DamageEffectNodeData),
                NodeType.HealEffect => typeof(HealEffectNodeData),
                NodeType.CostEffect => typeof(CostEffectNodeData),
                NodeType.ModifyAttributeEffect => typeof(ModifyAttributeEffectNodeData),
                NodeType.ProjectileEffect => typeof(ProjectileEffectNodeData),
                NodeType.PlacementEffect => typeof(PlacementEffectNodeData),
                NodeType.CooldownEffect => typeof(CooldownEffectNodeData),
                NodeType.BuffEffect => typeof(BuffEffectNodeData),
                NodeType.GenericEffect => typeof(GenericEffectNodeData),
                NodeType.DisplaceEffect => typeof(DisplaceEffectNodeData),
                NodeType.ParticleCue => typeof(ParticleCueNodeData),
                NodeType.SoundCue => typeof(SoundCueNodeData),
                NodeType.FloatingTextCue => typeof(FloatingTextCueNodeData),
                NodeType.SearchTargetTask => typeof(SearchTargetTaskNodeData),
                NodeType.EndAbilityTask => typeof(EndAbilityTaskNodeData),
                NodeType.Animation => typeof(AnimationNodeData),
                NodeType.AttributeCompareCondition => typeof(AttributeCompareConditionNodeData),
                _ => typeof(NodeData)
            };

            try
            {
                var nodeData = (NodeData)Newtonsoft.Json.JsonConvert.DeserializeObject(nodeJson, targetType, CreateJsonSerializerSettings());
                return nodeData;
            }
            catch (Exception e)
            {
                Log.Error($"[SkillDataConverter] Failed to parse node by NodeType {nodeType}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析连接 JSON 数据
        /// </summary>
        private static List<ConnectionData> ParseConnections(List<string> connectionsJson)
        {
            var result = new List<ConnectionData>();

            if (connectionsJson == null || connectionsJson.Count == 0)
                return result;

            try
            {
                foreach (var connJson in connectionsJson)
                {
                    var connection = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionData>(connJson);
                    if (connection != null)
                    {
                        result.Add(connection);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SkillDataConverter] Failed to parse connections JSON: {e.Message}\nJSON: {connectionsJson}");
            }

            return result;
        }

        private static Newtonsoft.Json.JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.Converters.Add(new UnityObjectNullJsonConverter());
            return settings;
        }

    }
}
