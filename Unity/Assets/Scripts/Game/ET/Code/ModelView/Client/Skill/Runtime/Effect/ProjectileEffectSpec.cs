using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ProjectileEffectSpec : Entity, IAwake
    {
        public EntityRef<UGFEntityProjectile> ProjectileEntity;
        public GameObject ProjectileObject;
        public Vector2 ExpectedTargetPosition;
        public bool HasTriggeredHit;
    }
}
