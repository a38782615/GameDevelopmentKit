using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace ET.Client
{
    /// <summary>
    /// 公式计算器 - 解析并计算属性公式
    /// 支持格式如 "Caster.Attack * 1.5 + 10" 或 "Target.Health * 0.3"
    /// </summary>
    public static class FormulaEvaluator
    {
        // 属性引用正则：Caster.Health 或 Target.Health
        [StaticField]
        private static readonly Regex AttributePattern = new Regex(
            @"(Caster|Target)\.(\w+)",
            RegexOptions.Compiled);

        // 变量引用正则：$VariableName
        [StaticField]
        private static readonly Regex VariablePattern = new Regex(
            @"\$(\w+)",
            RegexOptions.Compiled);

        /// <summary>
        /// 计算公式
        /// </summary>
        /// <param name="formula">公式字符串</param>
        /// <param name="context">计算上下文</param>
        /// <returns>计算结果</returns>
        public static float Evaluate(string formula, FormulaContext context)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0f;

            // 尝试直接解析为数字
            if (float.TryParse(formula, out float directValue))
                return directValue;

            try
            {
                // 替换属性引用
                string expression = AttributePattern.Replace(formula, match =>
                {
                    string source = match.Groups[1].Value;
                    string attrName = match.Groups[2].Value;

                    // 尝试解析属性类型
                    if (!global::ET.NumericType.TryParseAttributeName(attrName, out int numericType))
                    {
                        return "0";
                    }

                    float? value = null;
                    if (source == "Caster" && context.CasterAttributes != null)
                    {
                        if (context.CasterAttributeOverrides != null && context.CasterAttributeOverrides.TryGetValue(numericType, out float overrideValue))
                        {
                            value = overrideValue;
                        }
                        else
                        {
                            value = context.CasterAttributes.GetValue(numericType);
                        }
                    }
                    else if (source == "Target" && context.TargetAttributes != null)
                    {
                        value = context.TargetAttributes.GetValue(numericType);
                    }

                    return (value ?? 0f).ToString(System.Globalization.CultureInfo.InvariantCulture);
                });

                // 替换变量引用
                expression = VariablePattern.Replace(expression, match =>
                {
                    string varName = match.Groups[1].Value;
                    if (context.Variables != null && context.Variables.TryGetValue(varName, out float varValue))
                    {
                        return varValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    return "0";
                });

                // 替换特殊变量
                expression = expression.Replace("StackCount", context.StackCount.ToString());
                expression = expression.Replace("Level", context.Level.ToString());

                // 计算表达式
                return EvaluateExpression(expression);
            }
            catch (Exception)
            {
                // 解析失败，返回0
                return 0f;
            }
        }

        /// <summary>
        /// 简单表达式计算（支持 +, -, *, /, 括号）
        /// </summary>
        private static float EvaluateExpression(string expression)
        {
            // 移除空格
            expression = expression.Replace(" ", "");

            if (string.IsNullOrEmpty(expression))
                return 0f;

            // 使用简单的递归下降解析器
            int pos = 0;
            return ParseAddSub(expression, ref pos);
        }

        private static float ParseAddSub(string expr, ref int pos)
        {
            float left = ParseMulDiv(expr, ref pos);

            while (pos < expr.Length)
            {
                char op = expr[pos];
                if (op != '+' && op != '-')
                    break;

                pos++;
                float right = ParseMulDiv(expr, ref pos);

                if (op == '+')
                    left += right;
                else
                    left -= right;
            }

            return left;
        }

        private static float ParseMulDiv(string expr, ref int pos)
        {
            float left = ParseUnary(expr, ref pos);

            while (pos < expr.Length)
            {
                char op = expr[pos];
                if (op != '*' && op != '/')
                    break;

                pos++;
                float right = ParseUnary(expr, ref pos);

                if (op == '*')
                    left *= right;
                else if (right != 0)
                    left /= right;
            }

            return left;
        }

        private static float ParseUnary(string expr, ref int pos)
        {
            if (pos < expr.Length && expr[pos] == '-')
            {
                pos++;
                return -ParsePrimary(expr, ref pos);
            }
            return ParsePrimary(expr, ref pos);
        }

        private static float ParsePrimary(string expr, ref int pos)
        {
            // 处理括号
            if (pos < expr.Length && expr[pos] == '(')
            {
                pos++; // 跳过 '('
                float result = ParseAddSub(expr, ref pos);
                if (pos < expr.Length && expr[pos] == ')')
                    pos++; // 跳过 ')'
                return result;
            }

            // 解析数字
            int start = pos;
            while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
            {
                pos++;
            }

            if (start == pos)
                return 0f;

            string numStr = expr.Substring(start, pos - start);
            if (float.TryParse(numStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float num))
            {
                return num;
            }

            return 0f;
        }

        /// <summary>
        /// 快速计算固定值或简单公式
        /// </summary>
        public static float EvaluateSimple(string formula, float defaultValue = 0f)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return defaultValue;

            if (float.TryParse(formula, out float value))
                return value;

            return defaultValue;
        }
    }


}
