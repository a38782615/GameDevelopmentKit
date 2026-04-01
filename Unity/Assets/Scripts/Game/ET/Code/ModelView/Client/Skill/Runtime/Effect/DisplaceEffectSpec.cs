
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 位移效果Spec - 持续移动目标位置（吸引/击退/吸引到指定点）
    /// 利用基类的 Duration/Tick 机制实现逐帧位移
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public partial class DisplaceEffectSpec : Entity, IAwake
    {
        // 位移运行时状态
        public float3 _displaceDirection;
        public float3 _targetPoint;
        public float3 _startPosition;
        public float _movedDistance;
        public EntityRef<AbilitySystemComponent> _targetAbility;
    }
}
