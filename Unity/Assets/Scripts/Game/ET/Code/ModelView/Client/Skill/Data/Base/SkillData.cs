using System.Collections.Generic;
using UnityEngine;
namespace ET
{
    public class SkillData : Object
    {
        public string SkillId;
        [SerializeReference]
        public List<NodeData> nodes = new List<NodeData>();
        public List<ConnectionData> connections = new List<ConnectionData>();
    }
}
