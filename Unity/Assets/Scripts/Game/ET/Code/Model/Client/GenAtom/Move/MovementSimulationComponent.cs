using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class MovementSimulationComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, EntityRef<MovementAgent>> Agents = new Dictionary<long, EntityRef<MovementAgent>>();
        public List<EntityRef<MovementAgent>> IndexedAgents = new List<EntityRef<MovementAgent>>();
        public RVO.Simulator Simulator = RVO.Simulator.Instance;
        public long Timer;
        public long LastStepTime;
        public float FixedTimeStep;
        public float MaxDeltaTime;
        public float Accumulator;
        public bool IsAgentDirty = true;
    }
}
