using System;
using Unity.Mathematics;

namespace ET.Geometry
{
    [Serializable]
    public struct RectangleF : IEquatable<RectangleF>
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleF(float2 location, float2 size)
        {
            X = location.x;
            Y = location.y;
            Width = size.x;
            Height = size.y;
        }

        public static RectangleF Empty => default;

        public float Left => X;

        public float Top => Y;

        public float Right => X + Width;

        public float Bottom => Y + Height;

        public float2 Location
        {
            get => new float2(X, Y);
            set
            {
                X = value.x;
                Y = value.y;
            }
        }

        public float2 Size
        {
            get => new float2(Width, Height);
            set
            {
                Width = value.x;
                Height = value.y;
            }
        }

        public float2 Center => new float2(X + Width * 0.5f, Y + Height * 0.5f);

        public bool IsEmpty => Width <= 0f || Height <= 0f;

        public bool Contains(float x, float y)
        {
            return x >= Left && x < Right && y >= Top && y < Bottom;
        }

        public bool Contains(float2 point)
        {
            return Contains(point.x, point.y);
        }

        public bool Contains(RectangleF rect)
        {
            return rect.Left >= Left && rect.Right <= Right && rect.Top >= Top && rect.Bottom <= Bottom;
        }

        public bool IntersectsWith(RectangleF rect)
        {
            return rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom;
        }

        public void Inflate(float width, float height)
        {
            X -= width;
            Y -= height;
            Width += width * 2f;
            Height += height * 2f;
        }

        public void Inflate(float2 size)
        {
            Inflate(size.x, size.y);
        }

        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        public void Offset(float2 offset)
        {
            Offset(offset.x, offset.y);
        }

        public float2 ClosestPoint(float2 point)
        {
            return math.clamp(point, new float2(Left, Top), new float2(Right, Bottom));
        }

        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
        {
            return new RectangleF(left, top, right - left, bottom - top);
        }

        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float left = math.max(a.Left, b.Left);
            float top = math.max(a.Top, b.Top);
            float right = math.min(a.Right, b.Right);
            float bottom = math.min(a.Bottom, b.Bottom);

            if (right <= left || bottom <= top)
            {
                return Empty;
            }

            return FromLTRB(left, top, right, bottom);
        }

        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            float left = math.min(a.Left, b.Left);
            float top = math.min(a.Top, b.Top);
            float right = math.max(a.Right, b.Right);
            float bottom = math.max(a.Bottom, b.Bottom);
            return FromLTRB(left, top, right, bottom);
        }

        public bool Equals(RectangleF other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is RectangleF other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
        }

        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !left.Equals(right);
        }
    }
}
