namespace ET.Client
{
    /// <summary>
    /// Cue Spec 鍩虹被锛岃礋璐ｆ壙杞借妭鐐归厤缃拰鎾斁鏃剁殑涓婁笅鏂囥€?
    /// </summary>
    [ChildOf(typeof(GameplayCueContainerComponent))]
    public class GameplayCueSpec : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 鎶€鑳?ID锛岀敤浜庝粠鏁版嵁涓績鑾峰彇鑺傜偣鏁版嵁銆?
        /// </summary>
        public string SkillId;

        /// <summary>
        /// 鑺傜偣 Guid銆?
        /// </summary>
        public string NodeGuid;

        /// <summary>
        /// 鎵ц涓婁笅鏂囨墍灞炵殑 AbilitySpec銆?
        /// </summary>
        public EntityRef<GameplayAbilitySpec> ContextOwner;

        /// <summary>
        /// 瀹為檯瑙﹀彂璇?Cue 鐨勬墽琛屼笂涓嬫枃銆?
        /// </summary>
        public EntityRef<SpecExecutionContext> Context;

        /// <summary>
        /// 鏄惁姝ｅ湪鎾斁銆?
        /// </summary>
        public bool IsRunning;

        /// <summary>
        /// 鏄惁宸茶鍙栨秷銆?
        /// </summary>
        public bool IsCancelled;

        /// <summary>
        /// 鏄惁闅忚妭鐐归攢姣併€?
        /// </summary>
        public bool DestroyWithNode;

        /// <summary>
        /// 鏍囩瀹瑰櫒銆?
        /// </summary>
        public CueTagContainer Tags;

        /// <summary>
        /// 褰撳墠婵€娲荤殑杩愯鎬佺粍浠躲€?
        /// </summary>
        public EntityRef<ActiveCueComponent> ActiveCueComponent;

        public string HandName;
    }
}
