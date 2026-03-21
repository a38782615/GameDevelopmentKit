using UnityEngine;

namespace ET.Client
{
    [ChildOf(typeof(ActiveCueComponent))]
    public class UGFEntityEffect : UGFEntity, IAwake<UGFEntityEffectInitData>, IUGFEntityOnShow, IUGFEntityOnHide
    {
        public UGFEntityEffectInitData InitData;
        public Quaternion Rotation = Quaternion.identity;
    }
}
