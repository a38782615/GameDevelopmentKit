namespace ET.Client
{
    [EntitySystemOf(typeof(TaskSpec))]
    [FriendOf(typeof(TaskSpec))]
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

        public static void InitTask(this TaskSpec self, string skillId, string nodeGuid, SpecExecutionContext context)
        {
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.Context = context;
            self.Source = context.Caster;

            var nodeData = self.TaskNodeData;
            if (nodeData != null)
                SpecFactory.AttachTaskComponent(self, nodeData.nodeType);
        }

        public static void Execute(this TaskSpec self)
        {
            var handler = self.GetHandler();
            if (handler == null)
            {
                var nodeType = self.TaskNodeData?.nodeType;
                Log.Error($"TaskHandler not found for NodeType: {nodeType}");
                return;
            }

            handler.Execute();
        }

        public static SpecExecutionContext GetContext(this TaskSpec self)
        {
            return self.Context;
        }

        public static AbilitySystemComponent GetTaskTarget(this TaskSpec self)
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

        private static ATaskHandler GetHandler(this TaskSpec self)
        {
            if (string.IsNullOrEmpty(self.HandName))
                return null;

            var handler = TaskDispatcherComponent.Instance.Get(self.HandName);
            if (handler == null)
                return null;

            handler.Spec = self;
            handler.NodeData = self.TaskNodeData;
            return handler;
        }
    }
}
