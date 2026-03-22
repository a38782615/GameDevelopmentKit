using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class MovementAgent : Entity, IAwake, IDestroy
    {
        public int AgentNo = -1;
        public float Radius;
        public float NeighborDist;
        public int MaxNeighbors;
        public float TimeHorizon;
        public float TimeHorizonObst;
        public float MaxSpeed;
        public float2 Velocity;
    }
}
