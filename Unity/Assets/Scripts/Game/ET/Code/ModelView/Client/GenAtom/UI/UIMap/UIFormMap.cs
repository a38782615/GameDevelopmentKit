using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormMap : UGFUIForm<MonoUIFormMap>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public int[] Maps = new int[3] { UGFUIEntityId.Map0, UGFUIEntityId.Map1, UGFUIEntityId.Map2 };
        public EntityRef<UIWidgetMap> CurrentMapWidget;
        public readonly List<long> FightUnitIds = new List<long>();
        public bool IsLoadingFightUnits;
        public bool IsSwitchingMap;
    }
}