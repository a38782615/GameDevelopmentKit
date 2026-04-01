namespace ET.Client
{
    [EntitySystemOf(typeof(ConditionSpec))]
    [FriendOf(typeof(ConditionSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
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

        public static void InitCondition(this ConditionSpec self, string skillId, string nodeGuid, SpecExecutionContext context)
        {
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.Context = context;
            self.Source = context.Caster;

            ConditionNodeData nodeData = self.GetConditionNodeData();
            if (nodeData != null)
            {
                self.AttachConditionComponent(nodeData.nodeType);
            }
        }

        public static bool Evaluate(this ConditionSpec self)
        {
            SpecExecutionContext context = self.Context;
            if (context == null)
            {
                return false;
            }

            ConditionNodeData nodeData = self.GetConditionNodeData();
            if (nodeData == null)
            {
                return false;
            }

            AConditionHandler handler = self.GetHandler();
            if (handler == null)
            {
                Log.Error($"ConditionHandler not found for NodeType: {nodeData.nodeType}");
                return false;
            }

            AbilitySystemComponent target = self.GetConditionTarget();
            return handler.Evaluate(target);
        }

        public static SpecExecutionContext GetContext(this ConditionSpec self)
        {
            return self.Context.As();
        }

        public static AbilitySystemComponent GetConditionTarget(this ConditionSpec self)
        {
            SpecExecutionContext context = self.Context;
            NodeData nodeData = self.GetNodeData();
            if (context == null)
            {
                return null;
            }

            TargetType targetType = nodeData?.targetType ?? TargetType.MainTarget;
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

        public static NodeData GetNodeData(this ConditionSpec self)
        {
            return self == null ? null : SkillDataCenter.Instance.GetNodeData(self.SkillId, self.NodeGuid);
        }

        public static ConditionNodeData GetConditionNodeData(this ConditionSpec self)
        {
            return self.GetNodeData() as ConditionNodeData;
        }

        private static AConditionHandler GetHandler(this ConditionSpec self)
        {
            if (string.IsNullOrEmpty(self.HandName))
            {
                return null;
            }

            AConditionHandler handler = ConditionDispatcherComponent.Instance.Get(self.HandName);
            if (handler == null)
            {
                return null;
            }

            handler.Spec = self;
            handler.NodeData = self.GetConditionNodeData();
            return handler;
        }

        private static void AttachConditionComponent(this ConditionSpec self, NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.AttributeCompareCondition:
                    self.HandName = "AttributeCompareConditionHandler";
                    self.EnsureConditionComponent<AttributeCompareConditionSpec>();
                    return;
                default:
                    self.HandName = string.Empty;
                    return;
            }
        }

        private static void EnsureConditionComponent<T>(this ConditionSpec self) where T : Entity, IAwake, new()
        {
            if (self.GetComponent<T>() == null)
            {
                self.AddComponent<T>();
            }
        }
    }
}
