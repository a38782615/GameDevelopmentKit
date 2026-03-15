using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameAIComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static partial class GameAIComponentSystem
    {
        public static Unit GetOwnerUnit(this GameAIComponent self)
        {
            return self.GetParent<Unit>();
        }

        public static AbilitySystemComponent GetOwnerASC(this GameAIComponent self)
        {
            return self.GetOwnerUnit()?.GetComponent<SkillUnit>()?.ASC.As();
        }

        public static float GetAttackRange(this DRGameAI aiConfig, float defaultValue = 2.5f)
        {
            if (aiConfig?.NodeParams == null || aiConfig.NodeParams.Count == 0)
            {
                return defaultValue;
            }

            return aiConfig.NodeParams[0] > 0 ? aiConfig.NodeParams[0] : defaultValue;
        }

        public static int GetPreferSkillId(this DRGameAI aiConfig)
        {
            if (aiConfig?.NodeParams == null || aiConfig.NodeParams.Count < 2)
            {
                return 0;
            }

            return aiConfig.NodeParams[1];
        }

        public static GameplayAbilitySpec FindPreferredAbility(this GameAIComponent self, DRGameAI aiConfig)
        {
            AbilitySystemComponent asc = self.GetOwnerASC();
            AbilityContainerComponent abilities = asc?.Abilities;
            if (abilities == null)
            {
                return null;
            }

            int preferSkillId = aiConfig.GetPreferSkillId();
            if (preferSkillId > 0)
            {
                GameplayAbilitySpec preferredAbility = abilities.FindAbilityById(preferSkillId);
                if (preferredAbility != null)
                {
                    return preferredAbility;
                }
            }

            foreach (EntityRef<GameplayAbilitySpec> abilityRef in abilities.GetGrantedAbilities())
            {
                GameplayAbilitySpec spec = abilityRef.As();
                if (spec?.AbilityNodeData?.skillId > 0)
                {
                    return spec;
                }
            }

            return null;
        }

        public static AbilitySystemComponent FindNearestTarget(this GameAIComponent self, float maxDistance)
        {
            Unit selfUnit = self.GetOwnerUnit();
            if (selfUnit == null)
            {
                return null;
            }

            Scene currentScene = self.Root()?.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent?.Children == null)
            {
                return null;
            }

            Vector3 selfPosition = selfUnit.GetWorldPosition();
            AbilitySystemComponent nearestTarget = null;
            float nearestDistanceSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;

            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit otherUnit || otherUnit.Id == selfUnit.Id)
                {
                    continue;
                }

                if ((UnitType)otherUnit.Config().Type == (UnitType)selfUnit.Config().Type)
                {
                    continue;
                }

                AbilitySystemComponent targetAsc = otherUnit.GetComponent<SkillUnit>()?.ASC.As();
                if (targetAsc == null)
                {
                    continue;
                }

                float? health = targetAsc.Attributes?.GetCurrentValue(AttrType.Health);
                if (health.HasValue && health.Value <= 0f)
                {
                    continue;
                }

                Vector3 targetPosition = otherUnit.GetWorldPosition();
                float deltaX = targetPosition.x - selfPosition.x;
                float deltaY = targetPosition.y - selfPosition.y;
                float deltaZ = targetPosition.z - selfPosition.z;
                float distanceSqr = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
                if (distanceSqr > nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestTarget = targetAsc;
            }

            return nearestTarget;
        }

        public static Vector3 GetWorldPosition(this Unit unit)
        {
            UnityEngine.GameObject owner = unit?.GetComponent<SkillUnit>()?.ASC.As()?.Owner;
            if (owner != null)
            {
                return owner.transform.position;
            }

            return unit == null
                ? Vector3.zero
                : new Vector3(unit.Position.x, unit.Position.y, unit.Position.z);
        }
    }
}
