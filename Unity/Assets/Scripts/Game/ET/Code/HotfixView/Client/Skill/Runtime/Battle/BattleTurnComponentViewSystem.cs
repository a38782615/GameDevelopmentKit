namespace ET.Client
{
    [FriendOf(typeof(BattleTurnComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static class BattleTurnComponentViewSystem
    {
        public static void RegisterAttack(this BattleTurnComponent self, GameplayAbilitySpec spec)
        {
            if (self == null || spec == null || spec.IsDisposed)
            {
                return;
            }

            if (!self.ActiveAttackSpecs.Add(spec.InstanceId))
            {
                return;
            }

            SkillUnit skillUnit = spec.GetASC?.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            SkillDiagFileLogger.Log($"[BattleTurn] AttackBegin skillId={spec.SkillId} spec={spec.InstanceId} unit={unit?.Id ?? 0} activeCount={self.ActiveAttackSpecs.Count}");
        }

        public static void UnregisterAttack(this BattleTurnComponent self, GameplayAbilitySpec spec)
        {
            if (self == null || spec == null)
            {
                return;
            }

            if (!self.ActiveAttackSpecs.Remove(spec.InstanceId))
            {
                return;
            }

            SkillUnit skillUnit = spec.GetASC?.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            SkillDiagFileLogger.Log($"[BattleTurn] AttackEnd skillId={spec.SkillId} spec={spec.InstanceId} unit={unit?.Id ?? 0} activeCount={self.ActiveAttackSpecs.Count}");
        }
    }
}
