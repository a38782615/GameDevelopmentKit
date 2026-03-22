using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class BodyCheckComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, EntityRef<EntityBody>> Bodies = new Dictionary<long, EntityRef<EntityBody>>();
        public List<EntityRef<EntityBody>> IndexedBodies = new List<EntityRef<EntityBody>>();
        public List<float3> IndexedPoints = new List<float3>();
        public List<int> CandidateIndices = new List<int>();
        public KDTree KDTree = new KDTree();
        public KDQuery KDQuery = new KDQuery();
        public bool IsTreeDirty = true;
        public float MaxBoundingRadius;
    }
}
