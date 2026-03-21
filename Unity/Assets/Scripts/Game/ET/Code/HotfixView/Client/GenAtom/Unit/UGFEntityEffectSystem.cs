using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UGFEntityEffect))]
    [EntitySystemOf(typeof(UGFEntityEffect))]
    public static partial class UGFEntityEffectSystem
    {
        [EntitySystem]
        private static void Awake(this UGFEntityEffect self, UGFEntityEffectInitData initData)
        {
            self.InitData = initData;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this UGFEntityEffect self)
        {
            if (self.CachedTransform == null)
            {
                return;
            }

            Transform transform = self.CachedTransform;
            transform.SetParent(self.InitData.AttachTransform, false);
            transform.position = self.InitData.Position;
            transform.rotation = self.Rotation;
            transform.localScale = self.InitData.Scale;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this UGFEntityEffect self, bool isShutdown)
        {
            if (self.CachedTransform == null)
            {
                return;
            }

            self.CachedTransform.SetParent(null, false);
        }
    }
}
