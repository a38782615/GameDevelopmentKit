namespace ET.Client
{
    [ChildOf(typeof(UIFormSkillComponent))]
    public class SkillCellComponent : Entity, IAwake<MonoUISkillItem>, IUpdate, IDestroy
    {
        public MonoUISkillItem View;
        public EntityRef<GameplayAbilitySpec> Spec;
        public float StateRefreshLeftTime;
        public bool StateInitialized;
        public bool CachedCanCast;
        public string CachedStateText;
        public EntityRef<UIFormSkillComponent> Owner => this.GetParent<UIFormSkillComponent>();
    }
}
