using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]
    [FriendOfAttribute(typeof(ET.Client.GameplayAbilitySpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayCueContainerComponent))]
    public static partial class SpecExecutionContextSystem
    {
        // ============ ASC 获取 ============

        public static AbilitySystemComponent GetCaster(this SpecExecutionContext self)
        {
            return self.Caster.As();
        }

        public static AbilitySystemComponent GetMainTarget(this SpecExecutionContext self)
        {
            return self.MainTarget.As();
        }

        public static AbilitySystemComponent GetParentInputTarget(this SpecExecutionContext self)
        {
            return self.ParentInputTarget.As();
        }

        public static GameplayAbilitySpec GetAbilitySpec(this SpecExecutionContext self)
        {
            return self.AbilitySpec.As();
        }

        public static GameplayEffectSpec GetOwnerEffectSpec(this SpecExecutionContext self)
        {
            return self.OwnerEffectSpec.As();
        }

        // ============ 目标管理 ============

        public static void SetTargets(this SpecExecutionContext self, List<EntityRef<AbilitySystemComponent>> targets)
        {
            self.Targets.Clear();
            if (targets != null)
                self.Targets.AddRange(targets);
        }

        public static void AddTarget(this SpecExecutionContext self, AbilitySystemComponent target)
        {
            if (target != null && !self.Targets.Contains(target))
                self.Targets.Add(target);
        }

        public static void ClearTargets(this SpecExecutionContext self)
        {
            self.Targets.Clear();
        }

        public static AbilitySystemComponent GetTargetByType(this SpecExecutionContext self, TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                    return self.GetCaster();
                case TargetType.MainTarget:
                    return self.GetMainTarget();
                case TargetType.ParentInput:
                    return self.GetParentInputTarget();
                default:
                    return self.GetMainTarget();
            }
        }

        public static AbilitySystemComponent GetTarget(this SpecExecutionContext self, TargetType targetType)
        {
            return self.GetTargetByType(targetType);
        }

        public static List<AbilitySystemComponent> GetTargetsByType(this SpecExecutionContext self, TargetType targetType)
        {
            var result = new List<AbilitySystemComponent>();
            switch (targetType)
            {
                case TargetType.Caster:
                    var caster = self.GetCaster();
                    if (caster != null) result.Add(caster);
                    break;
                case TargetType.MainTarget:
                    var mainTarget = self.GetMainTarget();
                    if (mainTarget != null) result.Add(mainTarget);
                    break;
                case TargetType.ParentInput:
                    var parentInput = self.GetParentInputTarget();
                    if (parentInput != null) result.Add(parentInput);
                    break;
                default:
                    foreach (var e in self.Targets)
                    {
                        var target = e.As();
                        if (target != null)
                        {
                            result.Add(target);
                        }
                    }
                    break;
            }
            return result;
        }

        public static List<AbilitySystemComponent> GetTargets(this SpecExecutionContext self, TargetType targetType)
        {
            return self.GetTargetsByType(targetType);
        }

        // ============ 自定义数据 ============

        public static void SetCustomData(this SpecExecutionContext self, string key, object value)
        {
            self.CustomData[key] = value;
        }

        public static T GetCustomData<T>(this SpecExecutionContext self, string key, T defaultValue = default)
        {
            if (self.CustomData.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        public static bool HasCustomData(this SpecExecutionContext self, string key)
        {
            return self.CustomData.ContainsKey(key);
        }

        // ============ 创建带父节点目标的上下文 ============

        /// <summary>
        /// 创建带有父节点目标的新上下文（用于范围搜索等场景）
        /// 注意：新上下文挂载在同一个 AbilitySpec 上，作为临时数据使用
        /// </summary>
        public static SpecExecutionContext CreateWithParentInput(this SpecExecutionContext self, AbilitySystemComponent parentInputTarget)
        {
            // 创建一个新的上下文实例（临时使用，不挂载到Entity树）
            var newContext = new SpecExecutionContext();
            newContext.AbilitySpec = self.AbilitySpec;
            newContext.OwnerEffectSpec = self.OwnerEffectSpec;
            newContext.Caster = self.Caster;
            newContext.MainTarget = self.MainTarget;
            newContext.ParentInputTarget = parentInputTarget;
            newContext.ProjectileObject = self.ProjectileObject;
            newContext.PlacementObject = self.PlacementObject;
            newContext.AbilityLevel = self.AbilityLevel;
            newContext.StackCount = self.StackCount;
            newContext.Targets.AddRange(self.Targets);

            foreach (var kvp in self.CustomData)
                newContext.CustomData[kvp.Key] = kvp.Value;

            return newContext;
        }

        // ============ 位置获取 ============

        public static Vector3 GetPosition(this SpecExecutionContext self, PositionSourceType sourceType, string bindingName = null)
        {
            GameObject sourceObject = self.GetSourceObject(sourceType);
            if (sourceObject == null) return Vector3.zero;
            return GetPositionFromObject(sourceObject, bindingName);
        }

        public static GameObject GetSourceObject(this SpecExecutionContext self, PositionSourceType sourceType)
        {
            switch (sourceType)
            {
                case PositionSourceType.Caster:
                    return self.GetCaster()?.Owner;
                case PositionSourceType.MainTarget:
                    return self.GetMainTarget()?.Owner;
                case PositionSourceType.ParentInput:
                    return self.GetParentInputTarget()?.Owner;
                case PositionSourceType.Projectile:
                    return self.ProjectileObject;
                case PositionSourceType.Placement:
                    return self.PlacementObject;
                default:
                    return null;
            }
        }

        private static Vector3 GetPositionFromObject(GameObject obj, string bindingName)
        {
            if (obj == null) return Vector3.zero;

            if (string.IsNullOrEmpty(bindingName))
                return obj.transform.position;

            Transform bindingPoint = obj.transform.Find(bindingName);
            if (bindingPoint != null)
                return bindingPoint.position;

            bindingPoint = FindChildRecursive(obj.transform, bindingName);
            if (bindingPoint != null)
                return bindingPoint.position;

            return obj.transform.position;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ============ 修改器上下文 ============

        public static ModifierCalculationContext CreateModifierContext(this SpecExecutionContext self, AbilitySystemComponent target)
        {
            return new ModifierCalculationContext
            {
                SourceAttributes = self.GetCaster()?.Attributes,
                TargetAttributes = target?.Attributes,
                EffectLevel = self.AbilityLevel
            };
        }


        // ============ 执行链路（原 SpecExecutor 已合并至此） ============

        public static void ExecuteConnectedNodes(this SpecExecutionContext self, string skillId, string nodeGuid, string outputPortName)
        {
            if (string.IsNullOrEmpty(skillId) || string.IsNullOrEmpty(nodeGuid))
                return;

            var connectedNodes = SkillDataCenter.Instance.GetConnectedNodes(skillId, nodeGuid, outputPortName);
            if (connectedNodes == null || connectedNodes.Count == 0)
                return;

            foreach (var nodeData in connectedNodes)
            {
                self.ExecuteNode(skillId, nodeData);
            }
        }

        /// <summary>
        /// 执行指定端口连接的Cue节点，并返回触发的CueSpec列表
        /// </summary>
        public static List<GameplayCueSpec> ExecuteConnectedCueNodes(this SpecExecutionContext self, string skillId, string nodeGuid, string outputPortName)
        {
            var triggeredCues = new List<GameplayCueSpec>();

            if (string.IsNullOrEmpty(skillId) || string.IsNullOrEmpty(nodeGuid))
                return triggeredCues;

            var connectedNodes = SkillDataCenter.Instance.GetConnectedNodes(skillId, nodeGuid, outputPortName);
            if (connectedNodes == null || connectedNodes.Count == 0)
                return triggeredCues;

            foreach (var nodeData in connectedNodes)
            {
                var category = GetNodeCategory(nodeData.nodeType);
                if (category == NodeCategory.Cue)
                {
                    var cueSpec = self.ExecuteCueNodeAndReturn(skillId, nodeData);
                    if (cueSpec != null)
                        triggeredCues.Add(cueSpec);
                }
                else
                {
                    self.ExecuteNode(skillId, nodeData);
                }
            }

            return triggeredCues;
        }

        /// <summary>
        /// 执行单个节点
        /// </summary>
        public static void ExecuteNode(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            if (nodeData == null) return;

            var category = GetNodeCategory(nodeData.nodeType);

            switch (category)
            {
                case NodeCategory.Effect:
                    self.ExecuteEffectNode(skillId, nodeData);
                    break;
                case NodeCategory.Task:
                    self.ExecuteTaskNode(skillId, nodeData);
                    break;
                case NodeCategory.Condition:
                    self.ExecuteConditionNode(skillId, nodeData);
                    break;
                case NodeCategory.Cue:
                    self.ExecuteCueNode(skillId, nodeData);
                    break;
            }
        }

        /// <summary>
        /// 执行效果节点 - 通过 EffectContainer AddChild 创建 Entity
        /// </summary>
        private static void ExecuteEffectNode(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            if (self == null) return;

            var caster = self.GetCaster();
            if (caster == null) return;

            // 根据目标获取 EffectContainer
            var target = self.GetTargetByType(nodeData.targetType);
            var container = (target ?? caster).EffectContainer;
            if (container == null) return;

            var effectSpec = container.AddChild<GameplayEffectSpec>();
            self.InitializeEffectSpec(effectSpec, skillId, nodeData);
            var handler = effectSpec.GetEffectHandler();
            if (handler == null)
            {
                if (!effectSpec.IsDisposed)
                {
                    effectSpec.Dispose();
                }
                return;
            }

            handler.Execute();

            // 如果是持续/周期效果且正在运行，注册到对应的Owner
            if (effectSpec.IsRunning && effectSpec.EffectNodeData?.durationType != EffectDurationType.Instant)
            {
                self.RegisterRunningEffect(effectSpec);
            }
            else if (!effectSpec.IsRunning && !effectSpec.IsApplied)
            {
                // 瞬时效果执行完毕，Dispose
                if (!effectSpec.IsDisposed)
                    effectSpec.Dispose();
            }
        }

        private static void ExecuteTaskNode(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            if (self == null) return;

            var caster = self.GetCaster();
            if (caster == null) return;

            var taskSpec = caster.AddChild<TaskSpec>();
            taskSpec.InitTask(skillId, nodeData.guid, self);
            taskSpec.Execute();

            if (!taskSpec.IsDisposed)
                taskSpec.Dispose();
        }

        private static void ExecuteConditionNode(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            if (self == null) return;

            var caster = self.GetCaster();
            if (caster == null) return;

            var conditionSpec = caster.AddChild<ConditionSpec>();
            conditionSpec.InitCondition(skillId, nodeData.guid, self);
            bool result = conditionSpec.Evaluate();

            self.ExecuteConnectedNodes(skillId, nodeData.guid, result ? "是" : "否");

            if (!conditionSpec.IsDisposed)
                conditionSpec.Dispose();
        }

        /// <summary>
        /// 执行Cue节点
        /// </summary>
        private static void ExecuteCueNode(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            var cueSpec = self.ExecuteCueNodeAndReturn(skillId, nodeData);
            if (cueSpec != null && cueSpec.IsRunning)
            {
                self.RegisterRunningCue(cueSpec);
            }
        }

        /// <summary>
        /// 执行Cue节点并返回CueSpec
        /// </summary>
        private static GameplayCueSpec ExecuteCueNodeAndReturn(this SpecExecutionContext self, string skillId, NodeData nodeData)
        {
            if (self == null) return null;

            var caster = self.GetCaster();
            if (caster == null) return null;

            var cueContainer = caster.GetComponent<GameplayCueContainerComponent>();
            if (cueContainer == null) return null;

            var cueSpec = cueContainer.AddChild<GameplayCueSpec>();
            self.InitializeCueSpec(cueSpec, skillId, nodeData);

            var handler = cueSpec.GetCueHandler();
            if (handler == null)
            {
                if (!cueSpec.IsDisposed)
                {
                    cueSpec.Dispose();
                }
                return null;
            }

            var target = nodeData == null ? self.GetMainTarget() : self.GetTargetByType(nodeData.targetType);
            if (self.CanPlayCueOnTarget(cueSpec, target))
            {
                handler.PlayCue(target);
            }

            return cueSpec;
        }

        /// <summary>
        /// 注册运行中的Effect
        /// </summary>
        private static void RegisterRunningEffect(this SpecExecutionContext self, GameplayEffectSpec effectSpec)
        {
            if (self == null) return;

            var abilitySpec = self.GetAbilitySpec();
            if (abilitySpec != null && abilitySpec.IsRunning && effectSpec.EffectNodeData.cancelOnAbilityEnd)
            {
                if (!abilitySpec.RunningEffects.Contains(effectSpec))
                {
                    abilitySpec.RunningEffects.Add(effectSpec);
                }
            }
        }

        /// <summary>
        /// 注册运行中的Cue
        /// </summary>
        private static void RegisterRunningCue(this SpecExecutionContext self, GameplayCueSpec cueSpec)
        {
            if (self == null) return;

            // 注册到 CueContainer
            var caster = self.GetCaster();
            var cueContainer = caster?.GetComponent<GameplayCueContainerComponent>();
            if (cueContainer != null && !cueContainer.ActiveCues.Contains(cueSpec))
            {
                cueContainer.ActiveCues.Add(cueSpec);
            }

            // 如果是Effect触发的Cue，注册到Effect
            if (self.OwnerEffectSpec.As() != null && cueSpec.DestroyWithNode)
            {
                var ownerEffect = self.GetOwnerEffectSpec();
                if (ownerEffect != null && !ownerEffect.TriggeredCueIds.Contains(cueSpec.Id))
                {
                    ownerEffect.TriggeredCueIds.Add(cueSpec.Id);
                }
            }
        }

        private static void InitializeEffectSpec(this SpecExecutionContext self, GameplayEffectSpec effectSpec, string skillId, NodeData nodeData)
        {
            effectSpec.SkillId = skillId;
            effectSpec.NodeGuid = nodeData.guid;
            effectSpec.Context = self;
            effectSpec.Source = self.Caster;
            effectSpec.Level = self.AbilityLevel;
            effectSpec.IsRunning = false;
            effectSpec.IsCancelled = false;
            effectSpec.IsApplied = false;
            effectSpec.IsExpired = false;
            effectSpec.WasRefreshed = false;
            effectSpec.ElapsedTime = 0f;
            effectSpec.PeriodTimer = 0f;
            effectSpec.StackCount = 1;
            effectSpec.TriggeredCueIds.Clear();
            effectSpec.Modifiers.Clear();
            effectSpec.SetByCallerValues.Clear();
            effectSpec.SnapshotValues.Clear();

            var effectData = effectSpec.EffectNodeData;
            if (effectData != null)
            {
                effectSpec.Tags = new EffectTagContainer(effectData);
            }

            var source = self.GetCaster();
            if (source?.Attributes != null)
            {
                effectSpec.SnapshotValues = source.Attributes.CreateSnapshot();
            }

            effectSpec.Duration = FormulaEvaluator.EvaluateSimple(effectData?.duration, 0f);
            effectSpec.Period = FormulaEvaluator.EvaluateSimple(effectData?.period, 1f);

            if (effectData?.attributeModifiers != null)
            {
                foreach (var modData in effectData.attributeModifiers)
                {
                    effectSpec.Modifiers.Add(AttributeModifier.FromData(modData));
                }
            }

            effectSpec.AttachEffectComponent(nodeData.nodeType);
            effectSpec.GetEffectHandler()?.OnInitialize();
        }

        private static AEffectHandler GetEffectHandler(this GameplayEffectSpec effectSpec)
        {
            if (effectSpec == null || string.IsNullOrEmpty(effectSpec.HandName))
            {
                return null;
            }

            var handler = EffectDispatcherComponent.Instance.Get(effectSpec.HandName);
            if (handler == null)
            {
                Log.Error($"EffectHandler not found: {effectSpec.HandName}");
                return null;
            }

            handler.Spec = effectSpec;
            handler.NodeData = effectSpec.EffectNodeData;
            return handler;
        }

        private static void InitializeCueSpec(this SpecExecutionContext self, GameplayCueSpec cueSpec, string skillId, NodeData nodeData)
        {
            cueSpec.SkillId = skillId;
            cueSpec.NodeGuid = nodeData.guid;
            cueSpec.ContextOwner = self.AbilitySpec;
            cueSpec.IsRunning = false;
            cueSpec.IsCancelled = false;
            cueSpec.ActiveCue = null;

            cueSpec.AttachCueComponent(nodeData.nodeType);

            var cueData = cueSpec.CueNodeData;
            if (cueData != null)
            {
                cueSpec.Tags = new CueTagContainer(cueData);
            }

            cueSpec.GetCueHandler()?.OnInitialize();
        }

        private static ACueHandler GetCueHandler(this GameplayCueSpec cueSpec)
        {
            if (cueSpec == null || string.IsNullOrEmpty(cueSpec.HandName))
            {
                return null;
            }

            var handler = CueDispatcherComponent.Instance.Get(cueSpec.HandName);
            if (handler == null)
            {
                Log.Error($"CueHandler not found: {cueSpec.HandName}");
                return null;
            }

            handler.Spec = cueSpec;
            handler.NodeData = cueSpec.CueNodeData;
            return handler;
        }

        private static bool CanPlayCueOnTarget(this SpecExecutionContext self, GameplayCueSpec cueSpec, AbilitySystemComponent target)
        {
            if (cueSpec == null || target == null)
            {
                return true;
            }

            if (!cueSpec.Tags.RequiredTags.IsEmpty && !target.OwnedTags.HasAllTags(cueSpec.Tags.RequiredTags))
            {
                return false;
            }

            if (!cueSpec.Tags.ImmunityTags.IsEmpty && target.OwnedTags.HasAnyTags(cueSpec.Tags.ImmunityTags))
            {
                return false;
            }

            return true;
        }

        private static void AttachEffectComponent(this GameplayEffectSpec spec, NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.DamageEffect:
                    spec.HandName = "DamageEffectSpecHandler";
                    spec.EnsureEffectComponent<DamageEffectSpec>();
                    return;
                case NodeType.HealEffect:
                    spec.HandName = "HealEffectSpecHandler";
                    spec.EnsureEffectComponent<HealEffectSpec>();
                    return;
                case NodeType.CostEffect:
                    spec.HandName = "CostEffectSpecHandler";
                    spec.EnsureEffectComponent<CostEffectSpec>();
                    return;
                case NodeType.ModifyAttributeEffect:
                    spec.HandName = "ModifyAttributeEffectSpecHandler";
                    spec.EnsureEffectComponent<ModifyAttributeEffectSpec>();
                    return;
                case NodeType.GenericEffect:
                    spec.HandName = "GenericEffectSpecHandler";
                    spec.EnsureEffectComponent<GenericEffectSpec>();
                    return;
                case NodeType.ProjectileEffect:
                    spec.HandName = "ProjectileEffectSpecHandler";
                    spec.EnsureEffectComponent<ProjectileEffectSpec>();
                    return;
                case NodeType.PlacementEffect:
                    spec.HandName = "PlacementEffectSpecHandler";
                    spec.EnsureEffectComponent<PlacementEffectSpec>();
                    return;
                case NodeType.DisplaceEffect:
                    spec.HandName = "DisplaceEffectSpecHandler";
                    spec.EnsureEffectComponent<DisplaceEffectSpec>();
                    return;
                case NodeType.CooldownEffect:
                    spec.HandName = "CooldownEffectSpecHandler";
                    spec.EnsureEffectComponent<CooldownEffectSpec>();
                    return;
                case NodeType.BuffEffect:
                    spec.HandName = "BuffEffectSpecHandler";
                    spec.EnsureEffectComponent<BuffEffectSpec>();
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        private static void AttachCueComponent(this GameplayCueSpec spec, NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.ParticleCue:
                    spec.HandName = "ParticleCueSpecHandler";
                    spec.EnsureCueComponent<ParticleCueSpec>();
                    return;
                case NodeType.SoundCue:
                    spec.HandName = "SoundCueSpecHandler";
                    spec.EnsureCueComponent<SoundCueSpec>();
                    return;
                case NodeType.FloatingTextCue:
                    spec.HandName = "FloatingTextCueSpecHandler";
                    spec.EnsureCueComponent<FloatingTextCueSpec>();
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        private static void EnsureEffectComponent<T>(this GameplayEffectSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        private static void EnsureCueComponent<T>(this GameplayCueSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        /// <summary>
        /// 获取节点分类
        /// </summary>
        private static NodeCategory GetNodeCategory(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.SearchTargetTask:
                case NodeType.EndAbilityTask:
                    return NodeCategory.Task;

                case NodeType.AttributeCompareCondition:
                    return NodeCategory.Condition;

                case NodeType.ParticleCue:
                case NodeType.SoundCue:
                case NodeType.FloatingTextCue:
                    return NodeCategory.Cue;

                default:
                    return NodeCategory.Effect;
            }
        }
    }
}
