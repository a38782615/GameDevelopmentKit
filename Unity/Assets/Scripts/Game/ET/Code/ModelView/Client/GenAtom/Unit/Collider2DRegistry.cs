using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(Collider2DComponent))]
    public static class Collider2DRegistry
    {
        [StaticField]
        private static readonly Dictionary<int, EntityRef<Collider2DComponent>> ColliderMap = new();

        public static Collider2DComponent Get(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            return ColliderMap.TryGetValue(collider.GetInstanceID(), out EntityRef<Collider2DComponent> component)
                ? component.As()
                : null;
        }

        public static AbilitySystemComponent GetASC(Collider2D collider)
        {
            return Get(collider)?.OwnerASC.As();
        }

        public static void Register(Collider2DComponent component, Collider2D collider)
        {
            if (component == null || collider == null)
            {
                return;
            }

            int instanceId = collider.GetInstanceID();
            if (ColliderMap.TryGetValue(instanceId, out EntityRef<Collider2DComponent> current) && current.As() == component)
            {
                return;
            }

            ColliderMap[instanceId] = component;
            component.ColliderInstanceIds.Add(instanceId);
        }

        public static void UnregisterAll(Collider2DComponent component)
        {
            if (component == null)
            {
                return;
            }

            foreach (int instanceId in component.ColliderInstanceIds)
            {
                if (ColliderMap.TryGetValue(instanceId, out EntityRef<Collider2DComponent> current) && current.As() == component)
                {
                    ColliderMap.Remove(instanceId);
                }
            }

            component.ColliderInstanceIds.Clear();
        }
    }
}
