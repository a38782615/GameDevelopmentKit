using System;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilityContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.TimeEffectRuntimeComponent))]
    [FriendOfAttribute(typeof(ET.Client.TimeCueRuntimeComponent))]
    [FriendOfAttribute(typeof(ET.Client.CooldownEffectSpec))]

    public static partial class GameplayAbilitySpecSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayAbilitySpec self)
        {
        }

        [EntitySystem]
        private static void Update(this GameplayAbilitySpec self)
        {
            // Tick 由 AbilityContainerComponent 驱动，不在这里处理
        }

        [EntitySystem]
        private static void Destroy(this GameplayAbilitySpec self)
        {
            self.RunningEffects.Clear();
            self.PendingRemoveEffects.Clear();
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

            if (self.AbilityNodeData != null)
            {
                self.Tags = new AbilityTagContainer(self.AbilityNodeData);
            }

            // 添加时间运行时组件
            self.AddComponent<TimeEffectRuntimeComponent>();
            self.AddComponent<TimeCueRuntimeComponent>();
        }

        // ============ 节点查找 ============

        private static void FindAbilityNode(this GameplayAbilitySpec self)
        {
            var graphData = self.GraphData;
            if (graphData?.nodes == null) return;

            foreach (var node in graphData.nodes)
            {
                if (node is AbilityNodeData abilityNode)
                {
                    self.AbilityNodeData = abilityNode;
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

            var connectedNodes = SkillDataCenter.Instance.GetConnectedNodes(self.SkillId, self.AbilityNodeGuid, "动画");
            if (connectedNodes == null) return;

            foreach (var node in connectedNodes)
            {
                if (node is AnimationNodeData animNode)
                {
                    self.AnimationNodeGuid = animNode.guid;
                    self.AnimationName = animNode.animationName;
                    int durationFrames = (int)FormulaEvaluator.EvaluateSimple(animNode.animationDuration, 1f);
                    self.AnimationDuration = SkillConstants.FramesToSeconds(durationFrames);
                    self.IsAnimationLooping = animNode.isAnimationLooping;

                    if (animNode.timeEffects != null)
                    {
                        foreach (var te in animNode.timeEffects)
                        {
                            timeEffectComp.TimeEffects.Add(new TimeEffectRuntime
                            {
                                TriggerTime = SkillConstants.FramesToSeconds(te.triggerTime),
                                PortId = te.portId,
                                HasTriggered = false
                            });
                        }
                    }

                    if (animNode.timeCues != null)
                    {
                        foreach (var tc in animNode.timeCues)
                        {
                            timeCueComp.TimeCues.Add(new TimeCueRuntime
                            {
                                StartTime = SkillConstants.FramesToSeconds(tc.startTime),
                                EndTime = tc.endTime < 0 ? -1f : SkillConstants.FramesToSeconds(tc.endTime),
                                PortId = tc.portId,
                                HasStarted = false,
                                HasEnded = false
                            });
                        }
                    }

                    break;
                }
            }
        }

        private static void FindCostAndCooldownNodes(this GameplayAbilitySpec self)
        {
            var graphData = self.GraphData;
            if (graphData?.connections == null || self.AbilityNodeData == null) return;

            foreach (var conn in graphData.connections)
            {
                if (conn.outputNodeGuid != self.AbilityNodeData.guid) continue;

                if (conn.outputPortName == "消耗")
                    self.CostNodeGuid = conn.inputNodeGuid;
                else if (conn.outputPortName == "冷却")
                    self.CooldownNodeGuid = conn.inputNodeGuid;
            }
        }

        // ============ 生命周期 ============

        public static bool CanActivate(this GameplayAbilitySpec self)
        {
            if (self.State == AbilityState.Active) return false;

            var asc = self.GetASC;
            if (asc == null) return false;

            var ownedTags = asc.OwnedTags;

            if (!self.Tags.ActivationRequiredTags.IsEmpty)
            {
                if (ownedTags == null || !ownedTags.HasAllTags(self.Tags.ActivationRequiredTags))
                    return false;
            }

            if (!self.Tags.ActivationBlockedTags.IsEmpty)
            {
                if (ownedTags != null && ownedTags.HasAnyTags(self.Tags.ActivationBlockedTags))
                    return false;
            }

            if (!self.CanAffordCost())
                return false;

            return true;
        }

        public static bool CanAffordCost(this GameplayAbilitySpec self)
        {
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
            if (cdEffect != null && cdEffect.GetComponent<CooldownEffectSpec>() is CooldownEffectSpec cooldownSpec && cooldownSpec.IsChargeCooldown)
                return cooldownSpec.CurrentCharges <= 0;

            var cooldownTag = self.GetCooldownTag();
            if (!cooldownTag.IsEmpty)
            {
                var asc = self.GetASC;
                return asc?.OwnedTags != null && asc.OwnedTags.HasTag(cooldownTag);
            }
            return false;
        }

        public static GameplayEffectSpec GetCooldownEffect(this GameplayAbilitySpec self)
        {
            if (string.IsNullOrEmpty(self.CooldownNodeGuid)) return null;
            var asc = self.GetASC;
            var container = asc?.EffectContainer;
            if (container == null)
            {
                return null;
            }

            foreach (var effectRef in container.ActiveEffects)
            {
                var effect = effectRef.As();
                if (effect?.NodeGuid == self.CooldownNodeGuid)
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
            info.IsChargeCooldown = cooldownSpec.IsChargeCooldown;
            if (cooldownSpec.IsChargeCooldown)
            {
                info.CurrentCharges = cooldownSpec.CurrentCharges;
                info.MaxCharges = cooldownSpec.MaxCharges;
                info.ChargeProgress = cooldownSpec.ChargeProgress;
                info.ChargeTimeRemaining = cooldownSpec.ChargeTimer;
                info.IsOnCooldown = cooldownSpec.CurrentCharges <= 0;
            }
            else
            {
                info.RemainingTime = cdEffect.RemainingTime;
                info.TotalDuration = cdEffect.Duration;
                info.IsOnCooldown = cdEffect.RemainingTime > 0;
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
                self.CancelSiblingAbilitiesWithTags(asc.Abilities, self.Tags.CancelAbilitiesWithTags);

            // 创建执行上下文
            self.Context = new SpecExecutionContext();
            self.Context.AbilitySpec = self;
            self.Context.Caster = asc;
            self.Context.AbilityLevel = self.Level;
            if (target != null)
            {
                self.Context.MainTarget = target;
                if (!self.Context.Targets.Contains(target))
                {
                    self.Context.Targets.Add(target);
                }
            }

            // 重置播放时间和时间效果/Cue状态
            self.CurrentPlayTime = 0f;
            self.ResetTimeEffectRuntime();
            self.GetTimeCueRuntime()?.ResetAll();

            // 播放动画
            self.PlayAnimation(self.AnimationName, self.IsAnimationLooping);

            // 执行消耗、冷却、激活
            if (!string.IsNullOrEmpty(self.AbilityNodeGuid))
            {
                self.Context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, "消耗");
                self.Context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, "冷却");
                self.Context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, "激活");
            }

            EventSystem.Instance.Invoke(new GameplayAbilitySpec.OnActivated()
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

            self.PlayAnimation("Stand", true);
            self.GetTimeCueRuntime()?.StopAll();

            // 清理运行中的Effect
            foreach (var effect in self.RunningEffects)
            {
                var e = effect.As();
                if (e == null) continue;
                var effectTarget = e.GetTarget();
                if (effectTarget?.EffectContainer != null)
                    self.RemoveEffectFromContainer(effectTarget.EffectContainer, effect);
                else
                    e.RemoveEffect();
            }
            self.RunningEffects.Clear();
            self.PendingRemoveEffects.Clear();

            // 移除激活时授予的标签
            if (asc != null && !self.Tags.ActivationOwnedTags.IsEmpty)
                asc.OwnedTags.RemoveTags(self.Tags.ActivationOwnedTags);

            EventSystem.Instance.Invoke(new GameplayAbilitySpec.OnEnded()
            {
                Spec = self,
                End = wasCancelled
            });
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
            if (self.AbilityNodeData?.eventOutputPorts == null) return;
            var context = self.Context;
            foreach (var portData in self.AbilityNodeData.eventOutputPorts)
            {
                if (portData.eventType == gameplayEvent)
                    context.ExecuteConnectedNodes(self.SkillId, self.AbilityNodeGuid, portData.PortId);
            }
        }

        // ============ Tick ============

        public static void TickAbility(this GameplayAbilitySpec self, float deltaTime)
        {
            if (self.State != AbilityState.Active) return;

            self.CurrentPlayTime += deltaTime;

            var context = self.Context;

            // 检查时间效果触发
            self.CheckTimeEffectTriggers(self.SkillId, self.AnimationNodeGuid, self.CurrentPlayTime, context);

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

                if (e.EffectNodeData?.durationType != EffectDurationType.Instant)
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

        private static int CancelSiblingAbilitiesWithTags(this GameplayAbilitySpec self, AbilityContainerComponent container, GameplayTagSet tags)
        {
            if (container == null || tags.IsEmpty)
            {
                return 0;
            }

            int cancelledCount = 0;
            for (int i = container.ActiveAbilities.Count - 1; i >= 0; i--)
            {
                var abilityRef = container.ActiveAbilities[i];
                var ability = abilityRef.As();
                if (ability == null || ability == self)
                {
                    continue;
                }

                if (!ability.Tags.AssetTags.HasAnyTags(tags))
                {
                    continue;
                }

                ability.CancelAbility();

                if (container.IsUpdating)
                {
                    if (!container.PendingRemove.Contains(abilityRef))
                    {
                        container.PendingRemove.Add(abilityRef);
                    }
                }
                else
                {
                    container.ActiveAbilities.RemoveAt(i);
                }

                cancelledCount++;
            }

            return cancelledCount;
        }

        private static void ResetTimeEffectRuntime(this GameplayAbilitySpec self)
        {
            var timeEffectRuntime = self.GetTimeEffectRuntime();
            if (timeEffectRuntime == null)
            {
                return;
            }

            foreach (var timeEffect in timeEffectRuntime.TimeEffects)
            {
                timeEffect.HasTriggered = false;
            }
        }

        private static void CheckTimeEffectTriggers(this GameplayAbilitySpec self, string skillId, string animationNodeGuid, float currentPlayTime, SpecExecutionContext context)
        {
            if (context == null)
            {
                return;
            }

            var timeEffectRuntime = self.GetTimeEffectRuntime();
            if (timeEffectRuntime == null)
            {
                return;
            }

            foreach (var timeEffect in timeEffectRuntime.TimeEffects)
            {
                if (!timeEffect.HasTriggered && currentPlayTime >= timeEffect.TriggerTime)
                {
                    timeEffect.HasTriggered = true;
                    context.ExecuteConnectedNodes(skillId, animationNodeGuid, timeEffect.PortId);
                }
            }
        }

        private static bool RemoveEffectFromContainer(this GameplayAbilitySpec self, GameplayEffectContainerComponent container, EntityRef<GameplayEffectSpec> effectRef)
        {
            if (container == null || !container.ActiveEffects.Contains(effectRef))
            {
                return false;
            }

            if (container.IsUpdating)
            {
                if (!container.PendingRemove.Contains(effectRef))
                {
                    container.PendingRemove.Add(effectRef);
                }

                return true;
            }

            var effect = effectRef.As();
            effect?.RemoveEffect();
            container.ActiveEffects.Remove(effectRef);
            if (effect != null && !effect.IsDisposed)
            {
                effect.Dispose();
            }

            return true;
        }

        // ============ 动画 ============

        private static void PlayAnimation(this GameplayAbilitySpec self, string name, bool loop)
        {
            var asc = self.GetASC;
            if (asc?.Owner == null || string.IsNullOrEmpty(name)) return;

            // var animator = asc.Owner.GetComponent<AnimationComponent>();
            // if (animator != null)
            //     animator.PlayAnimation(name, loop);
        }

        private static TimeCueRuntimeComponent GetTimeCueRuntime(this GameplayAbilitySpec self)
        {
            return self.GetComponent<TimeCueRuntimeComponent>();
        }

        private static TimeEffectRuntimeComponent GetTimeEffectRuntime(this GameplayAbilitySpec self)
        {
            return self.GetComponent<TimeEffectRuntimeComponent>();
        }
    }
}
