using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class PlacementEffectSpec : Entity, IAwake
    {
        public EntityRef<UGFEntityPlacement> PlacementEntity;
        public GameObject PlacementObject;
    }
}
