using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 条件分发器 - 参考 AIDispatcherComponent
    /// 自动收集所有 [ConditionHandler] 标记的 Handler
    /// </summary>
    [Code]
    public class ConditionDispatcherComponent : Singleton<ConditionDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, AConditionHandler> conditionHandlers = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(ConditionHandlerAttribute));
            foreach (Type type in types)
            {
                AConditionHandler handler = Activator.CreateInstance(type) as AConditionHandler;
                if (handler == null)
                {
                    Log.Error($"ConditionHandler is not AConditionHandler: {type.Name}");
                    continue;
                }

                this.conditionHandlers[type.Name] = handler;
            }
        }

        /// <summary>
        /// 根据节点类型获取条件Handler
        /// </summary>
        public AConditionHandler Get(string key)
        {
            this.conditionHandlers.TryGetValue(key, out var handler);
            return handler;
        }
    }
}
