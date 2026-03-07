
using UnityEngine;

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
        public Vector3 _displaceDirection;
        public Vector3 _targetPoint;
        public Vector3 _startPosition;
        public float _movedDistance;
        public Transform _targetTransform;
    }
}
