using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class Collider2DComponent: Entity, IAwake, IDestroy
    {
        public EntityRef<AbilitySystemComponent> OwnerASC;
        public List<int> ColliderInstanceIds = new();
    }
}
