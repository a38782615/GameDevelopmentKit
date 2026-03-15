
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗属性数值组件，在这里管理角色所有战斗属性数值的存储、变更、刷新等
    /// </summary>
    public partial class AttributeComponent : Entity, IAwake, IDestroy
    {
        public float Hp => NumericComponent.GetAsFloat(NumericType.Hp.GetHashCode());
        public float MaxHp => NumericComponent.GetAsFloat(NumericType.MaxHp.GetHashCode());
        public float Mode => NumericComponent.GetAsFloat(NumericType.Mode.GetHashCode());
        public float ModeMax => NumericComponent.GetAsFloat(NumericType.ModeMax.GetHashCode());
        public float Attack => NumericComponent.GetAsFloat(NumericType.Attack.GetHashCode());
        public float Armor => NumericComponent.GetAsFloat(NumericType.Armor.GetHashCode());
        public float CriticalProbability => NumericComponent.GetAsFloat(NumericType.CriticalProbability.GetHashCode());

        public NumericComponent NumericComponent => this.GetParent<Unit>().GetComponent<NumericComponent>();
        /// <summary>
        /// 所有的数据修改器
        /// Key为分组名称，其中如果和NumericComponent有联系，则必须使用NumericType对应String作为Key，例如NumericType.HP对应String就是HP
        /// Value为此装饰器分组中所有的装饰器
        /// </summary>
        public XList<DataModifier> AllModifiers;
        public int DataId;

        // public EquipComponent EquipComponent => this.GetParent<Unit>().GetComponent<EquipComponent>();
    }
}
