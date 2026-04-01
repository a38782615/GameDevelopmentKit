using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(AbilitySystemComponent))]
    public class AbilityViewComponent : Entity, IAwake, IDestroy
    {
        public GameObject Owner;
    }
}
