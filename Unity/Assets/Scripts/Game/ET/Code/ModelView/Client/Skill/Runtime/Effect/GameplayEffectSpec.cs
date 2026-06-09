using System;
using System.Collections.Generic;

namespace ET.Client
{
    [ChildOf(typeof(GameplayEffectContainerComponent))]
    public class GameplayEffectSpec : Entity, IAwake, IUpdate, IDestroy
    {
        public string SkillId;
        public string NodeGuid;
        public EntityRef<SpecExecutionContext> Context;
        public EntityRef<AbilitySystemComponent> Source;
        public EntityRef<AbilitySystemComponent> Target;
        public int Level = 1;
        public int StackCount = 1;
        public bool IsRunning;
        public bool IsCancelled;

        public EffectTagContainer Tags;
        public float Duration;
        public float Period;
        public List<AttributeModifier> Modifiers = new List<AttributeModifier>();
        public Dictionary<string, float> SetByCallerValues = new Dictionary<string, float>();
        public Dictionary<int, float> SnapshotValues = new Dictionary<int, float>();

        public float ActivationTime;
        public bool IsApplied;
        public bool IsExpired;
        public bool WasRefreshed;
        public bool IsRemoved;
        public bool HasExecutedCompleteFlow;
        public float ElapsedTime;
        public float PeriodTimer;
        public List<EntityRef<GameplayCueSpec>> TriggeredCueIds = new List<EntityRef<GameplayCueSpec>>();

        public string HandName;
    }
}
