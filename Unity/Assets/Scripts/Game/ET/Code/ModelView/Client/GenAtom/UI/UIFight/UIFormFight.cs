using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormFight : UGFUIForm<MonoUIFormFight>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public readonly List<long> FightUnitIds = new List<long>();
        public readonly List<EntityRef<UIWidgetHeadItem>> FightHeadItems = new List<EntityRef<UIWidgetHeadItem>>();
        public bool IsLoadingFightUnits;
    }
}
