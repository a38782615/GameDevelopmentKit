using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace ET.Client.Editor
{
    public static class SkillRuntimeDebugMenu
    {
        private enum MoveCastState
        {
            Idle,
            Moving,
            WaitingDamage,
        }

        private const string TriggerMoveAndCast1001MenuPath = "SkillEditor/Runtime/Move To Monster And Cast Skill 1001";
        private const string TriggerMoveAndCast1002MenuPath = "SkillEditor/Runtime/Move To Monster And Cast Skill 1002";
        private const string TriggerSkill7001MenuPath = "SkillEditor/Runtime/Trigger Skill 7001";
        private const string TriggerSkill1008MenuPath = "SkillEditor/Runtime/Trigger Skill 1008";
        private const string TriggerSkill1010MenuPath = "SkillEditor/Runtime/Trigger Skill 1010";

        private static MoveCastState moveCastState;
        private static Unit moveCastPlayerUnit;
        private static Unit moveCastMonsterUnit;
        private static AbilitySystemComponent moveCastPlayerAsc;
        private static AbilitySystemComponent moveCastMonsterAsc;
        private static GameplayAbilitySpec moveCastSpec;
        private static float3 moveCastStartPosition;
        private static float3 moveCastTargetPosition;
        private static double moveCastStartTime;
        private static double moveCastMoveDuration;
        private static double moveCastWaitStartTime;
        private static float moveCastHealthBefore;
        private static bool moveCastSkillTriggered;
        private static int moveCastSkillId;
        private static double moveCastWaitDuration;

        [MenuItem(TriggerMoveAndCast1001MenuPath)]
        public static void TriggerMoveAndCastSkill1001()
        {
            TriggerMoveAndCastSkill(1001, 1.2d);
        }

        [MenuItem(TriggerMoveAndCast1002MenuPath)]
        public static void TriggerMoveAndCastSkill1002()
        {
            TriggerMoveAndCastSkill(1002, 3.6d);
        }

        private static void TriggerMoveAndCastSkill(int skillId, double waitDuration)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Play Mode required. move-and-cast skillId={skillId}");
                return;
            }

            Scene currentScene = GetCurrentClientScene();
            if (currentScene == null)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Current scene not found. move-and-cast skillId={skillId}");
                return;
            }

            Unit playerUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            AbilitySystemComponent playerAsc = playerUnit?.GetComponent<SkillUnit>()?.ASC.As();
            GameplayAbilitySpec spec = playerAsc?.Abilities?.FindAbilityById(skillId);
            Unit monsterUnit = FindNearestMonster(currentScene, playerUnit);
            AbilitySystemComponent monsterAsc = monsterUnit?.GetComponent<SkillUnit>()?.ASC.As();
            if (playerUnit == null || playerAsc == null || spec == null || monsterUnit == null || monsterAsc == null)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Move-and-cast setup failed. skillId={skillId}");
                return;
            }

            CleanupMoveCastState();

            moveCastPlayerUnit = playerUnit;
            moveCastMonsterUnit = monsterUnit;
            moveCastPlayerAsc = playerAsc;
            moveCastMonsterAsc = monsterAsc;
            moveCastSpec = spec;
            moveCastSkillId = skillId;
            moveCastWaitDuration = waitDuration;
            moveCastStartPosition = GetUnitWorldPosition(playerUnit);
            moveCastTargetPosition = CalculateCastPosition(playerUnit, monsterUnit, 1.5f);
            moveCastHealthBefore = monsterAsc.Attributes?.GetCurrentValue(global::ET.NumericType.Hp) ?? -1f;

            float speed = playerUnit.GetComponent<NumericComponent>()?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed <= 0.01f)
            {
                speed = 6f;
            }

            float moveDistance = math.distance(moveCastStartPosition, moveCastTargetPosition);
            moveCastMoveDuration = moveDistance <= 0.01f ? 0.05d : moveDistance / speed;
            moveCastStartTime = EditorApplication.timeSinceStartup;
            moveCastState = MoveCastState.Moving;
            moveCastSkillTriggered = false;

            EditorApplication.update -= UpdateMoveCast1001;
            EditorApplication.update += UpdateMoveCast1001;
        }

        [MenuItem(TriggerSkill7001MenuPath)]
        public static void TriggerSkill7001()
        {
            TriggerSkill(7001);
        }

        [MenuItem(TriggerSkill1008MenuPath)]
        public static void TriggerSkill1008()
        {
            TriggerSkill(1008);
        }

        [MenuItem(TriggerSkill1010MenuPath)]
        public static void TriggerSkill1010()
        {
            TriggerSkill(1010);
        }

        private static void UpdateMoveCast1001()
        {
            if (!EditorApplication.isPlaying || moveCastState == MoveCastState.Idle)
            {
                CleanupMoveCastState();
                return;
            }

            if (moveCastPlayerUnit == null || moveCastMonsterUnit == null || moveCastPlayerAsc == null || moveCastMonsterAsc == null || moveCastSpec == null)
            {
                CleanupMoveCastState();
                return;
            }

            switch (moveCastState)
            {
                case MoveCastState.Moving:
                {
                    double elapsed = EditorApplication.timeSinceStartup - moveCastStartTime;
                    float progress = moveCastMoveDuration <= 0.0001d
                        ? 1f
                        : Mathf.Clamp01((float)(elapsed / moveCastMoveDuration));
                    moveCastPlayerUnit.Position = math.lerp(moveCastStartPosition, moveCastTargetPosition, progress);
                    if (progress < 1f)
                    {
                        return;
                    }

                    moveCastPlayerUnit.Position = moveCastTargetPosition;
                    string beforeState = DescribeState(moveCastSpec);
                    bool castSuccess = moveCastPlayerAsc.TryActivateAbility(moveCastSpec, moveCastMonsterAsc);
                    string afterState = DescribeState(moveCastSpec);
                    Debug.LogWarning($"[SkillRuntimeDebug] move-and-cast skillId={moveCastSkillId} castSuccess={castSuccess} before={beforeState} after={afterState}");
                    moveCastSkillTriggered = castSuccess;
                    moveCastWaitStartTime = EditorApplication.timeSinceStartup;
                    moveCastState = MoveCastState.WaitingDamage;

                    return;
                }
                case MoveCastState.WaitingDamage:
                {
                    double waitElapsed = EditorApplication.timeSinceStartup - moveCastWaitStartTime;
                    if (waitElapsed < moveCastWaitDuration)
                    {
                        return;
                    }

                    float healthAfter = moveCastMonsterAsc.Attributes?.GetCurrentValue(global::ET.NumericType.Hp) ?? -1f;
                    float damage = moveCastHealthBefore >= 0f && healthAfter >= 0f ? moveCastHealthBefore - healthAfter : 0f;
                    Debug.LogWarning($"[SkillRuntimeDebug] move-and-cast skillId={moveCastSkillId} success={moveCastSkillTriggered} wait={moveCastWaitDuration:0.0}s hpBefore={moveCastHealthBefore:0.##} hpAfter={healthAfter:0.##} damage={damage:0.##}");
                    CleanupMoveCastState();
                    return;
                }
            }
        }

        private static void CleanupMoveCastState()
        {
            EditorApplication.update -= UpdateMoveCast1001;
            moveCastState = MoveCastState.Idle;
            moveCastPlayerUnit = null;
            moveCastMonsterUnit = null;
            moveCastPlayerAsc = null;
            moveCastMonsterAsc = null;
            moveCastSpec = null;
            moveCastStartPosition = default;
            moveCastTargetPosition = default;
            moveCastStartTime = 0d;
            moveCastMoveDuration = 0d;
            moveCastWaitStartTime = 0d;
            moveCastHealthBefore = 0f;
            moveCastSkillTriggered = false;
            moveCastSkillId = 0;
            moveCastWaitDuration = 0d;
        }

        private static void TriggerSkill(int skillId)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Play Mode required. skillId={skillId}");
                return;
            }

            Scene currentScene = GetCurrentClientScene();
            if (currentScene == null)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Current scene not found. skillId={skillId}");
                return;
            }

            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            AbilitySystemComponent asc = unit?.GetComponent<SkillUnit>()?.ASC.As();
            GameplayAbilitySpec spec = asc?.Abilities?.FindAbilityById(skillId);
            if (spec == null || asc == null)
            {
                Debug.LogWarning($"[SkillRuntimeDebug] Skill not found. skillId={skillId}");
                return;
            }

            AbilitySystemComponent target = FindDefaultTarget(currentScene, unit);
            string projectilePath = GetProjectilePath(spec);
            int timeEffectCount = spec.GetComponent<TimeEffectRuntimeComponent>()?.TimeEffects?.Count ?? -1;
            string beforeState = DescribeState(spec);
            bool success = asc.TryActivateAbility(spec, target);
            string afterState = DescribeState(spec);


        }

        private static float3 CalculateCastPosition(Unit playerUnit, Unit monsterUnit, float castDistance)
        {
            GameObject playerObject = playerUnit?.GetComponent<GameObjectComponent>()?.GameObject;
            float facingSign = playerObject != null && playerObject.transform.localScale.x < 0f ? -1f : 1f;
            float3 monsterWorldPosition = GetUnitWorldPosition(monsterUnit);
            float targetX = monsterWorldPosition.x + castDistance * facingSign;
            return new float3(targetX, monsterWorldPosition.y, 0f);
        }

        private static Scene GetCurrentClientScene()
        {
            FiberManager fiberManager = FiberManager.Instance;
            if (fiberManager == null)
            {
                return null;
            }

            MethodInfo getMethod = typeof(FiberManager).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getMethod == null)
            {
                return null;
            }

            Fiber mainFiber = getMethod.Invoke(fiberManager, new object[] { ConstFiberId.Main }) as Fiber;
            Scene root = mainFiber?.Root;
            return root?.CurrentScene();
        }

        private static string DescribeState(GameplayAbilitySpec spec)
        {
            if (spec == null)
            {
                return "null-spec";
            }

            if (spec.IsActive)
            {
                return "Casting";
            }

            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            if (cooldownInfo.IsOnCooldown)
            {
                if (cooldownInfo.IsChargeCooldown)
                {
                    return $"Charge {cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
                }

                return $"CD {cooldownInfo.RemainingTime:0.00}";
            }

            return "Ready";
        }

        private static Unit FindNearestMonster(Scene currentScene, Unit selfUnit)
        {
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent?.Children == null)
            {
                return null;
            }

            Unit nearestUnit = null;
            float nearestDistanceSqr = float.MaxValue;
            float3 selfWorldPosition = GetUnitWorldPosition(selfUnit);
            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit unit || unit.Id == selfUnit?.Id)
                {
                    continue;
                }

                if ((UnitType)unit.Config().Type != UnitType.Monster)
                {
                    continue;
                }

                float3 targetWorldPosition = GetUnitWorldPosition(unit);
                float distanceSqr = math.distancesq(targetWorldPosition.xy, selfWorldPosition.xy);
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestUnit = unit;
                }
            }

            return nearestUnit;
        }

        private static AbilitySystemComponent FindDefaultTarget(Scene currentScene, Unit selfUnit)
        {
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent?.Children == null)
            {
                return null;
            }

            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit unit || unit.Id == selfUnit?.Id)
                {
                    continue;
                }

                AbilitySystemComponent target = unit.GetComponent<SkillUnit>()?.ASC.As();
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        private static string GetProjectilePath(GameplayAbilitySpec spec)
        {
            SkillData graphData = spec?.GraphData;
            if (graphData?.nodes == null)
            {
                return string.Empty;
            }

            foreach (NodeData node in graphData.nodes)
            {
                if (node is ProjectileEffectNodeData projectileNode)
                {
                    return projectileNode.projectilePrefabPath;
                }
            }

            return string.Empty;
        }

        private static string DescribeTimeEffectPorts(GameplayAbilitySpec spec)
        {
            TimeEffectRuntimeComponent runtimeComponent = spec?.GetComponent<TimeEffectRuntimeComponent>();
            if (runtimeComponent?.TimeEffects == null || string.IsNullOrEmpty(spec.AnimationNodeGuid))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (TimeEffectRuntime timeEffect in runtimeComponent.TimeEffects)
            {
                int connectedCount = SkillDataCenter.Instance.GetConnectedNodes(spec.SkillId, spec.AnimationNodeGuid, timeEffect.PortId)?.Count ?? 0;
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(timeEffect.PortId);
                builder.Append(':');
                builder.Append(connectedCount);
            }

            return builder.ToString();
        }

        private static string DescribeUnitRuntime(Unit unit)
        {
            if (unit == null)
            {
                return "null-unit";
            }

            GameObject gameObject = unit.GetComponent<GameObjectComponent>()?.GameObject;
            float3 worldPosition = GetUnitWorldPosition(unit);
            float3 centerPosition = GetUnitBindingWorldPosition(unit, "center");
            float scaleX = gameObject != null ? gameObject.transform.localScale.x : 0f;
            string gameObjectName = gameObject != null ? gameObject.name : "null-go";
            return $"cfg={unit.ConfigId} id={unit.Id} unitPos={unit.Position} worldPos={worldPosition} center={centerPosition} scaleX={scaleX:0.##} go={gameObjectName}";
        }

        private static float3 GetUnitWorldPosition(Unit unit)
        {
            GameObject gameObject = unit?.GetComponent<GameObjectComponent>()?.GameObject;
            if (gameObject != null)
            {
                return gameObject.transform.position;
            }

            return unit?.Position ?? default;
        }

        private static float3 GetUnitBindingWorldPosition(Unit unit, string bindingName)
        {
            GameObject gameObject = unit?.GetComponent<GameObjectComponent>()?.GameObject;
            if (gameObject == null)
            {
                return unit?.Position ?? default;
            }

            if (string.IsNullOrEmpty(bindingName))
            {
                return gameObject.transform.position;
            }

            Transform bindingTransform = gameObject.transform.Find(bindingName);
            if (bindingTransform == null)
            {
                bindingTransform = FindChildRecursive(gameObject.transform, bindingName);
            }

            return bindingTransform != null ? bindingTransform.position : gameObject.transform.position;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static float GetPlanarDistance(Unit first, Unit second)
        {
            float3 firstPosition = GetUnitWorldPosition(first);
            float3 secondPosition = GetUnitWorldPosition(second);
            return math.distance(firstPosition.xy, secondPosition.xy);
        }
    }
}
