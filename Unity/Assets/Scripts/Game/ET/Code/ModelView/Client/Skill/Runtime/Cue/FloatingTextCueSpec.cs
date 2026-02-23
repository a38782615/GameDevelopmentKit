
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 飘字Cue Spec
    /// 显示伤害、治疗、状态等飘字
    /// </summary>
    [ComponentOf(typeof(GameplayCueSpec))]
    public partial class FloatingTextCueSpec : Entity, IAwake
    {
        // ============ 动态数据 ============

        public PositionSourceType PositionSource { get; set; }
        public string PositionBindingName { get; set; }
        public FloatingTextType TextType { get; set; }
        public string FixedText { get; set; }
        public string ContextDataKey { get; set; }
        public Color TextColor { get; set; }
        public float FontSize { get; set; }
        public float Duration { get; set; }
        public Vector2 Offset { get; set; }
        public Vector2 MoveDirection { get; set; }
    }
}
