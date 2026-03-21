using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class GameplayCueManager : Entity, IAwake, IUpdate, IDestroy
    {
        public int LastTickFrame = -1;
        public readonly List<ActiveGameplayCue> ActiveCues = new List<ActiveGameplayCue>();
        public readonly List<ActiveGameplayCue> PendingRemoval = new List<ActiveGameplayCue>();
    }
}
