namespace ET.Client
{
    [ComponentOf(typeof(UIFormSkillComponent))]
    public class SkillCellComponent : UGFUIWidget<MonoUISkillItem>, IAwake, IUGFUIWidgetOnOpen, IUGFUIWidgetOnUpdate, IUGFUIWidgetOnClose
    {
        public EntityRef<SkillCardRuntime> Card;
        public float StateRefreshLeftTime;
        public bool StateInitialized;
        public bool CachedCanCast;
        public bool CachedCooldownVisible;
        public float CachedCooldownFillAmount;
        public string CachedStateText;
        public string CachedIconPath;
    }
}
