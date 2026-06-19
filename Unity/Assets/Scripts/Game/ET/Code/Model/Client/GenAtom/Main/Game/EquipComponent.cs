using System.Collections.Generic;

namespace ET
{
    public partial class EquipComponent : Entity, IAwake, IDestroy
    {
        public XDictionary<int, List<DataModifier>> EquipModifiers;
        public XList<DataModifier> All;
        public int DataId;
    }
}