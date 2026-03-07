namespace ET.Client
{
    [EntitySystemOf(typeof(ConditionSpec))]
    [FriendOf(typeof(ConditionSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]

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
        }

        // ============ 执行 ============

        /// <summary>
        /// 执行条件判断，通过 Dispatcher 查找 Handler 执行 Evaluate
        /// </summary>
        public static void Execute(this ConditionSpec self, SpecExecutionContext context)
        {
            if (context == null) return;

            var nodeData = self.NodeData;
            if (nodeData == null) return;

            // 通过 Dispatcher 查找 Handler
            var handler = ConditionDispatcherComponent.Instance.Get(nodeData.GetType().Name);
            handler.Spec = self;
            if (handler == null)
            {
                Log.Error($"ConditionHandler not found for NodeType: {nodeData.nodeType}");
                return;
            }

            // 获取目标
            var target = self.GetConditionTarget(context);

            // 执行条件判断
            bool result = handler.Evaluate(target);

            // 根据结果执行对应分支
            context.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, result ? "是" : "否");
        }

        // ============ 辅助方法 ============

        private static AbilitySystemComponent GetConditionTarget(this ConditionSpec self, SpecExecutionContext context)
        {
            var nodeData = self.NodeData;
            if (nodeData == null) return context?.GetMainTarget();
            return context?.GetTargetByType(nodeData.targetType);
        }
    }
}
