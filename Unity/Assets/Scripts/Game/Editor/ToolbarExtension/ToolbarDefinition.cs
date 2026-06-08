using System;

namespace ToolbarExtension
{
    public enum OnGUISide : byte
    {
        Left,
        Right,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ToolbarAttribute : Attribute
    {
        public OnGUISide Side { get; }
        public int Priority { get; }

        public ToolbarAttribute(OnGUISide side, int priority)
        {
            Side = side;
            Priority = priority;
        }
    }
}
