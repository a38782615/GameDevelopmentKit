namespace ET.Client
{
    [EntitySystemOf(typeof(ConditionSpec))]
    [FriendOf(typeof(ConditionSpec))]
    public static partial class ConditionSpecSystem
    {
        [EntitySystem]
        private static void Awake(this ConditionSpec self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ConditionSpec self)
        {
        }

        // ============ 初始化 ============

        public static void InitCondition(this ConditionSpec self, string skillId, string nodeGuid, SpecExecutionContext context)
        {
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.Context = context;
            self.Source = context.Caster;

            var nodeData = self.ConditionNodeData;
            if (nodeData != null)
                SpecFactory.AttachConditionComponent(self, nodeData.nodeType);
        }

        public static bool Evaluate(this ConditionSpec self)
        {
            if (self.Context == null) return false;

            var nodeData = self.ConditionNodeData;
            if (nodeData == null) return false;

            var handler = self.GetHandler();
            if (handler == null)
            {
                Log.Error($"ConditionHandler not found for NodeType: {nodeData.nodeType}");
                return false;
            }

            var target = self.GetConditionTarget();
            return handler.Evaluate(target);
        }

        public static SpecExecutionContext GetContext(this ConditionSpec self)
        {
            return self.Context;
        }

        public static AbilitySystemComponent GetConditionTarget(this ConditionSpec self)
        {
            var context = self.Context;
            var nodeData = self.NodeData;
            if (context == null)
                return null;

            var targetType = nodeData?.targetType ?? TargetType.MainTarget;
            switch (targetType)
            {
                case TargetType.Caster:
                    return context.Caster.As();
                case TargetType.ParentInput:
                    return context.ParentInputTarget.As();
                default:
                    return context.MainTarget.As();
            }
        }

        private static AConditionHandler GetHandler(this ConditionSpec self)
        {
            if (string.IsNullOrEmpty(self.HandName))
                return null;

            var handler = ConditionDispatcherComponent.Instance.Get(self.HandName);
            if (handler == null)
                return null;

            handler.Spec = self;
            handler.NodeData = self.ConditionNodeData;
            return handler;
        }
    }
}
