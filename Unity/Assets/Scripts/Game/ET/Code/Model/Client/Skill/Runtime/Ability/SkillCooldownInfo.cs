namespace ET.Client
{
    /// <summary>
    /// 技能冷却信息（统一接口，支持普通CD和充能CD）
    /// </summary>
    [EnableClass]
    public class SkillCooldownInfo : Object
    {
        /// <summary>
        /// 是否在CD中
        /// </summary>
        public bool IsOnCooldown;

        /// <summary>
        /// 是否是充能CD
        /// </summary>
        public bool IsChargeCooldown;

        // ============ 普通CD ============

        /// <summary>
        /// 剩余CD时间
        /// </summary>
        public float RemainingTime;

        /// <summary>
        /// 总CD时间
        /// </summary>
        public float TotalDuration;

        // ============ 充能CD ============

        /// <summary>
        /// 当前充能数
        /// </summary>
        public int CurrentCharges;

        /// <summary>
        /// 最大充能数
        /// </summary>
        public int MaxCharges;

        /// <summary>
        /// 下一层充能进度 (0-1)
        /// </summary>
        public float ChargeProgress;

        /// <summary>
        /// 下一层充能剩余时间
        /// </summary>
        public float ChargeTimeRemaining;
    }
}
