using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// Unity Animation 动画节点数据。
    /// </summary>
    [Serializable]
    public class UnityAnimationNodeData : NodeData
    {
        public GameObject animationPrefab;
        public string animationPrefabPath = "";
        public string animationComponentPath = "";

        public string animationName = "";
        public string animationDuration = "10";
        public bool isAnimationLooping = false;

        public List<TimeEffectData> timeEffects = new List<TimeEffectData>();
        public List<TimeCueData> timeCues = new List<TimeCueData>();
    }
}
