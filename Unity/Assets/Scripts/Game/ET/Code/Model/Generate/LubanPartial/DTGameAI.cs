using System.Collections.Generic;

namespace ET
{
    public partial class DTGameAI
    {
        public Dictionary<int, SortedDictionary<int, DRGameAI>> GameAIs = new Dictionary<int, SortedDictionary<int, DRGameAI>>();

        public SortedDictionary<int, DRGameAI> GetGameAI(int aiConfigId)
        {
            return this.GameAIs[aiConfigId];
        }

        partial void PostInit()
        {
            this.GameAIs.Clear();
            foreach (var kv in this.DataMap)
            {
                if (!this.GameAIs.TryGetValue(kv.Value.AIConfigId, out SortedDictionary<int, DRGameAI> aiNodeConfig))
                {
                    aiNodeConfig = new SortedDictionary<int, DRGameAI>();
                    this.GameAIs.Add(kv.Value.AIConfigId, aiNodeConfig);
                }

                aiNodeConfig[kv.Value.Order] = kv.Value;
            }
        }
    }
}
