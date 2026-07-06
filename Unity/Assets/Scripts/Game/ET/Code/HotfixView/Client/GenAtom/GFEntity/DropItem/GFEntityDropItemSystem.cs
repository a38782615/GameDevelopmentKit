using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GFEntityDropItem))]
    [EntitySystemOf(typeof(GFEntityDropItem))]
    public static partial class GFEntityDropItemSystem
    {
        [EntitySystem]
        private static void Awake(this GFEntityDropItem self, Vector3 position)
        {
            self.Position = position;
        }

        [EntitySystem]
        private static void Destroy(this GFEntityDropItem self)
        {
            self.Position = default;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this GFEntityDropItem self)
        {
            if (self.CachedTransform == null)
            {
                return;
            }

            self.CachedTransform.position = self.Position;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this GFEntityDropItem self, bool isShutdown)
        {
        }
    }
}
