using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 条件分发器 - 参考 EffectDispatcherComponent
    /// 自动收集所有 [AEffectHandler] 标记的 Handler
    /// </summary>
    [Code]
    public class EffectDispatcherComponent : Singleton<EffectDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, AEffectHandler> effectHandler = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(EffectHandlerAttribute));
            foreach (Type type in types)
            {
                AEffectHandler handler = Activator.CreateInstance(type) as AEffectHandler;
                if (handler == null)
                {
                    Log.Error($"AEffectHandler is not AEffectHandler: {type.Name}");
                    continue;
                }

                this.effectHandler[type.Name] = handler;
            }
        }

        /// <summary>
        /// 根据节点类型获取条件Handler
        /// </summary>
        public AEffectHandler Get(string key)
        {
            this.effectHandler.TryGetValue(key, out var handler);
            return handler;
        }
    }
}
