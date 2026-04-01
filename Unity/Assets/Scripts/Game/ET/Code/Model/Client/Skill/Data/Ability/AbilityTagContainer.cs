namespace ET.Client
{
    public struct AbilityTagContainer
    {
        public GameplayTagSet AssetTags;
        public GameplayTagSet CancelAbilitiesWithTags;
        public GameplayTagSet BlockAbilitiesWithTags;
        public GameplayTagSet ActivationOwnedTags;
        public GameplayTagSet ActivationRequiredTags;
        public GameplayTagSet ActivationBlockedTags;
        public GameplayTagSet OngoingBlockedTags;

        public AbilityTagContainer(
            GameplayTagSet assetTags,
            GameplayTagSet cancelAbilityTags,
            GameplayTagSet blockAbilityTags,
            GameplayTagSet activationOwnedTags,
            GameplayTagSet activationRequiredTags,
            GameplayTagSet activationBlockedTags,
            GameplayTagSet ongoingBlockedTags)
        {
            this.AssetTags = assetTags;
            this.CancelAbilitiesWithTags = cancelAbilityTags;
            this.BlockAbilitiesWithTags = blockAbilityTags;
            this.ActivationOwnedTags = activationOwnedTags;
            this.ActivationRequiredTags = activationRequiredTags;
            this.ActivationBlockedTags = activationBlockedTags;
            this.OngoingBlockedTags = ongoingBlockedTags;
        }
    }
}
