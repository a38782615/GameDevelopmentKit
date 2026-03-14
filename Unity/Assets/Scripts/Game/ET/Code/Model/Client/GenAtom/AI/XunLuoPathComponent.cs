using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class XunLuoPathComponent: Entity, IAwake
    {
        public float3[] path = global::ET.ModeDefine.Is2D
                ? new float3[] { new float3(0f, 0f, 0f), new float3(20f, 0f, 0f), new float3(20f, 20f, 0f), new float3(0f, 20f, 0f) }
                : new float3[] { new float3(0f, 0f, 0f), new float3(20f, 0f, 0f), new float3(20f, 0f, 20f), new float3(0f, 0f, 20f) };
        public int Index;
    }
}
