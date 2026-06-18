using Game;
using UnityEngine;
using Unity.Mathematics;

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

        public static int GetAttackIntervalMs(this GameAIComponent self, int defaultValue = 500)
        {
            AbilitySystemComponent asc = self.GetOwnerASC();
            float attackSpeed = asc?.Attributes?.GetValue(global::ET.NumericType.AttackSpeed) ?? 0f;
            return Mathf.Max(1, Mathf.RoundToInt(attackSpeed));
        }

        public static void MarkPatrolIdle(this GameAIComponent self, DRGameAI aiConfig)
        {
            if (self == null)
            {
                return;
            }

            int idleMs = self.GetAttackIntervalMs();
            self.PatrolIdleRemainingMs = idleMs;
            self.PatrolIdleUntil = TimeInfo.Instance.ClientNow() + idleMs;
        }

        public static bool HasPendingPatrolIdle(this GameAIComponent self)
        {
            return self != null && self.PatrolIdleRemainingMs > 0;
        }

        public static int GetRemainingPatrolIdleMs(this GameAIComponent self)
        {
            return self == null ? 0 : Mathf.Max(0, self.PatrolIdleRemainingMs);
        }

        public static void ConsumePatrolIdleMs(this GameAIComponent self, int elapsedMs)
        {
            if (self == null || elapsedMs <= 0 || self.PatrolIdleRemainingMs <= 0)
            {
                return;
            }

            self.PatrolIdleRemainingMs = Mathf.Max(0, self.PatrolIdleRemainingMs - elapsedMs);
            self.PatrolIdleUntil = self.PatrolIdleRemainingMs > 0
                ? TimeInfo.Instance.ClientNow() + self.PatrolIdleRemainingMs
                : 0;
        }

        public static void ClearPatrolIdle(this GameAIComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.PatrolIdleUntil = 0;
            self.PatrolIdleRemainingMs = 0;
        }

        public static int GetPreferSkillId(this DRGameAI aiConfig)
        {
            if (aiConfig?.NodeParams == null || aiConfig.NodeParams.Count < 2)
            {
                return 0;
            }

            return aiConfig.NodeParams[1];
        }

        public static int GetNodeParam(this DRGameAI aiConfig, int index, int defaultValue)
        {
            if (aiConfig?.NodeParams == null || aiConfig.NodeParams.Count <= index)
            {
                return defaultValue;
            }

            int value = aiConfig.NodeParams[index];
            return value > 0 ? value : defaultValue;
        }

        public static float GetPatrolMinDistance(this DRGameAI aiConfig, float defaultValue = 2f)
        {
            return Mathf.Max(0.5f, aiConfig.GetNodeParam(0, Mathf.RoundToInt(defaultValue)));
        }

        public static float GetPatrolMaxDistance(this DRGameAI aiConfig, float defaultValue = 4f)
        {
            float minDistance = aiConfig.GetPatrolMinDistance();
            return Mathf.Max(minDistance, aiConfig.GetNodeParam(1, Mathf.RoundToInt(defaultValue)));
        }

        public static bool HasAIHandler(this GameAIComponent self, string nodeName)
        {
            return self.FindAIConfigByName(nodeName) != null;
        }

        public static DRGameAI FindAIConfigByName(this GameAIComponent self, string nodeName)
        {
            if (self == null || string.IsNullOrEmpty(nodeName))
            {
                return null;
            }

            if (!Tables.Instance.DTGameAI.GameAIs.TryGetValue(self.AIConfigId, out var oneAI) || oneAI == null)
            {
                return null;
            }

            foreach (DRGameAI aiNode in oneAI.Values)
            {
                if (aiNode == null)
                {
                    continue;
                }

                if (aiNode.Name == nodeName ||
                    aiNode.Name == $"GameAI_{nodeName}" ||
                    aiNode.Name == $"AI_{nodeName}")
                {
                    return aiNode;
                }
            }

            return null;
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
                if (spec?.GetSkillNumericId() > 0)
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

            int startPosIdx = NormalizeFormationPosition(selfUnit.PosIdx);
            for (int offset = 0; offset < GameConst.FormationPositionCount; offset++)
            {
                int targetPosIdx = (startPosIdx + offset) % GameConst.FormationPositionCount;
                AbilitySystemComponent target = FindHostileTargetAtPosition(unitComponent, selfUnit, targetPosIdx);
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        private static AbilitySystemComponent FindHostileTargetAtPosition(UnitComponent unitComponent, Unit selfUnit,  int targetPosIdx)
        {
            AbilitySystemComponent nearestTarget = null;

            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit otherUnit || otherUnit.Id == selfUnit.Id)
                {
                    continue;
                }

                if (!IsHostileUnit(selfUnit, otherUnit) || NormalizeFormationPosition(otherUnit.PosIdx) != targetPosIdx)
                {
                    continue;
                }

                AbilitySystemComponent targetAsc = otherUnit.GetComponent<SkillUnit>()?.ASC.As();
                if (!IsAliveTarget(targetAsc))
                {
                    continue;
                }

                nearestTarget = targetAsc;
                if (nearestTarget != null)
                {
                    break;
                }
            }

            return nearestTarget;
        }

        private static bool IsHostileUnit(Unit selfUnit, Unit otherUnit)
        {
            return (UnitType)otherUnit.Config().Type != (UnitType)selfUnit.Config().Type;
        }

        private static bool IsAliveTarget(AbilitySystemComponent targetAsc)
        {
            return targetAsc.IsAlive();
        }

        private static int NormalizeFormationPosition(int posIdx)
        {
            int normalized = posIdx % GameConst.FormationPositionCount;
            return normalized < 0 ? normalized + GameConst.FormationPositionCount : normalized;
        }

        public static Vector3 GetWorldPosition(this Unit unit)
        {
            UnityEngine.GameObject owner = unit?.GetComponent<SkillUnit>()?.ASC.As()?.GetOwnerObject();
            if (owner != null)
            {
                return owner.transform.position;
            }

            return unit == null
                ? Vector3.zero
                : new Vector3(unit.Position.x, unit.Position.y, unit.Position.z);
        }

        public static bool TryGetRandomPatrolTargetInScreen(this GameAIComponent self, DRGameAI aiConfig, out float3 target)
        {
            target = default;

            Unit unit = self?.GetOwnerUnit();
            if (unit == null)
            {
                return false;
            }

            Camera camera = GameEntry.Camera?.CurrentSceneCamera ?? Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 origin = unit.GetWorldPosition();
            float minDistance = aiConfig.GetPatrolMinDistance();
            float maxDistance = aiConfig.GetPatrolMaxDistance();
            float viewportMargin = 0.08f;
            Vector2[] directions =
            {
                Vector2.right,
                (Vector2.right + Vector2.up).normalized,
                Vector2.up,
                (Vector2.left + Vector2.up).normalized,
                Vector2.left,
                (Vector2.left + Vector2.down).normalized,
                Vector2.down,
                (Vector2.right + Vector2.down).normalized,
            };

            for (int i = 0; i < directions.Length; ++i)
            {
                int swapIndex = UnityEngine.Random.Range(i, directions.Length);
                (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
            }

            foreach (Vector2 direction in directions)
            {
                float distance = UnityEngine.Random.Range(minDistance, maxDistance);
                Vector3 candidate = origin + new Vector3(direction.x * distance, direction.y * distance, 0f);
                Vector3 viewportPoint = camera.WorldToViewportPoint(candidate);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                if (viewportPoint.x < viewportMargin || viewportPoint.x > 1f - viewportMargin ||
                    viewportPoint.y < viewportMargin || viewportPoint.y > 1f - viewportMargin)
                {
                    continue;
                }

                target = new float3(candidate.x, candidate.y, unit.Position.z);
                return true;
            }

            float depth = camera.WorldToScreenPoint(origin).z;
            if (depth <= 0f)
            {
                depth = Mathf.Abs(Vector3.Dot(origin - camera.transform.position, camera.transform.forward));
            }

            if (depth <= 0f)
            {
                depth = 10f;
            }

            Vector3 minWorld = camera.ViewportToWorldPoint(new Vector3(viewportMargin, viewportMargin, depth));
            Vector3 maxWorld = camera.ViewportToWorldPoint(new Vector3(1f - viewportMargin, 1f - viewportMargin, depth));
            Vector2 fallbackDirection = directions[UnityEngine.Random.Range(0, directions.Length)];
            float fallbackDistance = UnityEngine.Random.Range(minDistance, maxDistance);
            Vector3 fallback = origin + new Vector3(fallbackDirection.x * fallbackDistance, fallbackDirection.y * fallbackDistance, 0f);
            fallback.x = Mathf.Clamp(fallback.x, Mathf.Min(minWorld.x, maxWorld.x), Mathf.Max(minWorld.x, maxWorld.x));
            fallback.y = Mathf.Clamp(fallback.y, Mathf.Min(minWorld.y, maxWorld.y), Mathf.Max(minWorld.y, maxWorld.y));
            target = new float3(fallback.x, fallback.y, unit.Position.z);
            return true;
        }
    }
}
