using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(PlacementEffectSpec))]
    [FriendOf(typeof(UGFEntityPlacement))]
    [EntitySystemOf(typeof(UGFEntityPlacement))]
    public static partial class PlacementEntitySystem
    {
        [EntitySystem]
        private static void Awake(this UGFEntityPlacement self, PlacementInitData initData)
        {
            self.InitData = initData;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this UGFEntityPlacement self)
        {
            self.InitializeRuntimeState();

            PlacementEffectSpec placementSpec = self.GetPlacementSpec();
            if (placementSpec == null)
            {
                return;
            }

            GameObject placementObject = self.GetPlacementObject();
            placementSpec.PlacementEntity = self;
            placementSpec.PlacementObject = placementObject;
            self.GetEffectSpec()?.GetContext()?.SetPlacementObject(placementObject);
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this UGFEntityPlacement self, bool isShutdown)
        {
            GameObject placementObject = self.GetPlacementObject();

            self.TriggerExitForCurrentTargets();
            self.Initialized = false;
            self.DestroyRequested = true;
            self.CurrentTargets.Clear();

            PlacementEffectSpec placementSpec = self.GetPlacementSpec();
            if (placementSpec != null)
            {
                if (placementSpec.PlacementEntity.As() == self)
                {
                    placementSpec.PlacementEntity = default;
                }

                if (placementSpec.PlacementObject == placementObject)
                {
                    placementSpec.PlacementObject = null;
                }
            }

            SpecExecutionContext context = self.GetEffectSpec()?.GetContext();
            if (context != null && context.GetPlacementObject() == placementObject)
            {
                context.SetPlacementObject(null);
            }
        }

        [UGFEntitySystem]
        private static void UGFEntityOnUpdate(this UGFEntityPlacement self, float elapseSeconds, float realElapseSeconds)
        {
            if (!self.CanContinue() || !self.InitData.EnableCollision)
            {
                return;
            }

            self.CheckCollision();
        }

        public static void Cancel(this UGFEntityPlacement self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Initialized = false;
            self.DestroyRequested = true;
            self.Dispose();
        }

        private static void InitializeRuntimeState(this UGFEntityPlacement self)
        {
            self.Initialized = true;
            self.DestroyRequested = false;
            self.CurrentTargets.Clear();

            if (self.CachedTransform != null)
            {
                self.CachedTransform.position = self.InitData.Position;
            }
        }

        private static bool CanContinue(this UGFEntityPlacement self)
        {
            return self != null && !self.IsDisposed && self.Initialized && !self.DestroyRequested;
        }

        private static void CheckCollision(this UGFEntityPlacement self)
        {
            if (self.CachedTransform == null)
            {
                return;
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.CachedTransform.position, self.InitData.CollisionRadius);
            HashSet<long> targetsInRange = new HashSet<long>();

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = Collider2DRegistry.GetASC(collider);
                if (!self.IsValidTarget(asc))
                {
                    continue;
                }

                if (asc == self.InitData.SourceASC.As())
                {
                    continue;
                }

                long targetId = asc.InstanceId;
                targetsInRange.Add(targetId);
                if (self.CurrentTargets.ContainsKey(targetId))
                {
                    continue;
                }

                self.CurrentTargets[targetId] = asc;
                self.TriggerEnter(asc);
                if (!self.CanContinue())
                {
                    return;
                }
            }

            List<long> exitedTargetIds = new List<long>();
            foreach ((long targetId, EntityRef<AbilitySystemComponent> _) in self.CurrentTargets)
            {
                if (!targetsInRange.Contains(targetId))
                {
                    exitedTargetIds.Add(targetId);
                }
            }

            for (int i = 0; i < exitedTargetIds.Count; i++)
            {
                long targetId = exitedTargetIds[i];
                AbilitySystemComponent target = self.CurrentTargets[targetId].As();
                self.CurrentTargets.Remove(targetId);
                self.TriggerExit(target);
                if (!self.CanContinue())
                {
                    return;
                }
            }
        }

        private static bool IsValidTarget(this UGFEntityPlacement self, AbilitySystemComponent target)
        {
            if (target == null || target.IsDisposed || target.Owner == null)
            {
                return false;
            }

            if (!self.InitData.CollisionTargetTags.IsEmpty && !target.OwnedTags.HasAnyTags(self.InitData.CollisionTargetTags))
            {
                return false;
            }

            if (!self.InitData.CollisionExcludeTags.IsEmpty && target.OwnedTags.HasAnyTags(self.InitData.CollisionExcludeTags))
            {
                return false;
            }

            return true;
        }

        private static void TriggerExitForCurrentTargets(this UGFEntityPlacement self)
        {
            List<AbilitySystemComponent> currentTargets = new List<AbilitySystemComponent>(self.CurrentTargets.Count);
            foreach (EntityRef<AbilitySystemComponent> targetRef in self.CurrentTargets.Values)
            {
                AbilitySystemComponent target = targetRef.As();
                if (target != null)
                {
                    currentTargets.Add(target);
                }
            }

            self.CurrentTargets.Clear();

            for (int i = 0; i < currentTargets.Count; i++)
            {
                self.TriggerExit(currentTargets[i]);
            }
        }

        private static void TriggerEnter(this UGFEntityPlacement self, AbilitySystemComponent target)
        {
            self.ExecuteFlow(target, SkillPortId.PlacementEffect.OnEnter);
        }

        private static void TriggerExit(this UGFEntityPlacement self, AbilitySystemComponent target)
        {
            self.ExecuteFlow(target, SkillPortId.PlacementEffect.OnExit);
        }

        private static void ExecuteFlow(this UGFEntityPlacement self, AbilitySystemComponent target, int outputPortId)
        {
            if (target == null || self == null || self.IsDisposed)
            {
                return;
            }

            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            PlacementEffectSpec placementSpec = self.GetPlacementSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || placementSpec == null || context == null || effectSpec.IsDisposed)
            {
                return;
            }

            SpecExecutionContext childContext = context.CreateWithParentInput(target);
            if (childContext == null)
            {
                return;
            }

            try
            {
                childContext.SetPlacementObject(self.GetPlacementObject());
                childContext.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, outputPortId);
            }
            finally
            {
                childContext.Dispose();
            }
        }

        private static GameplayEffectSpec GetEffectSpec(this UGFEntityPlacement self)
        {
            return self?.GetParent<GameplayEffectSpec>();
        }

        private static PlacementEffectSpec GetPlacementSpec(this UGFEntityPlacement self)
        {
            return self.GetEffectSpec()?.GetComponent<PlacementEffectSpec>();
        }

        private static GameObject GetPlacementObject(this UGFEntityPlacement self)
        {
            return self?.CachedTransform != null ? self.CachedTransform.gameObject : null;
        }
    }
}
