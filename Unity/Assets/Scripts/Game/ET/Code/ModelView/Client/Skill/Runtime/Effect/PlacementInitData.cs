namespace ET.Client
{
    /// <summary>
    /// 放置物初始化数据
    /// </summary>
    public struct PlacementInitData
    {
        public float CollisionRadius;
        public GameplayTagSet CollisionTargetTags;
        public GameplayTagSet CollisionExcludeTags;
        public AbilitySystemComponent SourceASC;
    }
}
