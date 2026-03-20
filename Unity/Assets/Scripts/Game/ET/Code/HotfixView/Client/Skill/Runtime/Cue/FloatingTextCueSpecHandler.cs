
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 飘字Cue Spec
    /// 显示伤害、治疗、状态等飘字
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]
    public partial class FloatingTextCueSpecHandler : ACueHandler
    {
        public FloatingTextCueNodeData GetNode()
        {
            var nodeData = NodeData as FloatingTextCueNodeData;
            return nodeData;
        }
        public FloatingTextCueSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<FloatingTextCueSpec>();
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
        // ============ 初始化 ============

        public override void OnInitialize()
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData != null)
            {
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
        }

        // ============ 执行 ============

        public override void PlayCue(AbilitySystemComponent target)
        {
            var nodeData = GetNode();
            if (nodeData == null)
            {
#if UNITY_EDITOR
                SkillDiagFileLogger.Log("[DiagFloatText] skip nodeData=null");
#endif
                return;
            }
            var floatSpec = SelfSpec();
            if (floatSpec == null)
            {
#if UNITY_EDITOR
                SkillDiagFileLogger.Log("[DiagFloatText] skip floatSpec=null");
#endif
                return;
            }
            var Context = GetContext();
            // 获取 DamageResult（如果有）
            DamageResult? damageResult = null;
            if (!string.IsNullOrEmpty(floatSpec.ContextDataKey) && Context != null)
            {
                var value = Context.GetCustomData<object>(floatSpec.ContextDataKey, null);
                if (value is DamageResult dr)
                {
                    damageResult = dr;
                }
            }

            // 获取显示文本
            string displayText = GetDisplayText(damageResult);
#if UNITY_EDITOR
            SkillDiagFileLogger.Log(
                $"[DiagFloatText] play target={target?.Owner?.name ?? "null"} cueTargetType={nodeData.targetType} positionSource={floatSpec.PositionSource} key={floatSpec.ContextDataKey} hasContext={(Context != null)} hasDamageResult={damageResult.HasValue} displayText={displayText ?? "null"}");
#endif
            if (string.IsNullOrEmpty(displayText))
                return;

            // 根据 DamageResult 决定颜色和字体大小
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

            // 使用 PositionSourceType 获取显示位置
            Vector3 worldPosition = Context.GetPosition(floatSpec.PositionSource, floatSpec.PositionBindingName);
            worldPosition += new Vector3(floatSpec.Offset.x, floatSpec.Offset.y, 0);

            // 播放飘字
            Spec.ActiveCue = GameplayCueManager.GetOrCreate().PlayFloatingTextCue(
                displayText,
                worldPosition,
                finalColor,
                finalFontSize,
                floatSpec.Duration,
                floatSpec.TextType
            );

#if UNITY_EDITOR
            SkillDiagFileLogger.Log(
                $"[DiagFloatText] activeCue={(Spec.ActiveCue != null)} worldPos={worldPosition} color={finalColor} fontSize={finalFontSize:0.##} duration={floatSpec.Duration:0.##}");
#endif

            if (Spec.ActiveCue != null)
            {
                Spec.IsRunning = true;
            }
        }

        /// <summary>
        /// 获取显示文本
        /// </summary>
        private string GetDisplayText(DamageResult? damageResult)
        {
            var floatSpec = SelfSpec();
            if (floatSpec == null)
            {
                return null;
            }
            var Context = GetContext();
            string text = "";

            // 优先处理 DamageResult
            if (damageResult.HasValue)
            {
                var dr = damageResult.Value;
                if (dr.IsMiss)
                {
                    text = "Miss";
                }
                else
                {
                    text = "-" + UnityEngine.Mathf.RoundToInt(dr.Damage).ToString();
                    if (dr.IsCritical)
                    {
                        text = "暴击! " + text;
                    }
                }
            }
            // 其他类型的上下文数据
            else if (!string.IsNullOrEmpty(floatSpec.ContextDataKey) && Context != null)
            {
                var value = Context.GetCustomData<object>(floatSpec.ContextDataKey, null);
                if (value != null)
                {
                    string numText = "";
                    if (value is float floatValue)
                    {
                        numText = UnityEngine.Mathf.RoundToInt(floatValue).ToString();
                    }
                    else if (value is int intValue)
                    {
                        numText = intValue.ToString();
                    }
                    else
                    {
                        numText = value.ToString();
                    }

                    // 根据飘字类型添加前缀
                    switch (floatSpec.TextType)
                    {
                        case FloatingTextType.Damage:
                            text = "-" + numText;
                            break;
                        case FloatingTextType.Heal:
                        case FloatingTextType.Experience:
                        case FloatingTextType.Gold:
                            text = "+" + numText;
                            break;
                        default:
                            text = numText;
                            break;
                    }
                }
            }

            // 如果没有动态数据，使用固定文本
            if (string.IsNullOrEmpty(text))
            {
                text = floatSpec.FixedText;
            }

            return text;
        }

        public override void StopCue()
        {
            if (Spec.ActiveCue != null)
            {
                GameplayCueManager.GetOrCreate().StopCue(Spec.ActiveCue);
                Spec.ActiveCue = null;
            }
        }

        public override void Reset()
        {
            var selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }
            selfSpec.PositionSource = PositionSourceType.ParentInput;
            selfSpec.PositionBindingName = "";
            selfSpec.TextType = FloatingTextType.Damage;
            selfSpec.FixedText = "";
            selfSpec.ContextDataKey = "Damage";
            selfSpec.TextColor = Color.white;
            selfSpec.FontSize = 32f;
            selfSpec.Duration = 1.5f;
            selfSpec.Offset = new Vector2(0, 1f);
            selfSpec.MoveDirection = new Vector2(0, 1f);
            Spec.DestroyWithNode = false;
        }
    }
}
