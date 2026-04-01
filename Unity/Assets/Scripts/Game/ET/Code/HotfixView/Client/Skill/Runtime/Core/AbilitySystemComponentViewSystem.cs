namespace ET.Client
{
    [FriendOf(typeof(AbilityViewComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static class AbilitySystemComponentViewSystem
    {
        public static UnityEngine.GameObject GetOwnerObject(this AbilitySystemComponent self)
        {
            return self?.GetComponent<AbilityViewComponent>()?.Owner;
        }

        public static UnityEngine.Transform GetOwnerTransform(this AbilitySystemComponent self)
        {
            return self.GetOwnerObject()?.transform;
        }

        public static void SetOwnerObject(this AbilitySystemComponent self, UnityEngine.GameObject owner)
        {
            if (self == null)
            {
                return;
            }

            AbilityViewComponent viewComponent = self.GetComponent<AbilityViewComponent>();
            if (viewComponent == null)
            {
                viewComponent = self.AddComponent<AbilityViewComponent>();
            }

            viewComponent.Owner = owner;
        }

        public static void ClearOwnerObject(this AbilitySystemComponent self)
        {
            AbilityViewComponent viewComponent = self?.GetComponent<AbilityViewComponent>();
            if (viewComponent != null)
            {
                viewComponent.Owner = null;
            }
        }
    }
}
