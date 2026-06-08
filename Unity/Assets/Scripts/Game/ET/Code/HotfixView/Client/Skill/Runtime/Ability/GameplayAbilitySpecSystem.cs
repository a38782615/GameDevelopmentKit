using System;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.AbilityContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.TimeEffectRuntimeComponent))]
    [FriendOfAttribute(typeof(ET.Client.TimeCueRuntimeComponent))]
    [FriendOfAttribute(typeof(ET.Client.CooldownEffectSpec))]
    [FriendOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardRuntime))]
    [FriendOf(typeof(SpecExecutionContext))]

    public static partial class GameplayAbilitySpecSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayAbilitySpec self)
        {
            // 添加时间运行时组件
            self.AddComponent<TimeEffectRuntimeComponent>();
            self.AddComponent<TimeCueRuntimeComponent>();
        }

        [EntitySystem]
        private static void Update(this GameplayAbilitySpec self)
        {
            // Tick 由 AbilityContainerComponent 驱动，不在这里处理
        }

        [EntitySystem]
        private static void Destroy(this GameplayAbilitySpec self)
        {
            self.DisposeExecutionContexts();
            self.RunningEffects.Clear();
            self.PendingRemoveEffects.Clear();
            self.LinkedCardInstanceIds.Clear();
            self.ActivatingCardInstanceId = 0;
            self.ActivatingCardResolvedCostMp = 0f;
        }

        // ============ 初始化 ============

        public static void InitAbility(this GameplayAbilitySpec self, SkillData graphData, AbilitySystemComponent asc)
        {
            self.Owner = asc;

            if (graphData != null)
            {
                self.SkillId = graphData.SkillId;
                SkillDataCenter.Instance.RegisterSkillGraph(graphData);
            }

            self.FindAbilityNode();
            self.FindCostAndCooldownNodes();

            AbilityNodeData abilityNodeData = self.GetAbilityNodeData();
            if (abilityNodeData != null)
            {
                self.Tags = new AbilityTagContainer(
                    abilityNodeData.assetTags,
                    abilityNodeData.cancelAbilitiesWithTags,
                    abilityNodeData.blockAbilitiesWithTags,
                    abilityNodeData.activationOwnedTags,
                    abilityNodeData.activationRequiredTags,
                    abilityNodeData.activationBlockedTags,
                    abilityNodeData.ongoingBlockedTags);
            }
        }

        // ============ 节点查找 ============

        private static void FindAbilityNode(this GameplayAbilitySpec self)
        {
            var graphData = self.GetGraphData();
            if (graphData?.nodes == null) return;

            foreach (var node in graphData.nodes)
            {
                if (node is AbilityNodeData abilityNode)
                {
                    self.AbilityNodeGuid = abilityNode.guid;
                    break;
                }
            }

            self.FindAnimationNode();
        }

        private static void FindAnimationNode(this GameplayAbilitySpec self)
        {
            var timeEffectComp = self.GetTimeEffectRuntime();
            var timeCueComp = self.GetTimeCueRuntime();
            timeEffectComp.TimeEffects.Clear();
            timeCueComp.TimeCues.Clear();

            if (string.IsNullOrEmpty(self.AbilityNodeGuid)) return;

            var connectedNodes = SkillDataCenter.Instance.GetConnectedNodes(self.SkillId, self.AbilityNodeGuid, SkillPortId.Ability.Animation);
            if (connectedNodes == null) return;

            foreach (var node in connectedNodes)
            {
                if (node is AnimationNodeData animNode)
                {
                    self.ApplyAnimationNode(animNode.guid, animNode.animationName, animNode.animationDuration,
                        string.Empty, animNode.isAnimationLooping, animNode.timeEffects, animNode.timeCues);
                    break;
                }

                if (node is UnityAnimationNodeData unityAnimationNode)
                {
                    self.ApplyAnimationNode(unityAnimationNode.guid, unityAnimationNode.animationName, unityAnimationNode.animationDuration,
                        unityAnimationNode.animationComponentPath, unityAnimationNode.isAnimationLooping, unityAnimationNode.timeEffects, unityAnimationNode.timeCues);
                    break;
                }
            }
        }

        private static void ApplyAnimationNode(this GameplayAbilitySpec self, string animationNodeGuid, string animationName,
            string animationDuration, string animationComponentPath, bool isAnimationLooping, List<TimeEffectData> timeEffects, List<TimeCueData> timeCues)
        {
            var timeEffectComp = self.GetTimeEffectRuntime();
            var timeCueComp = self.GetTimeCueRuntime();

            self.AnimationNodeGuid = animationNodeGuid;
            self.AnimationName = animationName;
            self.AnimationComponentPath = animationComponentPath ?? string.Empty;
            int durationFrames = (int)FormulaEvaluator.EvaluateSimple(animationDuration, 1f);
            self.AnimationDuration = SkillConstants.FramesToSeconds(durationFrames);
            self.IsAnimationLooping = isAnimationLooping;

            if (timeEffects != null)
            {
                foreach (var te in timeEffects)
                {
                    timeEffectComp.TimeEffects.Add(new TimeEffectRuntime
                    {
                        TriggerTime = SkillConstants.FramesToSeconds(te.triggerTime),
                        PortId = te.PortId,
                        HasTriggered = false
                    });
                }
            }

            if (timeCues == null)
            {
                return;
            }

            foreach (var tc in timeCues)
            {
                timeCueComp.TimeCues.Add(new TimeCueRuntime
                {
                    StartTime = SkillConstants.FramesToSeconds(tc.startTime),
                    EndTime = tc.endTime < 0 ? -1f : SkillConstants.FramesToSeconds(tc.endTime),
                    PortId = tc.PortId,
                    HasStarted = false,
                    HasEnded = false
                });
            }
        }

        private static void FindCostAndCooldownNodes(this GameplayAbilitySpec self)
        {
            var graphData = self.GetGraphData();
            AbilityNodeData abilityNodeData = self.GetAbilityNodeData();
            if (graphData?.connections == null || abilityNodeData == null) return;

            foreach (var conn in graphData.connections)
            {
                if (conn.outputNodeGuid != abilityNodeData.guid) continue;

                int outputPortId = conn.GetOutputPortId(NodeType.Ability);
                if (outputPortId == SkillPortId.Ability.Cost)
                    self.CostNodeGuid = conn.inputNodeGuid;
                else if (outputPortId == SkillPortId.Ability.Cooldown)
                    self.CooldownNodeGuid = conn.inputNodeGuid;
            }
        }

        // ============ 生命周期 ============

        public static bool CanActivate(this GameplayAbilitySpec self)
        {
            if (self.State == AbilityState.Active) return false;

            var asc = self.GetASC;
            if (asc == null) return false;

            if (!self.Tags.ActivationRequiredTags.IsEmpty)
            {
                if (!asc.OwnedTags.HasAllTags(self.Tags.ActivationRequiredTags))
                    return false;
            }

            if (!self.Tags.ActivationBlockedTags.IsEmpty)
            {
                if (asc.OwnedTags.HasAnyTags(self.Tags.ActivationBlockedTags))
                    return false;
            }

            if (!self.CanAffordCost())
                return false;

            return true;
        }

        public static bool CanAffordCost(this GameplayAbilitySpec self)
        {
            if (self.ActivatingCardInstanceId > 0)
            {
                var ownerAsc = self.GetASC;
                var attributes = ownerAsc?.Attributes;
                if (attributes == null)
                {
                    return false;
                }

                float currentMp = attributes.GetCurrentValue(global::ET.NumericType.Mp);
                return currentMp >= self.GetCurrentResolvedCardCostMp();
            }

            if (string.IsNullOrEmpty(self.CostNodeGuid)) return true;

            var costNodeData = SkillDataCenter.Instance.GetNodeData(self.SkillId, self.CostNodeGuid) as CostEffectNodeData;
            if (costNodeData?.attributeModifiers == null) return true;

            var asc = self.GetASC;
            if (asc?.Attributes == null) return true;

            foreach (var modData in costNodeData.attributeModifiers)
            {
                var modifier = AttributeModifier.FromData(modData);
                float costValue = UnityEngine.Mathf.Abs(modifier.CalculateMagnitude(null));
                float? currentValue = asc.Attributes.GetCurrentValue(modifier.TargetAttrType);
                if (!currentValue.HasValue) continue;
                if (currentValue.Value < costValue) return false;
            }

            return true;
        }

        public static bool IsOnCooldown(this GameplayAbilitySpec self)
        {
            var cdEffect = self.GetCooldownEffect();
            if (cdEffect != null && cdEffect.GetComponent<CooldownEffectSpec>() is CooldownEffectSpec cooldownSpec && cooldownSpec.IsChargeCooldown())
                return cooldownSpec.CurrentCharges <= 0;

            var cooldownTag = self.GetCooldownTag();
            if (!cooldownTag.IsEmpty)
            {
                var asc = self.GetASC;
                return asc != null && asc.OwnedTags.HasTag(cooldownTag);
            }
            return false;
        }

        public static GameplayEffectSpec GetCooldownEffect(this GameplayAbilitySpec self)
        {
            if (string.IsNullOrEmpty(self.CooldownNodeGuid)) return null;
            var asc = self.GetASC;
            var effectContainer = asc?.EffectContainer;
            if (effectContainer == null)
            {
                return null;
            }

            foreach (var effectRef in effectContainer.ActiveEffects)
            {
                var effect = effectRef.As();
                if (effect != null && effect.NodeGuid == self.CooldownNodeGuid)
                {
                    return effect;
                }
            }

            return null;
        }

        private static GameplayTag GetCooldownTag(this GameplayAbilitySpec self)
        {
            if (string.IsNullOrEmpty(self.CooldownNodeGuid)) return default;

            var cooldownNodeData = SkillDataCenter.Instance.GetNodeData(self.SkillId, self.CooldownNodeGuid) as CooldownEffectNodeData;
            if (cooldownNodeData?.grantedTags.Tags != null && cooldownNodeData.grantedTags.Tags.Length > 0)
                return cooldownNodeData.grantedTags.Tags[0];
            return default;
        }

        public static SkillCooldownInfo GetCooldownInfo(this GameplayAbilitySpec self)
        {
            var info = new SkillCooldownInfo();
            var cdEffect = self.GetCooldownEffect();

            if (cdEffect == null)
            {
                info.IsOnCooldown = false;
                return info;
            }

            var cooldownSpec = cdEffect.GetComponent<CooldownEffectSpec>();
            info.IsChargeCooldown = cooldownSpec.IsChargeCooldown();
            if (cooldownSpec.IsChargeCooldown())
            {
                info.CurrentCharges = cooldownSpec.CurrentCharges;
                info.MaxCharges = cooldownSpec.MaxCharges;
                info.ChargeProgress = cooldownSpec.GetChargeProgress();
                info.ChargeTimeRemaining = cooldownSpec.ChargeTimer;
                info.IsOnCooldown = cooldownSpec.CurrentCharges <= 0;
            }
            else
            {
                info.RemainingTime = cdEffect.GetRemainingTime();
                info.TotalDuration = cdEffect.Duration;
                info.IsOnCooldown = cdEffect.GetRemainingTime() > 0;
            }

            return info;
        }

        // ============ 激活/结束 ============

        public static bool ActivateAbility(this GameplayAbilitySpec self, AbilitySystemComponent target = null)
        {
            if (!self.CanActivate()) return false;

            var asc = self.GetASC;
            self.State = AbilityState.Active;
            self.IsRunning = true;
            self.ActivationTime = UnityEngine.Time.time;

            // 注册标签监听
            self.RegisterTagListener();
            // asc.OnTGameplayEvent += self.OnGameplayEvent;

            // 添加激活时授予的标签
            if (!self.Tags.ActivationOwnedTags.IsEmpty)
                asc.OwnedTags.AddTags(self.Tags.ActivationOwnedTags);

            // 取消带有指定标签的其他技能
            if (!self.Tags.CancelAbilitiesWithTags.IsEmpty)
            {
                var abilityContainer = asc.Abilities;
                if (abilityContainer != null)
                {
                    for (int i = abilityContainer.ActiveAbilities.Count - 1; i >= 0; i--)
                    {
                        var ability = abilityContainer.ActiveAbilities[i].As();
                        if (ability != null && ability.Tags.AssetTags.HasAnyTags(self.Tags.CancelAbilitiesWithTags))
                        {
                            ability.CancelAbility();
                        }
                    }
                }
            }

            self.DisposeExecutionContexts();

            // 创建执行上下文
            SpecExecutionContext context = self.AddChild<SpecExecutionContext>();
            context.SetCaster(asc);
            context.SetAbilityLevel(self.Level);
            self.ApplyActivatingCardAttributeOverrides(context);
            if (target != null)
            {
                context.SetMainTarget(target);
                context.AddTarget(target);
            }
            self.Context = context;

            // 重置播放时间和时间效果/Cue状态
            self.CurrentPlayTime = 0f;
            self.GetTimeEffectRuntime()?.ResetAll();
            self.GetTimeCueRuntime()?.ResetAll();

            // 播放动画
            self.RequestPlayAnimation(self.AnimationName, self.AnimationComponentPath, self.IsAnimationLooping);

            // 执行消耗、冷却、激活
            if (!string.IsNullOrEmpty(self.AbilityNodeGuid))
            {
                SkillUnit skillUnit = asc.GetParent<SkillUnit>();
                Unit unit = skillUnit?.Unit.As();
                SkillDiagFileLogger.Log($"[Ability] Activate skillId={self.SkillId} abilityNodeGuid={self.AbilityNodeGuid} target={target?.InstanceId ?? 0}");
                SkillDiagFileLogger.LogAbilityActivated(self.SkillId, unit?.Id ?? 0, asc.InstanceId, target?.InstanceId ?? 0, self.AbilityNodeGuid);
                if (self.ActivatingCardInstanceId <= 0)
                {
                    context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, SkillPortId.Ability.Cost);
                }
                context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, SkillPortId.Ability.Cooldown);
                context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, SkillPortId.Ability.Activate);
            }

            EventSystem.Instance.Publish(self.Root(), new GameplayAbilitySpec.OnActivated()
            {
                Spec = self
            });
            return true;
        }

        public static void EndAbility(this GameplayAbilitySpec self, bool wasCancelled = false)
        {
            if (self.State != AbilityState.Active) return;

            var asc = self.GetASC;

            self.UnregisterTagListener();
            // if (asc != null)
            //     asc.OnTGameplayEvent -= self.OnGameplayEvent;

            self.State = wasCancelled ? AbilityState.Cancelled : AbilityState.Ended;
            self.IsRunning = false;

            self.RequestPlayAnimation("Stand", string.Empty, true);
            self.GetTimeCueRuntime()?.StopAll();

            // 清理运行中的Effect
            foreach (var effect in self.RunningEffects)
            {
                var e = effect.As();
                if (e == null) continue;
                var effectTarget = e.Target.As();
                if (effectTarget != null)
                    self.RemoveEffectFromContainer(effectTarget.EffectContainer, e);
                else
                    e.RemoveEffect();
            }
            self.RunningEffects.Clear();
            self.PendingRemoveEffects.Clear();

            // 移除激活时授予的标签
            if (asc != null && !self.Tags.ActivationOwnedTags.IsEmpty)
                asc.OwnedTags.RemoveTags(self.Tags.ActivationOwnedTags);

            SkillDiagFileLogger.Log($"[Ability] End skillId={self.SkillId} cancelled={wasCancelled}");
            self.ActivatingCardInstanceId = 0;
            self.ActivatingCardResolvedCostMp = 0f;

            EventSystem.Instance.Publish(self.Root(), new GameplayAbilitySpec.OnEnded()
            {
                Spec = self,
                End = wasCancelled
            });
            self.DisposeExecutionContexts();
            self.State = AbilityState.Inactive;
        }

        public static void CancelAbility(this GameplayAbilitySpec self)
        {
            self.EndAbility(true);
        }

        // ============ 标签监听 ============

        private static void RegisterTagListener(this GameplayAbilitySpec self)
        {
            var asc = self.GetASC;
            if (asc?.OwnedTags == null) return;

            if (!self.Tags.OngoingBlockedTags.IsEmpty)
                asc.OwnedTags.OnTagAdded += self.OnOwnerTagAdded;
        }

        private static void UnregisterTagListener(this GameplayAbilitySpec self)
        {
            var asc = self.GetASC;
            if (asc?.OwnedTags == null) return;

            if (!self.Tags.OngoingBlockedTags.IsEmpty)
                asc.OwnedTags.OnTagAdded -= self.OnOwnerTagAdded;
        }

        private static void OnOwnerTagAdded(this GameplayAbilitySpec self, GameplayTag tag)
        {
            if (self.Tags.OngoingBlockedTags.HasTag(tag))
                self.CancelAbility();
        }

        public static void OnGameplayEvent(this GameplayAbilitySpec self, GameplayEventType gameplayEvent)
        {
            AbilityNodeData abilityNodeData = self.GetAbilityNodeData();
            if (abilityNodeData?.eventOutputPorts == null) return;
            SpecExecutionContext context = self.Context;
            if (context == null) return;
            foreach (var portData in abilityNodeData.eventOutputPorts)
            {
                if (portData.eventType == gameplayEvent)
                {
                    context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, portData.PortId);
                }
            }
        }

        // ============ Tick ============

        public static void TickAbility(this GameplayAbilitySpec self, float deltaTime)
        {
            if (self.State != AbilityState.Active) return;

            self.CurrentPlayTime += deltaTime;
            SpecExecutionContext context = self.Context;
            if (context == null) return;

            // 检查时间效果触发
            self.GetTimeEffectRuntime()?.CheckTriggers(self.SkillId, self.AnimationNodeGuid, self.CurrentPlayTime, context);

            // 检查时间Cue触发
            self.GetTimeCueRuntime()?.CheckTriggers(self.SkillId, self.AnimationNodeGuid, self.CurrentPlayTime, self.AnimationDuration, context);

            // 更新运行中的Effect
            self.UpdateRunningEffects(deltaTime);
        }

        private static void UpdateRunningEffects(this GameplayAbilitySpec self, float deltaTime)
        {
            for (int i = 0; i < self.RunningEffects.Count; i++)
            {
                var effect = self.RunningEffects[i];
                var e = effect.As();
                if (e == null) continue;

                if (e.GetEffectNodeData()?.durationType != EffectDurationType.Instant)
                    e.TickEffect(deltaTime);

                if (!e.IsRunning)
                    self.PendingRemoveEffects.Add(effect);
            }

            if (self.PendingRemoveEffects.Count > 0)
            {
                foreach (var effect in self.PendingRemoveEffects)
                    self.RunningEffects.Remove(effect);
                self.PendingRemoveEffects.Clear();
            }
        }

        private static void DisposeExecutionContexts(this GameplayAbilitySpec self)
        {
            self.Context = default;
            using ListComponent<long> removeChildIds = ListComponent<long>.Create();
            foreach (Entity child in self.Children.Values)
            {
                if (child is SpecExecutionContext context)
                {
                    GameplayEffectSpec ownerEffectSpec = context.GetOwnerEffectSpec();
                    if (ownerEffectSpec != null && !ownerEffectSpec.IsDisposed && !ownerEffectSpec.IsRemoved)
                    {
                        continue;
                    }

                    removeChildIds.Add(child.Id);
                }
            }

            foreach (long childId in removeChildIds)
            {
                self.RemoveChild(childId);
            }
        }

        // ============ Effect注册 ============

        public static void RegisterRunningEffect(this GameplayAbilitySpec self, GameplayEffectSpec effectSpec)
        {
            if (effectSpec != null && effectSpec.IsRunning && !self.RunningEffects.Contains(effectSpec))
                self.RunningEffects.Add(effectSpec);
        }

        // ============ 查询 ============

        public static bool BlocksAbilityWithTags(this GameplayAbilitySpec self, GameplayTagSet abilityTags)
        {
            if (!self.IsActive || self.Tags.BlockAbilitiesWithTags.IsEmpty)
                return false;
            return abilityTags.HasAnyTags(self.Tags.BlockAbilitiesWithTags);
        }

        // ============ 动画 ============

        private static void RequestPlayAnimation(this GameplayAbilitySpec self, string name, string animationComponentPath, bool loop)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            SkillUnit skillUnit = self.GetASC?.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            if (unit == null)
            {
                return;
            }

            EventSystem.Instance.Publish(unit.Scene(), new SkillAnimationPlay
            {
                Unit = unit,
                AnimationName = name,
                AnimationComponentPath = animationComponentPath ?? string.Empty,
                Loop = loop
            });
        }

        private static TimeCueRuntimeComponent GetTimeCueRuntime(this GameplayAbilitySpec self)
        {
            return self.GetComponent<TimeCueRuntimeComponent>();
        }

        private static TimeEffectRuntimeComponent GetTimeEffectRuntime(this GameplayAbilitySpec self)
        {
            return self.GetComponent<TimeEffectRuntimeComponent>();
        }

        private static void RemoveEffectFromContainer(this GameplayAbilitySpec self, GameplayEffectContainerComponent container, GameplayEffectSpec effectSpec)
        {
            if (container == null || effectSpec == null)
            {
                return;
            }

            if (container.IsUpdating)
            {
                if (!container.PendingRemove.Contains(effectSpec))
                {
                    container.PendingRemove.Add(effectSpec);
                }
                return;
            }

            effectSpec.RemoveEffect();
            container.ActiveEffects.Remove(effectSpec);
            if (!effectSpec.IsDisposed)
            {
                effectSpec.Dispose();
            }
        }

        public static SkillData GetGraphData(this GameplayAbilitySpec self)
        {
            return self == null ? null : SkillDataCenter.Instance.GetSkillGraph(self.SkillId);
        }

        public static AbilityNodeData GetAbilityNodeData(this GameplayAbilitySpec self)
        {
            if (self == null || string.IsNullOrEmpty(self.AbilityNodeGuid))
            {
                return null;
            }

            return SkillDataCenter.Instance.GetNodeData(self.SkillId, self.AbilityNodeGuid) as AbilityNodeData;
        }

        public static int GetSkillNumericId(this GameplayAbilitySpec self)
        {
            int skillId = self.GetAbilityNodeData()?.skillId ?? 0;
            if (skillId > 0)
            {
                return skillId;
            }

            return int.TryParse(self?.SkillId, out skillId) ? skillId : 0;
        }

        public static void BindCardInstance(this GameplayAbilitySpec self, long cardInstanceId)
        {
            if (self == null || cardInstanceId <= 0)
            {
                return;
            }

            if (!self.LinkedCardInstanceIds.Contains(cardInstanceId))
            {
                self.LinkedCardInstanceIds.Add(cardInstanceId);
            }
        }

        public static void UnbindCardInstance(this GameplayAbilitySpec self, long cardInstanceId)
        {
            if (self == null || cardInstanceId <= 0)
            {
                return;
            }

            self.LinkedCardInstanceIds.Remove(cardInstanceId);
            if (self.ActivatingCardInstanceId == cardInstanceId)
            {
                self.ActivatingCardInstanceId = 0;
            }
        }

        public static void SetActivatingCardInstance(this GameplayAbilitySpec self, long cardInstanceId)
        {
            if (self == null)
            {
                return;
            }

            self.ActivatingCardInstanceId = cardInstanceId;
        }

        public static float GetCurrentResolvedCardCostMp(this GameplayAbilitySpec self)
        {
            return self?.ActivatingCardResolvedCostMp ?? 0f;
        }

        private static void ApplyActivatingCardAttributeOverrides(this GameplayAbilitySpec self, SpecExecutionContext context)
        {
            if (self == null || context == null || self.ActivatingCardInstanceId <= 0)
            {
                return;
            }

            AbilitySystemComponent asc = self.GetASC;
            SkillCardDeckComponent deck = asc?.GetParent<SkillUnit>()?.SkillCardDeck.As();
            SkillCardRuntime card = deck?.GetChild<SkillCardRuntime>(self.ActivatingCardInstanceId);
            if (card?.AttributeOverrides == null || card.AttributeOverrides.Count <= 0)
            {
                return;
            }

            foreach ((int numericType, float value) in card.AttributeOverrides)
            {
                context.CasterAttributeOverrides[numericType] = value;
            }

            SkillDiagFileLogger.Log($"[Ability] Apply card attribute overrides, SkillId={self.GetSkillNumericId()}, CardInstanceId={card.CardInstanceId}, OverrideCount={context.CasterAttributeOverrides.Count}");
        }
    }
}
