using Unity.Mathematics;

namespace ET.Client
{
    [FriendOf(typeof(MovementAgent))]
    [FriendOf(typeof(global::ET.Move2DComponent))]
    public static partial class MovementAgentRuntime
    {
        private const float DefaultRadius = 0.5f;
        private const float DefaultNeighborDistanceMultiplier = 6f;
        private const float DefaultMinNeighborDistance = 1f;
        private const int DefaultMaxNeighbors = 12;
        private const float DefaultTimeHorizon = 0.6f;
        private const float DefaultTimeHorizonObst = 0.1f;
        private const float DefaultMaxSpeed = 0.1f;

        public static void Initialize(this MovementAgent self)
        {
            if (self == null)
            {
                return;
            }

            self.AgentNo = -1;
            self.Velocity = float2.zero;
            self.MaxNeighbors = DefaultMaxNeighbors;
            self.TimeHorizon = DefaultTimeHorizon;
            self.TimeHorizonObst = DefaultTimeHorizonObst;

            self.ApplyConfig();
            self.RefreshDynamicSettings();
        }

        public static Unit GetUnit(this MovementAgent self)
        {
            return self?.GetParent<Unit>();
        }

        public static float2 GetPosition(this MovementAgent self)
        {
            Unit unit = self.GetUnit();
            return unit == null ? float2.zero : unit.Position.ToPlanar();
        }

        public static void SetVelocity(this MovementAgent self, float2 velocity)
        {
            if (self == null)
            {
                return;
            }

            self.Velocity = velocity;
        }

        public static void RefreshDynamicSettings(this MovementAgent self)
        {
            if (self == null)
            {
                return;
            }

            Unit unit = self.GetUnit();
            float speed = unit?.GetComponent<NumericComponent>()?.GetAsFloat(NumericType.Speed) ?? 0f;
            global::ET.Move2DComponent move2DComponent = unit?.GetComponent<global::ET.Move2DComponent>();
            if (move2DComponent != null && move2DComponent.StartTime != 0 && move2DComponent.Speed > 0.0001f)
            {
                speed = move2DComponent.Speed;
            }

            self.MaxSpeed = math.max(speed, DefaultMaxSpeed);
            self.NeighborDist = math.max(self.Radius * DefaultNeighborDistanceMultiplier, DefaultMinNeighborDistance);
        }

        private static void ApplyConfig(this MovementAgent self)
        {
            DRUnitConfig config = self.GetUnit()?.Config();
            self.Radius = GetConfigRadius(config);
        }

        private static float GetConfigRadius(DRUnitConfig config)
        {
            if (config == null)
            {
                return DefaultRadius;
            }

            if ((EntityBody.ShapeType)config.Shape == EntityBody.ShapeType.CircleShape && config.Width > 0)
            {
                return config.Width * 0.5f;
            }

            if (config.Width > 0 || config.Height > 0)
            {
                float2 size = new float2(math.max(config.Width, 1), math.max(config.Height, 1));
                return math.length(size) * 0.5f;
            }

            return DefaultRadius;
        }
    }
}
