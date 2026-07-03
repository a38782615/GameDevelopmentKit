using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormFight : UGFUIForm<MonoUIFormFight>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
    }
}
