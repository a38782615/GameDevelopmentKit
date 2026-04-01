namespace ET.Client
{
    [FriendOf(typeof(CooldownEffectSpec))]
    [FriendOf(typeof(GameplayEffectSpec))]
    public static class CooldownEffectSpecSystem
    {
        public static CooldownEffectNodeData GetCooldownNodeData(this CooldownEffectSpec self)
        {
            return self?.GetParent<GameplayEffectSpec>()?.GetNodeData() as CooldownEffectNodeData;
        }

        public static bool IsChargeCooldown(this CooldownEffectSpec self)
        {
            return self.GetCooldownNodeData()?.cooldownType == CooldownType.Charge;
        }

        public static bool IsCharging(this CooldownEffectSpec self)
        {
            return self.IsChargeCooldown() && self.CurrentCharges < self.MaxCharges;
        }

        public static float GetChargeProgress(this CooldownEffectSpec self)
        {
            return self != null && self.ChargeTime > 0f ? 1f - (self.ChargeTimer / self.ChargeTime) : 1f;
        }
    }
}
