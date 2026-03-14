using Unity.Mathematics;

namespace ET
{
    public static class MoveModeExtensions
    {
        public static float2 ToPlanar(this float3 value)
        {
            return global::ET.ModeDefine.Is2D ? value.xy : new float2(value.x, value.z);
        }

        public static float3 ToModePosition(this float2 value)
        {
            return global::ET.ModeDefine.Is2D ? new float3(value.x, value.y, 0f) : new float3(value.x, 0f, value.y);
        }

        public static float3 ToModeDirection(this float2 value)
        {
            return global::ET.ModeDefine.Is2D ? new float3(value.x, value.y, 0f) : new float3(value.x, 0f, value.y);
        }

        public static quaternion ToPlanarRotation(this float2 direction)
        {
            if (math.lengthsq(direction) < 0.0001f)
            {
                return quaternion.identity;
            }

            if (global::ET.ModeDefine.Is2D)
            {
                return quaternion.RotateZ(math.atan2(direction.y, direction.x));
            }

            return quaternion.LookRotation(new float3(direction.x, 0f, direction.y), math.up());
        }
    }
}
