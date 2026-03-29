using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class PlacementEffectSpec : Entity, IAwake
    {
        public EntityRef<UGFEntityPlacement> PlacementEntity;
        public bool IsLogicActive;
        public Vector3 RuntimePosition;
        public Dictionary<long, EntityRef<AbilitySystemComponent>> CurrentTargets = new Dictionary<long, EntityRef<AbilitySystemComponent>>();
    }
}
