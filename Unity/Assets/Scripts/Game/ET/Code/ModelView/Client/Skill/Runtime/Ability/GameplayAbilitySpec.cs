using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 鎶€鑳借繍琛屾椂瀹炰緥 - 瀵瑰簲GAS鐨凢GameplayAbilitySpec
    /// 姣忎釜鎺堜簣鐨勬妧鑳介兘鏈変竴涓猄pec瀹炰緥锛屽寘鍚繍琛屾椂鐘舵€佸拰鎵ц閫昏緫
    /// </summary>
    [ChildOf(typeof(AbilityContainerComponent))]
    public partial class GameplayAbilitySpec : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 鎶€鑳絀D锛堢敤浜庝粠鏁版嵁涓績鑾峰彇鏁版嵁锛?
        /// </summary>
        public string SkillId;

        /// <summary>
        /// 鎷ユ湁姝ゆ妧鑳界殑ASC鐨凟ntity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> Owner;

        /// <summary>
        /// 褰撳墠鐘舵€?
        /// </summary>
        public AbilityState State = AbilityState.Inactive;

        /// <summary>
        /// 鎶€鑳界瓑绾?
        /// </summary>
        public int Level = 1;

        /// <summary>
        /// 鏍囩瀹瑰櫒
        /// </summary>
        public AbilityTagContainer Tags;

        /// <summary>
        /// 婵€娲绘椂闂?
        /// </summary>
        public float ActivationTime;

        /// <summary>
        /// 鏄惁姝ｅ湪婵€娲?
        /// </summary>
        public bool IsActive => State == AbilityState.Active;

        /// <summary>
        /// 鏄惁姝ｅ湪鎵ц
        /// </summary>
        public bool IsRunning;

        /// <summary>
        /// Ability鑺傜偣鐨刧uid
        /// </summary>
        public string AbilityNodeGuid;

        public List<long> LinkedCardInstanceIds = new List<long>();

        public long ActivatingCardInstanceId;

        public float ActivatingCardResolvedCostMp;

        // ============ 鎵ц鐩稿叧 ============
        public EntityRef<SpecExecutionContext> Context;

        /// <summary>
        /// 姝ｅ湪鎵ц鐨凟ffect鍒楄〃锛堟妧鑳界鐞嗘寔缁?鍛ㄦ湡Effect锛?
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> RunningEffects = new List<EntityRef<GameplayEffectSpec>>();

        /// <summary>
        /// 寰呯Щ闄ょ殑Effect
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> PendingRemoveEffects = new List<EntityRef<GameplayEffectSpec>>();

        // ============ 缂撳瓨鐨勮妭鐐规暟鎹?============

        /// <summary>
        /// 娑堣€楄妭鐐笹uid
        /// </summary>
        public string CostNodeGuid;

        /// <summary>
        /// 鍐峰嵈鑺傜偣Guid
        /// </summary>
        public string CooldownNodeGuid;

        /// <summary>
        /// 鍔ㄧ敾鑺傜偣Guid
        /// </summary>
        public string AnimationNodeGuid;

        // ============ 鍔ㄧ敾鐩稿叧 ============

        /// <summary>
        /// 鍔ㄧ敾鍚嶇О
        /// </summary>
        public string AnimationName;

        public string AnimationComponentPath;

        /// <summary>
        /// 鍔ㄧ敾鏃堕暱
        /// </summary>
        public float AnimationDuration;

        /// <summary>
        /// 鏄惁寰幆鎾斁鍔ㄧ敾
        /// </summary>
        public bool IsAnimationLooping;

        /// <summary>
        /// 褰撳墠鎾斁鏃堕棿
        /// </summary>
        public float CurrentPlayTime;

        // ============ 浜嬩欢 ============

        public struct OnActivated
        {
            public GameplayAbilitySpec Spec;
        }

        public struct OnEnded
        {
            public GameplayAbilitySpec Spec;
            public bool End;
        }

        // ============ 渚挎嵎璁块棶 ============

        /// <summary>
        /// 鑾峰彇鎵€灞濧SC
        /// </summary>
        public AbilitySystemComponent GetASC => this.GetParent<AbilityContainerComponent>()?.GetASC;

        /// <summary>
        /// 鑾峰彇鏃堕棿Cue杩愯鏃剁粍浠?
        /// </summary>
        public TimeCueRuntimeComponent GetTimeCueRuntime => this.GetComponent<TimeCueRuntimeComponent>();

        /// <summary>
        /// 鑾峰彇鏃堕棿鏁堟灉杩愯鏃剁粍浠?
        /// </summary>
        public TimeEffectRuntimeComponent GetTimeEffectRuntime => this.GetComponent<TimeEffectRuntimeComponent>();
    }
}
