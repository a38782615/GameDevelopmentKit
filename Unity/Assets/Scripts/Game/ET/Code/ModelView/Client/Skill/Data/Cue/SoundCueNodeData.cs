using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 音效Cue节点数据
    /// </summary>
    [Serializable]
    public class SoundCueNodeData : CueNodeData
    {
        public AudioClip soundClip;
        public string soundClipPath = "";
        public float soundVolume = 1f;
        public bool soundLoop = false;

    }
}
