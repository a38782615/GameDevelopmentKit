using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormFight : UGFUIForm<MonoUIFormFight>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public int CurrentMap = 0;
        public int[] Maps = new int[2] { UGFUIEntityId.Map0, UGFUIEntityId.Map1 };
        public readonly List<long> FightUnitIds = new List<long>();
        public bool IsLoadingFightUnits;
        public RectTransform[] LPos;
        public RectTransform[] RPos;
    }
}
