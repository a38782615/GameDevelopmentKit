namespace ET.Client
{
    /// <summary>
    /// SkillUnit - 战斗单位的 MonoBehaviour 桥接
    /// 现在通过 ET 的 Unit Entity 创建 AbilitySystemComponent
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [ComponentOf(typeof(Unit))]
    public partial class SkillUnit : Entity, IAwake
    {
        public EntityRef<Unit> Unit => this.GetParent<Unit>();
        public EntityRef<AbilitySystemComponent> ASC => GetComponent<AbilitySystemComponent>();
        public EntityRef<SkillCardDeckComponent> SkillCardDeck => GetComponent<SkillCardDeckComponent>();
        public EntityRef<RelicContainerComponent> RelicContainer => GetComponent<RelicContainerComponent>();

        public AbilitySystemComponent ownerASC => this.ASC.As();
    }
}
