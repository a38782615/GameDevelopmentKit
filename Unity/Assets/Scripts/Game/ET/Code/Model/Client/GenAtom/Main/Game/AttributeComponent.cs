
namespace ET
{
    /// <summary>
    /// 战斗属性数值组件，在这里管理角色所有战斗属性数值的存储、变更、刷新等
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public partial class AttributeComponent : Entity, IAwake<int,int,int>, IDestroy
    {
        public int ConfigId;
        public int Level;
        public int SubLevel;
        public int MaxAge => NumericComponent.GetAsInt(NumericType.MaxAge);

        public NumericComponent NumericComponent => this.GetParent<Unit>().GetComponent<NumericComponent>();
    }
}
