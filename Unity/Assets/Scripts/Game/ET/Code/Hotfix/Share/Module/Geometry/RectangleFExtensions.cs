using Unity.Mathematics;

namespace ET.Geometry
{
    public static class RectangleFExtensions
    {
        public static float2 Min(this RectangleF rect)
        {
            return new float2(rect.Left, rect.Top);
        }

        public static float2 Max(this RectangleF rect)
        {
            return new float2(rect.Right, rect.Bottom);
        }

        public static RectangleF Expand(this RectangleF rect, float amount)
        {
            rect.Inflate(amount, amount);
            return rect;
        }

        public static RectangleF Expand(this RectangleF rect, float2 amount)
        {
            rect.Inflate(amount.x, amount.y);
            return rect;
        }

        public static RectangleF Translate(this RectangleF rect, float2 offset)
        {
            rect.Offset(offset);
            return rect;
        }

        public static float2 GetClosestPoint(this RectangleF rect, float2 point)
        {
            return rect.ClosestPoint(point);
        }

        public static float GetDistanceSq(this RectangleF rect, float2 point)
        {
            float2 closest = rect.ClosestPoint(point);
            return math.distancesq(closest, point);
        }

        public static bool OverlapsCircle(this RectangleF rect, float2 center, float radius)
        {
            return rect.GetDistanceSq(center) <= radius * radius;
        }

        public static RectangleF FromCenter(float2 center, float2 size)
        {
            float2 half = size * 0.5f;
            return new RectangleF(center - half, size);
        }

        public static RectangleF ToRectangleF(this float2 center, float width, float height)
        {
            return FromCenter(center, new float2(width, height));
        }

        public static RectangleF ToRectangleF(this float2 center, float2 size)
        {
            return FromCenter(center, size);
        }
    }
}
