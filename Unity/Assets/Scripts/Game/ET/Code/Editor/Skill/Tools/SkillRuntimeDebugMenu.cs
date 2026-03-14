using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ET.Client.Editor
{
    public static class SkillRuntimeDebugMenu
    {
        private const string TriggerSkill1010MenuPath = "SkillEditor/Runtime/Trigger Skill 1010";

        [MenuItem(TriggerSkill1010MenuPath)]
        public static void TriggerSkill1010()
        {
            TriggerSkill(1010);
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
            SkillDiagFileLogger.Log(
                $"[DiagSkillDebug] graph skillId={skillId} animation={spec.AnimationName} animationGuid={spec.AnimationNodeGuid} duration={spec.AnimationDuration:0.00} timeEffects={timeEffectCount} projectilePath={projectilePath} ports={DescribeTimeEffectPorts(spec)}");
            string beforeState = DescribeState(spec);
            bool success = asc.TryActivateAbility(spec, target);
            string afterState = DescribeState(spec);

            SkillDiagFileLogger.Log(
                $"[DiagSkillDebug] trigger skillId={skillId} success={success} before={beforeState} after={afterState} hasTarget={(target != null)}");
            Debug.Log(
                $"[SkillRuntimeDebug] trigger skillId={skillId} success={success} before={beforeState} after={afterState} hasTarget={(target != null)}");
        }

        private static Scene GetCurrentClientScene()
        {
            MethodInfo getMethod = typeof(FiberManager).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);
            Fiber mainFiber = getMethod?.Invoke(FiberManager.Instance, new object[] { ConstFiberId.Main }) as Fiber;
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
    }
}
