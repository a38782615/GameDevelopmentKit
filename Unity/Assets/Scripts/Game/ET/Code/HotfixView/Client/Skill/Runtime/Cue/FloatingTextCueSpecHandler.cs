using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameplayCueSpec))]
    public partial class FloatingTextCueSpecHandler : ACueHandler
    {
        public FloatingTextCueNodeData GetNode()
        {
            return NodeData as FloatingTextCueNodeData;
        }

        public FloatingTextCueSpec SelfSpec()
        {
            FloatingTextCueSpec selfSpec = Spec.GetComponent<FloatingTextCueSpec>();
            if (selfSpec == null)
            {
                selfSpec = Spec.AddComponent<FloatingTextCueSpec>();
            }

            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
            FloatingTextCueSpec selfSpec = SelfSpec();
            FloatingTextCueNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            selfSpec.PositionSource = nodeData.positionSource;
            selfSpec.PositionBindingName = nodeData.positionBindingName;
            selfSpec.TextType = nodeData.textType;
            selfSpec.FixedText = nodeData.fixedText;
            selfSpec.ContextDataKey = nodeData.contextDataKey;
            selfSpec.TextColor = nodeData.textColor;
            selfSpec.FontSize = nodeData.fontSize;
            selfSpec.Duration = nodeData.duration;
            selfSpec.Offset = nodeData.offset;
            selfSpec.MoveDirection = nodeData.moveDirection;
            Spec.DestroyWithNode = nodeData.destroyWithNode;
        }

        public override void PlayCue(AbilitySystemComponent target)
        {
            FloatingTextCueNodeData nodeData = GetNode();
            FloatingTextCueSpec floatSpec = SelfSpec();
            SpecExecutionContext context = GetContext();
            if (nodeData == null || floatSpec == null || context == null)
            {
                return;
            }

            DamageResult? damageResult = null;
            if (!string.IsNullOrEmpty(floatSpec.ContextDataKey))
            {
                object value = context.GetCustomData<object>(floatSpec.ContextDataKey, null);
                if (value is DamageResult dr)
                {
                    damageResult = dr;
                }
            }

            string displayText = GetDisplayText(damageResult);
            if (string.IsNullOrEmpty(displayText))
            {
                return;
            }

            Color finalColor = floatSpec.TextColor;
            float finalFontSize = floatSpec.FontSize;
            if (damageResult.HasValue)
            {
                if (damageResult.Value.IsMiss)
                {
                    finalColor = nodeData.missColor;
                }
                else if (damageResult.Value.IsCritical)
                {
                    finalColor = nodeData.criticalColor;
                    finalFontSize = nodeData.criticalFontSize;
                }
            }

            Vector3 worldPosition = context.GetPosition(floatSpec.PositionSource, floatSpec.PositionBindingName);
            worldPosition += new Vector3(floatSpec.Offset.x, floatSpec.Offset.y, 0f);

            ActiveCueComponent activeCue = Spec.EnsureActiveCueComponent(false);
            if (activeCue == null)
            {
                return;
            }

            bool played = activeCue.PlayFloatingText(
                displayText,
                worldPosition,
                finalColor,
                finalFontSize,
                floatSpec.Duration,
                floatSpec.TextType);
            if (!played)
            {
                Spec.RemoveActiveCueComponent();
                return;
            }

            Spec.IsRunning = true;
        }

        private string GetDisplayText(DamageResult? damageResult)
        {
            FloatingTextCueSpec floatSpec = SelfSpec();
            SpecExecutionContext context = GetContext();
            if (floatSpec == null)
            {
                return null;
            }

            string text = string.Empty;
            if (damageResult.HasValue)
            {
                DamageResult dr = damageResult.Value;
                if (dr.IsMiss)
                {
                    text = "Miss";
                }
                else
                {
                    text = $"-{Mathf.RoundToInt(dr.Damage)}";
                    if (dr.IsCritical)
                    {
                        text = $"暴击! {text}";
                    }
                }
            }
            else if (!string.IsNullOrEmpty(floatSpec.ContextDataKey) && context != null)
            {
                object value = context.GetCustomData<object>(floatSpec.ContextDataKey, null);
                if (value != null)
                {
                    string numText;
                    if (value is float floatValue)
                    {
                        numText = Mathf.RoundToInt(floatValue).ToString();
                    }
                    else if (value is int intValue)
                    {
                        numText = intValue.ToString();
                    }
                    else
                    {
                        numText = value.ToString();
                    }

                    switch (floatSpec.TextType)
                    {
                        case FloatingTextType.Damage:
                            text = $"-{numText}";
                            break;
                        case FloatingTextType.Heal:
                        case FloatingTextType.Experience:
                        case FloatingTextType.Gold:
                            text = $"+{numText}";
                            break;
                        default:
                            text = numText;
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                text = floatSpec.FixedText;
            }

            return text;
        }

        public override void StopCue()
        {
            if (Spec.GetActiveCue() == null)
            {
                return;
            }

            Spec.RemoveActiveCueComponent();
        }

        public override void Reset()
        {
            FloatingTextCueSpec selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }

            selfSpec.PositionSource = PositionSourceType.ParentInput;
            selfSpec.PositionBindingName = string.Empty;
            selfSpec.TextType = FloatingTextType.Damage;
            selfSpec.FixedText = string.Empty;
            selfSpec.ContextDataKey = "Damage";
            selfSpec.TextColor = Color.white;
            selfSpec.FontSize = 32f;
            selfSpec.Duration = 1.5f;
            selfSpec.Offset = new Vector2(0f, 1f);
            selfSpec.MoveDirection = new Vector2(0f, 1f);
            Spec.DestroyWithNode = false;
        }
    }
}
