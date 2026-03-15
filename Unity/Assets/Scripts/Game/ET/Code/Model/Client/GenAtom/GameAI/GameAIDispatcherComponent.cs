using System;
using System.Collections.Generic;

namespace ET.Client
{
    [Code]
    public class GameAIDispatcherComponent : Singleton<GameAIDispatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<string, AGameAIHandler> gameAIHandlers = new();

        public void Awake()
        {
            var types = CodeTypes.Instance.GetTypes(typeof(GameAIHandlerAttribute));
            foreach (Type type in types)
            {
                AGameAIHandler gameAIHandler = Activator.CreateInstance(type) as AGameAIHandler;
                if (gameAIHandler == null)
                {
                    Log.Error($"game ai handler is invalid: {type.Name}");
                    continue;
                }

                this.TryAdd(type.Name, gameAIHandler);

                const string prefix = "GameAI_";
                if (type.Name.StartsWith(prefix))
                {
                    string shortName = type.Name.Substring(prefix.Length);
                    this.TryAdd(shortName, gameAIHandler);
                    this.TryAdd($"AI_{shortName}", gameAIHandler);
                }
            }
        }

        public AGameAIHandler Get(string key)
        {
            this.gameAIHandlers.TryGetValue(key, out AGameAIHandler gameAIHandler);
            return gameAIHandler;
        }

        private void TryAdd(string key, AGameAIHandler handler)
        {
            if (string.IsNullOrEmpty(key) || handler == null || this.gameAIHandlers.ContainsKey(key))
            {
                return;
            }

            this.gameAIHandlers.Add(key, handler);
        }
    }
}
