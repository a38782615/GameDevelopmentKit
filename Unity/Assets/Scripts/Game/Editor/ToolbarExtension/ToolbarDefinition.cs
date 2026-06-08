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

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ToolbarButtonAttribute : Attribute
    {
        public OnGUISide Side { get; }
        public int Priority { get; }
        public string Text { get; }
        public string Tooltip { get; }

        public ToolbarButtonAttribute(OnGUISide side, int priority, string text, string tooltip = null)
        {
            Side = side;
            Priority = priority;
            Text = text;
            Tooltip = tooltip ?? text;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ToolbarDropdownAttribute : Attribute
    {
        public OnGUISide Side { get; }
        public int Priority { get; }
        public string Text { get; }
        public string Tooltip { get; }

        public ToolbarDropdownAttribute(OnGUISide side, int priority, string text, string tooltip = null)
        {
            Side = side;
            Priority = priority;
            Text = text;
            Tooltip = tooltip ?? text;
        }
    }
}
