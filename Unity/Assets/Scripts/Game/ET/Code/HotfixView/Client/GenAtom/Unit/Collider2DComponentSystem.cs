using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(Collider2DComponent))]
    [FriendOf(typeof(Collider2DComponent))]
    public static partial class Collider2DComponentSystem
    {
        [EntitySystem]
        private static void Awake(this Collider2DComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this Collider2DComponent self)
        {
            Collider2DRegistry.UnregisterAll(self);
            self.OwnerASC = default;
        }

        public static void Bind(this Collider2DComponent self, GameObject gameObject, AbilitySystemComponent ownerAsc)
        {
            self.OwnerASC = ownerAsc;
            Collider2DRegistry.UnregisterAll(self);

            if (gameObject == null)
            {
                return;
            }

            Collider2D[] colliders = gameObject.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D collider in colliders)
            {
                Collider2DRegistry.Register(self, collider);
            }
        }
    }
}
