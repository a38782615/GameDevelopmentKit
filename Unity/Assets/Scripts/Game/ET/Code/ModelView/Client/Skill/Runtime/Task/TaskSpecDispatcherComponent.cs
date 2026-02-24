using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 条件分发器 - 参考 TaskSpecDispatcherComponent
    /// 自动收集所有 [ATaskSpecHandler] 标记的 Handler
    /// </summary>
    [Code]
    public class TaskSpecDispatcherComponent : Singleton<TaskSpecDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, ATaskSpecHandler> TaskSpecHandler = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(TaskSpecHandlerAttribute));
            foreach (Type type in types)
            {
                ATaskSpecHandler handler = Activator.CreateInstance(type) as ATaskSpecHandler;
                if (handler == null)
                {
                    Log.Error($"ATaskSpecHandler is not ATaskSpecHandler: {type.Name}");
                    continue;
                }

                this.TaskSpecHandler[type.Name] = handler;
            }
        }

        /// <summary>
        /// 根据节点类型获取条件Handler
        /// </summary>
        public ATaskSpecHandler Get(string key)
        {
            this.TaskSpecHandler.TryGetValue(key, out var handler);
            return handler;
        }
    }
}
