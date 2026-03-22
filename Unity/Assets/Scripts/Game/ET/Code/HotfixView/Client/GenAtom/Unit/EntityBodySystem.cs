using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(EntityBody))]
    [FriendOf(typeof(EntityBody))]
    public static partial class EntityBodySystem
    {
        [EntitySystem]
        private static void Awake(this EntityBody self)
        {
            Unit unit = self.GetParent<Unit>();
            DRUnitConfig config = unit?.Config();
            if (config == null)
            {
                self.Shape = EntityBody.CircleShape;
                self.Width = 0;
                self.Height = 0;
                return;
            }

            self.Shape = config.Shape;
            self.Width = config.Width;
            self.Height = config.Height;
            unit.Scene()?.GetComponent<BodyCheckComponent>()?.Register(self);
        }

        [EntitySystem]
        private static void Destroy(this EntityBody self)
        {
            self.Scene()?.GetComponent<BodyCheckComponent>()?.Unregister(self);
        }

        public static Unit GetUnit(this EntityBody self)
        {
            return self?.GetParent<Unit>();
        }

        public static float2 GetCenter(this EntityBody self)
        {
            Unit unit = self.GetUnit();
            return unit == null ? float2.zero : unit.Position.ToPlanar();
        }

        public static bool IsCircle(this EntityBody self)
        {
            return self != null && self.Shape == EntityBody.CircleShape;
        }

        public static float GetBoundingRadius(this EntityBody self)
        {
            if (self == null)
            {
                return 0f;
            }

            if (self.IsCircle())
            {
                return self.Width * 0.5f;
            }

            return math.length(new float2(self.Width, self.Height)) * 0.5f;
        }

        public static AbilitySystemComponent GetAbilitySystem(this EntityBody self)
        {
            Unit unit = self.GetUnit();
            return unit?.GetComponent<SkillUnit>()?.ASC.As();
        }
    }
}
