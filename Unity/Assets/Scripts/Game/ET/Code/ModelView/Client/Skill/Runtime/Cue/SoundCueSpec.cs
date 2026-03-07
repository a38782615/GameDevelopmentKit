using UnityEngine;


namespace ET.Client
{
    /// <summary>
    /// 音效Cue Spec
    /// 播放音效
    /// </summary>
    [ComponentOf(typeof(GameplayCueSpec))]
    public class SoundCueSpec : Entity, IAwake
    {
        // ============ 动态数据 ============

        /// <summary>
        /// 音效资源
        /// </summary>
        public AudioClip SoundClip { get; set; }

        /// <summary>
        /// 音量
        /// </summary>
        public float SoundVolume { get; set; }

        /// <summary>
        /// 是否循环
        /// </summary>
        public bool SoundLoop { get; set; }

    }
}
