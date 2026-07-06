using Unity.Mathematics;

namespace ET
{
    public struct ChangePosition
    {
        public Unit Unit;
        public float3 OldPos;
    }

    public struct ChangeRotation
    {
        public Unit Unit;
    }

    public struct UnitDeath
    {
        public Unit Unit;
    }
}
