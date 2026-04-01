using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 鏁堟灉Spec鍩虹被
    /// 鐬椂鏁堟灉: 涓嶆巿浜堟爣绛撅紝鐩存帴淇敼BaseValue锛堟案涔咃級
    /// 鎸佺画鏁堟灉: Apply鏃舵巿浜堟爣绛?娣诲姞Modifier锛堜复鏃讹級锛孯emove鏃剁Щ闄?
    /// </summary>
    [ChildOf(typeof(GameplayEffectContainerComponent))]
    public class GameplayEffectSpec : Entity, IAwake, IUpdate, IDestroy
    {
        // ============ 鍩虹鏍囪瘑 ============
        public string SkillId;
        public string NodeGuid;
        public EntityRef<SpecExecutionContext> Context;
        public EntityRef<AbilitySystemComponent> Source;
        public EntityRef<AbilitySystemComponent> Target;
        public int Level = 1;
        public int StackCount = 1;
        public bool IsRunning;
        public bool IsCancelled;

        // ============ 杩愯鏃舵暟鎹紙鍙淇敼锛?============
        public EffectTagContainer Tags;
        public float Duration;
        public float Period;
        public List<AttributeModifier> Modifiers = new List<AttributeModifier>();
        public Dictionary<string, float> SetByCallerValues = new Dictionary<string, float>();
        public Dictionary<int, float> SnapshotValues = new Dictionary<int, float>();

        // ============ 杩愯鏃剁姸鎬?============
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
