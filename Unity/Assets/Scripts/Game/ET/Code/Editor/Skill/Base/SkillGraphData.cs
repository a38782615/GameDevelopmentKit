using System.Collections.Generic;
using UnityEngine;

namespace ET.Client.Editor
{
    [CreateAssetMenu(fileName = "SkillGraph", menuName = "SkillEditor/SkillGraph")]
    public class SkillGraphData : ScriptableObject
    {
        public string SkillId;
        [SerializeReference]
        public List<NodeData> nodes = new List<NodeData>();
        public List<ConnectionData> connections = new List<ConnectionData>();
    }
}
