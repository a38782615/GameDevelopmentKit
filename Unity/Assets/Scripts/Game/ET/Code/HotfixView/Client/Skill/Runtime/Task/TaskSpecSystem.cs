using System;

namespace ET.Client
{
    [EntitySystemOf(typeof(TaskSpec))]
    [FriendOf(typeof(TaskSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]
    public static partial class TaskSpecSystem
    {
        [EntitySystem]
        private static void Awake(this TaskSpec self)
        {
        }

        [EntitySystem]
        private static void Destroy(this TaskSpec self)
        {
        }

        // ============ 初始化 ============
        public static void Initialize(this TaskSpec self, string skillId, string nodeGuid, SpecExecutionContext context)
        {
            self.SpecId = Guid.NewGuid().ToString();
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.Context = context;
            self.Source = context.Caster; 
        }

        // ============ 辅助方法 ============
        public static AbilitySystemComponent GetTarget(this TaskSpec self)
        {
            var nodeData = self.NodeData;
            return nodeData == null ? self.Context?.MainTarget : self.Context.GetTargetByType(nodeData.targetType);
        }
    }
}