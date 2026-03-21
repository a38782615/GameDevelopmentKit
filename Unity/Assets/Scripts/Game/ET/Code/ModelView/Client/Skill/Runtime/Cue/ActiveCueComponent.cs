using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// GameplayCue 的运行态组件，负责持有当前播放中的特效、音频和飘字。
    /// </summary>
    [ComponentOf(typeof(GameplayCueSpec))]
    public class ActiveCueComponent : Entity, IAwake, IDestroy
    {
        public bool IsExpired;
        public float StartTime;
        public float Duration;
        public float ElapsedTime;
        public bool IsLooping;
        public int FloatingTextHandle;
        public EntityRef<UGFEntityEffect> AttachedEffectEntity;
        public AudioSource AttachedAudioSource;
    }
}
