using UnityEngine;

namespace ET.Client
{
    [ChildOf(typeof(GameplayCueManager))]
    public class UGFEntityEffect : UGFEntity, IAwake<UGFEntityEffectInitData>, IUGFEntityOnShow, IUGFEntityOnHide
    {
        public UGFEntityEffectInitData InitData;
        public Quaternion Rotation = Quaternion.identity;
    }
}
