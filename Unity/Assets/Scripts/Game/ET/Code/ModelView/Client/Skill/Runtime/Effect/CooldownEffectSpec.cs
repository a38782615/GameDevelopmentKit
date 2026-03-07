namespace ET.Client
{
    /// <summary>
    /// 冷却效果Spec
    /// 支持普通CD和充能CD两种模式
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class CooldownEffectSpec : Entity, IAwake
    {
        public CooldownEffectNodeData CooldownNodeData => GetParent<GameplayEffectSpec>().NodeData as CooldownEffectNodeData;

        // ============ 充能CD状态 ============

        /// <summary>
        /// 当前充能数
        /// </summary>
        public int CurrentCharges;

        /// <summary>
        /// 最大充能数
        /// </summary>
        public int MaxCharges;

        /// <summary>
        /// 每层充能时间
        /// </summary>
        public float ChargeTime;

        /// <summary>
        /// 当前充能计时器
        /// </summary>
        public float ChargeTimer;

        /// <summary>
        /// 是否是充能CD
        /// </summary>
        public bool IsChargeCooldown => CooldownNodeData?.cooldownType == CooldownType.Charge;

        /// <summary>
        /// 是否正在充能
        /// </summary>
        public bool IsCharging => IsChargeCooldown && CurrentCharges < MaxCharges;

        /// <summary>
        /// 充能进度 (0-1)
        /// </summary>
        public float ChargeProgress => ChargeTime > 0 ? 1f - (ChargeTimer / ChargeTime) : 1f;
    }
}
