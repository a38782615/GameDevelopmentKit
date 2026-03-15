using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormUnitAttributeComponent : UGFUIForm<MonoUIFormUnitAttribute>, IAwake, IUGFUIFormOnOpen, IUGFUIFormOnClose, IUGFUIFormOnUpdate
    {
        public readonly List<AttrType> OrderedAttrTypes = new List<AttrType>();
        public readonly List<MonoUIUnitAttributeRow> PlayerRows = new List<MonoUIUnitAttributeRow>();
        public readonly List<MonoUIUnitAttributeRow> MonsterRows = new List<MonoUIUnitAttributeRow>();
        public float RefreshLeftTime;
        public bool LayoutBuilt;
    }
}
