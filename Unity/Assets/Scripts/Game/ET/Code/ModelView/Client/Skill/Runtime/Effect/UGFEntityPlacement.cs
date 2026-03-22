using System.Collections.Generic;

namespace ET.Client
{
    [ChildOf(typeof(GameplayEffectSpec))]
    public class UGFEntityPlacement : UGFEntity, IAwake<PlacementInitData>, IUGFEntityOnShow, IUGFEntityOnUpdate, IUGFEntityOnHide
    {
        public PlacementInitData InitData;
        public bool Initialized;
        public bool DestroyRequested;
        public Dictionary<long, EntityRef<AbilitySystemComponent>> CurrentTargets = new Dictionary<long, EntityRef<AbilitySystemComponent>>();
    }
}
