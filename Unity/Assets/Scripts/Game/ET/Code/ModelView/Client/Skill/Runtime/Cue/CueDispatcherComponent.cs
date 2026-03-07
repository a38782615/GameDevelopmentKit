using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 条件分发器 - 参考 CueDispatcherComponent
    /// 自动收集所有 [ACueHandler] 标记的 Handler
    /// </summary>
    [Code]
    public class CueDispatcherComponent : Singleton<CueDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, ACueHandler> cueHandler = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(CueHandlerAttribute));
            foreach (Type type in types)
            {
                ACueHandler handler = Activator.CreateInstance(type) as ACueHandler;
                if (handler == null)
                {
                    Log.Error($"ACueHandler is not ACueHandler: {type.Name}");
                    continue;
                }

                this.cueHandler[type.Name] = handler;
            }
        }

        /// <summary>
        /// 根据节点类型获取条件Handler
        /// </summary>
        public ACueHandler Get(string key)
        {
            this.cueHandler.TryGetValue(key, out var handler);
            return handler;
        }
    }
}
