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

            var taskSpec = TaskSpecDispatcherComponent.Instance.Get(self.NodeData.GetType().Name);
            taskSpec.NodeData = self.NodeData as TaskNodeData;
            taskSpec.Spec = self;
        }

        // ============ 辅助方法 ============
        public static AbilitySystemComponent GetTarget(this TaskSpec self)
        {
            var nodeData = self.NodeData;
            var context = self.Context;
            if (context == null)
            {
                return null;
            }

            if (nodeData == null)
            {
                return context.MainTarget;
            }

            switch (nodeData.targetType)
            {
                case TargetType.Caster:
                    return context.Caster;
                case TargetType.MainTarget:
                    return context.MainTarget;
                case TargetType.ParentInput:
                    return context.ParentInputTarget;
                default:
                    return context.MainTarget;
            }
        }

        public static void Execute(this TaskSpec self)
        {
            var taskSpec = TaskSpecDispatcherComponent.Instance.Get(self.NodeData.GetType().Name);
            taskSpec.Execute();
        }
    }
}