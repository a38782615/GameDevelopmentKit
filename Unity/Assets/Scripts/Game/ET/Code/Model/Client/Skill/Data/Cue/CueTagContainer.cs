namespace ET.Client
{
    public struct CueTagContainer
    {
        public GameplayTagSet RequiredTags;
        public GameplayTagSet ImmunityTags;

        public CueTagContainer(GameplayTagSet requiredTags, GameplayTagSet immunityTags)
        {
            this.RequiredTags = requiredTags;
            this.ImmunityTags = immunityTags;
        }
    }
}
