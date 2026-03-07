using System;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]

    public static partial class GameplayEffectSpecSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayEffectSpec self)
        {
        }

        [EntitySystem]
        private static void Update(this GameplayEffectSpec self)
        {
            // Tick 由容器驱动
        }

        [EntitySystem]
        private static void Destroy(this GameplayEffectSpec self)
        {
            self.Modifiers?.Clear();
            self.SetByCallerValues?.Clear();
            self.SnapshotValues?.Clear();
            self.TriggeredCueIds?.Clear();
        }

        // ============ 初始化 ============

        public static void InitEffect(this GameplayEffectSpec self, string skillId, string nodeGuid, SpecExecutionContext context)
        {
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.Context = context;
            self.Source = context.Caster;
            self.Level = context.AbilityLevel;
            self.IsRunning = false;
            self.IsCancelled = false;
            self.IsApplied = false;
            self.IsExpired = false;
            self.WasRefreshed = false;
            self.TriggeredCueIds.Clear();

            var effectData = self.EffectNodeData;
            if (effectData != null)
                self.Tags = new EffectTagContainer(effectData);

            var source = self.GetSource();
            if (source?.Attributes != null)
                self.SnapshotValues = source.Attributes.CreateSnapshot();

            self.Duration = FormulaEvaluator.EvaluateSimple(effectData?.duration, 0f);
            self.Period = FormulaEvaluator.EvaluateSimple(effectData?.period, 1f);

            if (effectData?.attributeModifiers != null)
            {
                self.Modifiers.Clear();
                foreach (var modData in effectData.attributeModifiers)
                    self.Modifiers.Add(AttributeModifier.FromData(modData));
            }

            self.OnInitialize();
        }

        /// <summary>
        /// 子类可重写的初始化钩子
        /// </summary>
        public static void OnInitialize(this GameplayEffectSpec self)
        {
            // 基类空实现，子类通过扩展方法覆盖
        }

        // ============ 执行入口 ============

        public static void Execute(this GameplayEffectSpec self)
        {
            var context = self.GetContext();
            if (context == null) return;

            var target = self.GetEffectTarget(context);
            if (target == null || !self.CanApplyTo(target)) return;

            self.IsRunning = true;
            var effectData = self.EffectNodeData;

            if (effectData?.durationType == EffectDurationType.Instant)
            {
                self.ExecuteInitialFlow(target, context);
                self.ExecuteCompleteFlow(context);
                self.IsRunning = false;
            }
            else
            {
                var container = target.EffectContainer;
                var existingEffect = container?.FindStackableEffect(self);

                if (existingEffect != null)
                {
                    var existingData = existingEffect.EffectNodeData;
                    int stackLimit = existingData?.stackLimit ?? 0;
                    bool isAtStackLimit = stackLimit > 0 && existingEffect.StackCount >= stackLimit;
                    if (isAtStackLimit)
                    {
                        var overflowPolicy = existingData?.stackOverflowPolicy ?? StackOverflowPolicy.DenyApplication;
                        if (overflowPolicy == StackOverflowPolicy.AllowOverflowEffect)
                            context.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "溢出");
                        if (overflowPolicy == StackOverflowPolicy.DenyApplication)
                        {
                            existingEffect.RefreshEffect();
                            self.WasRefreshed = true;
                            return;
                        }
                    }
                    existingEffect.AddStack();
                    self.WasRefreshed = true;
                }
                else
                {
                    self.ExecuteInitialFlow(target, context);

                    if (effectData?.isPeriodic == true && effectData?.executeOnApplication == true)
                        self.ExecutePeriodicFlow(context);

                    if (effectData?.durationType == EffectDurationType.Duration && self.Duration <= 0)
                    {
                        self.ExecuteCompleteFlow(context);
                        self.IsRunning = false;
                    }
                    else
                    {
                        container?.AddEffect(self);
                        self.Target = target;
                        self.IsApplied = true;
                        self.ActivationTime = UnityEngine.Time.time;

                        if (!self.Tags.GrantedTags.IsEmpty)
                            target.OwnedTags.AddTags(self.Tags.GrantedTags);
                        if (!self.Tags.RemoveGameplayEffectsWithTags.IsEmpty)
                            target.RemoveActiveEffectsWithTags(self.Tags.RemoveGameplayEffectsWithTags);

                        self.RegisterTagListener();

                        if (target.Attributes != null && self.Modifiers?.Count > 0)
                        {
                            var modContext = self.CreateCalculationContext(target);
                            foreach (var modifier in self.Modifiers)
                            {
                                var attribute = target.Attributes.GetAttribute(modifier.TargetAttrType);
                                if (attribute != null)
                                {
                                    attribute.AddModifier(modifier, self);
                                    attribute.Recalculate(modContext);
                                }
                            }
                        }
                    }
                }
            }
        }

        // ============ 三大流程 ============

        private static void ExecuteInitialFlow(this GameplayEffectSpec self, AbilitySystemComponent target, SpecExecutionContext ctx)
        {
            if (self.EffectNodeData?.durationType == EffectDurationType.Instant && target?.Attributes != null && self.Modifiers?.Count > 0)
            {
                var calcContext = self.CreateCalculationContext(target);
                foreach (var modifier in self.Modifiers)
                {
                    var attribute = target.Attributes.GetAttribute(modifier.TargetAttrType);
                    if (attribute == null) continue;
                    float magnitude = modifier.CalculateMagnitude(calcContext);
                    switch (modifier.Operation)
                    {
                        case ModifierOperation.Add: attribute.BaseValue += magnitude; break;
                        case ModifierOperation.Multiply: attribute.BaseValue *= magnitude; break;
                        case ModifierOperation.Divide: if (Math.Abs(magnitude) > 0.0001f) attribute.BaseValue /= magnitude; break;
                        case ModifierOperation.Override: attribute.BaseValue = magnitude; break;
                    }
                }
            }

            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "初始效果");
        }

        private static void ExecutePeriodicFlow(this GameplayEffectSpec self, SpecExecutionContext ctx)
        {
            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "每周期执行");
        }

        private static void ExecuteCompleteFlow(this GameplayEffectSpec self, SpecExecutionContext ctx)
        {
            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "完成效果");
        }

        // ============ Tick ============

        public static void TickEffect(this GameplayEffectSpec self, float deltaTime)
        {
            if (self.IsExpired || !self.IsApplied) return;

            self.ElapsedTime += deltaTime;
            var effectData = self.EffectNodeData;
            var ctx = self.GetContext();

            if (effectData?.isPeriodic == true && self.Period > 0)
            {
                self.PeriodTimer += deltaTime;
                if (self.PeriodTimer >= self.Period)
                {
                    self.PeriodTimer -= self.Period;
                    self.ExecutePeriodicFlow(ctx);
                }
            }

            if (effectData?.durationType == EffectDurationType.Duration && self.Duration > 0 && self.ElapsedTime >= self.Duration)
                self.Expire();
        }

        // ============ 刷新 ============

        public static void RefreshEffect(this GameplayEffectSpec self)
        {
            var effectData = self.EffectNodeData;
            if (effectData?.stackDurationRefreshPolicy == StackDurationRefreshPolicy.RefreshOnSuccessfulApplication)
            {
                self.ElapsedTime = 0f;
                self.ActivationTime = UnityEngine.Time.time;
            }
            if (effectData?.stackPeriodResetPolicy == StackPeriodResetPolicy.ResetOnSuccessfulApplication)
                self.PeriodTimer = 0f;

            self.WasRefreshed = true;
            var ctx = self.GetContext();
            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "刷新时");
        }

        // ============ 过期和移除 ============

        public static void Expire(this GameplayEffectSpec self)
        {
            if (self.IsExpired) return;

            var policy = self.EffectNodeData?.stackExpirationPolicy ?? StackExpirationPolicy.ClearEntireStack;
            switch (policy)
            {
                case StackExpirationPolicy.ClearEntireStack:
                    self.IsExpired = true;
                    self.RemoveEffect();
                    break;
                case StackExpirationPolicy.RemoveSingleStackAndRefreshDuration:
                    if (self.StackCount > 1)
                    {
                        self.StackCount--;
                        self.ActivationTime = UnityEngine.Time.time;
                        self.ElapsedTime = 0f;
                        self.RecalculateModifiers();
                    }
                    else
                    {
                        self.IsExpired = true;
                        self.RemoveEffect();
                    }
                    break;
                case StackExpirationPolicy.RefreshDuration:
                    if (self.StackCount > 0)
                    {
                        self.ActivationTime = UnityEngine.Time.time;
                        self.ElapsedTime = 0f;
                    }
                    else
                    {
                        self.IsExpired = true;
                        self.RemoveEffect();
                    }
                    break;
            }
        }

        public static void RemoveEffect(this GameplayEffectSpec self)
        {
            // 取消Cue
            if (self.TriggeredCueIds.Count > 0)
            {
                var asc = self.GetSource();
                var cueContainer = asc?.GetComponent<GameplayCueContainerComponent>();
                if (cueContainer != null)
                {
                    foreach (var cueId in self.TriggeredCueIds)
                    {
                        var cue = cueContainer.GetChild<GameplayCueSpec>(cueId);
                        if (cue != null && cue.IsRunning)
                            cue.CancelCue();
                    }
                }
                self.TriggeredCueIds.Clear();
            }

            // 移除属性修改器
            var target = self.GetTarget();
            if (target?.Attributes != null)
            {
                foreach (var modifier in self.Modifiers)
                {
                    var attribute = target.Attributes.GetAttribute(modifier.TargetAttrType);
                    if (attribute != null)
                    {
                        attribute.RemoveModifiersFromSource(self);
                        attribute.Recalculate();
                    }
                }
            }

            // 移除标签
            if (target != null && !self.Tags.GrantedTags.IsEmpty)
                target.OwnedTags.RemoveTags(self.Tags.GrantedTags);

            self.UnregisterTagListener();

            var ctx = self.GetContext();
            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "全部移除后");
            self.ExecuteCompleteFlow(ctx);

            self.IsExpired = true;
            self.IsRunning = false;
        }

        public static void CancelEffect(this GameplayEffectSpec self)
        {
            if (!self.IsRunning && !self.IsApplied) return;

            self.IsCancelled = true;
            self.IsRunning = false;

            // 取消Cue
            var asc = self.GetSource();
            var cueContainer = asc?.GetComponent<GameplayCueContainerComponent>();
            if (cueContainer != null && self.TriggeredCueIds.Count > 0)
            {
                foreach (var cueId in self.TriggeredCueIds)
                {
                    var cue = cueContainer.GetChild<GameplayCueSpec>(cueId);
                    if (cue != null && cue.IsRunning)
                        cue.CancelCue();
                }
                self.TriggeredCueIds.Clear();
            }

            // 移除属性修改器
            var target = self.GetTarget();
            if (target?.Attributes != null)
            {
                foreach (var modifier in self.Modifiers)
                {
                    var attribute = target.Attributes.GetAttribute(modifier.TargetAttrType);
                    if (attribute != null)
                    {
                        attribute.RemoveModifiersFromSource(self);
                        attribute.Recalculate();
                    }
                }
            }

            if (target != null && !self.Tags.GrantedTags.IsEmpty)
                target.OwnedTags.RemoveTags(self.Tags.GrantedTags);

            var ctx = self.GetContext();
            ctx.ExecuteConnectedNodes(self.SkillId, self.NodeGuid, "全部移除后");

            self.IsExpired = true;
        }

        // ============ 堆叠 ============

        public static bool AddStack(this GameplayEffectSpec self, int count = 1)
        {
            var effectData = self.EffectNodeData;
            int stackLimit = effectData?.stackLimit ?? 0;
            var overflowPolicy = effectData?.stackOverflowPolicy ?? StackOverflowPolicy.DenyApplication;

            if (stackLimit > 0 && self.StackCount >= stackLimit && overflowPolicy == StackOverflowPolicy.DenyApplication)
            {
                self.RefreshEffect();
                return false;
            }

            int newStack = stackLimit > 0 ? Math.Min(self.StackCount + count, stackLimit) : self.StackCount + count;
            if (newStack == self.StackCount) return false;

            self.StackCount = newStack;
            self.RefreshEffect();
            self.RecalculateModifiers();
            return true;
        }

        public static bool RemoveStack(this GameplayEffectSpec self, int count = 1)
        {
            int newStack = Math.Max(0, self.StackCount - count);
            if (newStack == self.StackCount) return false;

            self.StackCount = newStack;
            if (newStack == 0) self.Expire();
            else self.RecalculateModifiers();
            return true;
        }

        private static void RecalculateModifiers(this GameplayEffectSpec self)
        {
            var target = self.GetTarget();
            if (target?.Attributes == null) return;

            foreach (var modifier in self.Modifiers)
            {
                var attribute = target.Attributes.GetAttribute(modifier.TargetAttrType);
                attribute?.MarkDirty();
                attribute?.Recalculate();
            }
        }

        // ============ 标签监听 ============

        private static void RegisterTagListener(this GameplayEffectSpec self)
        {
            var target = self.GetTarget();
            if (target?.OwnedTags == null) return;

            if (!self.Tags.OngoingRequiredTags.IsEmpty)
                target.OwnedTags.OnTagAdded += self.OnOwnerTagAdded;
        }

        private static void UnregisterTagListener(this GameplayEffectSpec self)
        {
            var target = self.GetTarget();
            if (target?.OwnedTags == null) return;

            if (!self.Tags.OngoingRequiredTags.IsEmpty)
                target.OwnedTags.OnTagAdded -= self.OnOwnerTagAdded;
        }

        private static void OnOwnerTagAdded(this GameplayEffectSpec self, GameplayTag tag)
        {
            if (self.Tags.OngoingRequiredTags.HasTag(tag))
                self.RemoveEffect();
        }

        // ============ Cue管理 ============

        public static void RegisterTriggeredCue(this GameplayEffectSpec self, long cueId)
        {
            if (cueId != 0 && !self.TriggeredCueIds.Contains(cueId))
                self.TriggeredCueIds.Add(cueId);
        }

        // ============ 辅助方法 ============

        private static AbilitySystemComponent GetEffectTarget(this GameplayEffectSpec self, SpecExecutionContext context)
        {
            var nodeData = self.NodeData;
            if (nodeData == null) return context?.GetMainTarget();
            return context?.GetTargetByType(nodeData.targetType);
        }

        public static bool CanApplyTo(this GameplayEffectSpec self, AbilitySystemComponent target)
        {
            if (target == null) return false;
            if (!self.Tags.ApplicationRequiredTags.IsEmpty && !target.HasAllTags(self.Tags.ApplicationRequiredTags)) return false;
            if (!self.Tags.ApplicationImmunityTags.IsEmpty && target.HasAnyTags(self.Tags.ApplicationImmunityTags)) return false;
            return true;
        }

        public static ModifierCalculationContext CreateCalculationContext(this GameplayEffectSpec self, AbilitySystemComponent target)
        {
            var source = self.GetSource();
            return new ModifierCalculationContext
            {
                SourceAttributes = source?.Attributes,
                TargetAttributes = target?.Attributes,
                SnapshotValues = self.SnapshotValues,
                EffectLevel = self.Level
            };
        }

        public static void ResetEffect(this GameplayEffectSpec self)
        {
            self.IsRunning = false;
            self.IsCancelled = false;
            self.IsApplied = false;
            self.IsExpired = false;
            self.WasRefreshed = false;
            self.ElapsedTime = 0f;
            self.PeriodTimer = 0f;
            self.StackCount = 1;
            self.Modifiers?.Clear();
            self.SetByCallerValues?.Clear();
            self.SnapshotValues?.Clear();
            self.TriggeredCueIds?.Clear();
            EffectDispatcherComponent.Instance.Get(self.HandName).Reset();
        }


        /// <summary>
        /// 获取施法者ASC
        /// </summary>
        public static AbilitySystemComponent GetSource(this GameplayEffectSpec self)
        {
            return self.GetParent<GameplayEffectContainerComponent>().GetASC;
        }

        /// <summary>
        /// 获取目标ASC
        /// </summary>
        public static AbilitySystemComponent GetTarget(this GameplayEffectSpec self)
        {
            return self.Target;
        }

        /// <summary>
        /// 获取执行上下文
        /// </summary>
        public static SpecExecutionContext GetContext(this GameplayEffectSpec self)
        {
            return self.Context;
        }

        /// <summary>
        /// 获取所属效果容器
        /// </summary>
        public static GameplayEffectContainerComponent GetContainer(this GameplayEffectSpec self)
        {
            return self.GetParent<GameplayEffectContainerComponent>();
        }

        // ============ SetByCaller ============
        public static void SetSetByCallerValue(this GameplayEffectSpec self, string key, float value)
        {
            self.SetByCallerValues[key] = value;
        }
        public static float GetSetByCallerValue(this GameplayEffectSpec self, string key, float defaultValue = 0f)
        {
            return self.SetByCallerValues.TryGetValue(key, out float value) ? value : defaultValue;
        }
    }
}
