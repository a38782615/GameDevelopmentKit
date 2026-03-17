
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 战斗属性数值组件，在这里管理角色所有战斗属性数值的存储、变更、刷新等
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public partial class AttributeComponent : Entity, IAwake, IDestroy
    {
        public int Level;
        public float Hp => NumericComponent.GetAsFloat(NumericType.Hp);
        public float MaxHp => NumericComponent.GetAsFloat(NumericType.MaxHp);
        public float Mode => NumericComponent.GetAsFloat(NumericType.Mode);
        public float ModeMax => NumericComponent.GetAsFloat(NumericType.ModeMax);
        public float Attack => NumericComponent.GetAsFloat(NumericType.Attack);
        public float Armor => NumericComponent.GetAsFloat(NumericType.Armor);
        public float CriticalProbability => NumericComponent.GetAsFloat(NumericType.CriticalProbability);

        public NumericComponent NumericComponent => this.GetParent<Unit>().GetComponent<NumericComponent>();
        public XList<DataModifier> AllModifiers;
        public int DataId;

        // public EquipComponent EquipComponent => this.GetParent<Unit>().GetComponent<EquipComponent>();
    }
}
