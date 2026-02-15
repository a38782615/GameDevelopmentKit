using System;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 音效Cue节点数据
    /// </summary>
    [Serializable]
    public class SoundCueNodeData : CueNodeData
    {
        public AudioClip soundClip;
        public float soundVolume = 1f;
        public bool soundLoop = false;

    }
}
