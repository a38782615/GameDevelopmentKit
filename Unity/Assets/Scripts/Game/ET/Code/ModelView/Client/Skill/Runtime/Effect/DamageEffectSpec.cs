using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 伤害效果Spec（瞬时效果）
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class DamageEffectSpec : Entity, IAwake
    {
        public bool HasRuntimeFollowup;
        public Vector3 KnockbackDirection;
        public float KnockbackRemainingDistance;
        public float KnockbackSpeed;
        public Transform KnockbackTransform;
    }
}
