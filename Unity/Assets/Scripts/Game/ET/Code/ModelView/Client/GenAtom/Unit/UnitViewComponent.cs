namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class UnitViewComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<global::ET.UGFEntity> ViewEntity;
    }
}
