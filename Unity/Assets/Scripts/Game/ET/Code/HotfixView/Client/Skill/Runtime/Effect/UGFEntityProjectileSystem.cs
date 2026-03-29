using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(UGFEntityProjectile))]
    [FriendOf(typeof(ProjectileEffectSpec))]
    [EntitySystemOf(typeof(UGFEntityProjectile))]
    public static partial class UGFEntityProjectileSystem
    {
        [EntitySystem]
        private static void Awake(this UGFEntityProjectile self, ProjectileInitData initData)
        {
            self.InitData = initData;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this UGFEntityProjectile self)
        {
            ProjectileEffectSpec projectileSpec = self.GetEffectSpec()?.GetComponent<ProjectileEffectSpec>();
            if (projectileSpec != null)
            {
                projectileSpec.ProjectileEntity = self;
            }

            self.SyncFromSpec();
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this UGFEntityProjectile self, bool isShutdown)
        {
            ProjectileEffectSpec projectileSpec = self.GetEffectSpec()?.GetComponent<ProjectileEffectSpec>();
            if (projectileSpec != null && projectileSpec.ProjectileEntity.As() == self)
            {
                projectileSpec.ProjectileEntity = default;
            }
        }

        [UGFEntitySystem]
        private static void UGFEntityOnUpdate(this UGFEntityProjectile self, float elapseSeconds, float realElapseSeconds)
        {
            self.SyncFromSpec();
        }

        public static void Cancel(this UGFEntityProjectile self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Dispose();
        }

        private static void SyncFromSpec(this UGFEntityProjectile self)
        {
            ProjectileEffectSpec projectileSpec = self.GetEffectSpec()?.GetComponent<ProjectileEffectSpec>();
            if (projectileSpec == null || self.CachedTransform == null)
            {
                return;
            }

            self.CachedTransform.position = projectileSpec.CurrentPosition;
            if (projectileSpec.CurrentDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(projectileSpec.CurrentDirection.y, projectileSpec.CurrentDirection.x) * Mathf.Rad2Deg;
                self.CachedTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static GameplayEffectSpec GetEffectSpec(this UGFEntityProjectile self)
        {
            return self?.GetParent<GameplayEffectSpec>();
        }
    }
}
