using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormFight : UGFUIForm<MonoUIFormFight>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public int[] Maps = new int[3] { UGFUIEntityId.Map0, UGFUIEntityId.Map1 , UGFUIEntityId.Map2};
        public readonly List<long> FightUnitIds = new List<long>();
        public bool IsLoadingFightUnits;
        public RectTransform[] LPos;
        public RectTransform[] RPos;
    }
}
