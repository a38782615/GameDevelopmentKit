using System;
using System.Collections.Generic;

namespace ET.Client
{
    [Code]
    public class TaskDispatcherComponent : Singleton<TaskDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, ATaskHandler> taskHandlers = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(TaskHandlerAttribute));
            foreach (Type type in types)
            {
                ATaskHandler handler = Activator.CreateInstance(type) as ATaskHandler;
                if (handler == null)
                {
                    Log.Error($"TaskHandler is not ATaskHandler: {type.Name}");
                    continue;
                }

                this.taskHandlers[type.Name] = handler;
            }
        }

        public ATaskHandler Get(string key)
        {
            this.taskHandlers.TryGetValue(key, out var handler);
            return handler;
        }
    }
}
