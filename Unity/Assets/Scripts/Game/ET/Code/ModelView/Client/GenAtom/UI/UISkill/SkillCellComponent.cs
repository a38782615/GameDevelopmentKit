namespace ET.Client
{
    [ComponentOf(typeof(UIFormSkillComponent))]
    public class SkillCellComponent : UGFUIWidget<MonoUISkillItem>, IAwake, IUGFUIWidgetOnOpen, IUGFUIWidgetOnUpdate, IUGFUIWidgetOnClose
    {
        public EntityRef<GameplayAbilitySpec> Spec;
        public float StateRefreshLeftTime;
        public bool StateInitialized;
        public bool CachedCanCast;
        public string CachedStateText;
        public string CachedIconPath;
    }
}
