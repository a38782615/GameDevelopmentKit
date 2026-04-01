namespace ET.Client
{
    /// <summary>
    /// 鍐峰嵈鏁堟灉Spec
    /// 鏀寔鏅€欳D鍜屽厖鑳紺D涓ょ妯″紡
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class CooldownEffectSpec : Entity, IAwake
    {
        // ============ 鍏呰兘CD鐘舵€?============

        /// <summary>
        /// 褰撳墠鍏呰兘鏁?
        /// </summary>
        public int CurrentCharges;

        /// <summary>
        /// 鏈€澶у厖鑳芥暟
        /// </summary>
        public int MaxCharges;

        /// <summary>
        /// 姣忓眰鍏呰兘鏃堕棿
        /// </summary>
        public float ChargeTime;

        /// <summary>
        /// 褰撳墠鍏呰兘璁℃椂鍣?
        /// </summary>
        public float ChargeTimer;
    }
}
