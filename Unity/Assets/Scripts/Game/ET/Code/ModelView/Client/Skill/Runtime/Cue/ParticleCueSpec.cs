
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 粒子特效Cue Spec
    /// 播放粒子特效
    /// </summary>
    [ComponentOf(typeof(GameplayCueSpec))]
    public partial class ParticleCueSpec : Entity, IAwake
    {
        // ============ 动态数据 ============

        /// <summary>
        /// 粒子预制体
        /// </summary>
        public GameObject ParticlePrefab { get; set; }

        /// <summary>
        /// 位置来源类型
        /// </summary>
        public PositionSourceType PositionSource { get; set; }

        /// <summary>
        /// 绑定点名称
        /// </summary>
        public string ParticleBindingName { get; set; }

        /// <summary>
        /// 位置偏移
        /// </summary>
        public Vector3 ParticleOffset { get; set; }

        /// <summary>
        /// 缩放
        /// </summary>
        public Vector3 ParticleScale { get; set; }

        /// <summary>
        /// 是否附着到目标
        /// </summary>
        public bool AttachToTarget { get; set; }

        /// <summary>
        /// 是否循环
        /// </summary>
        public bool ParticleLoop { get; set; }
    }
}
