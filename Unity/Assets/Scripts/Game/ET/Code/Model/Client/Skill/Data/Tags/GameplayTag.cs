using System;

namespace ET.Client
{
    /// <summary>
    /// 游戏标签，支持层级结构，例如 "Ability.Attack.Melee"。
    /// </summary>
    [Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>
    {
        [Newtonsoft.Json.JsonProperty]
        public string Name;

        [Newtonsoft.Json.JsonProperty]
        public int HashCode;

        [Newtonsoft.Json.JsonProperty]
        public string ShortName;

        [Newtonsoft.Json.JsonProperty]
        public int[] AncestorHashCodes;

        [Newtonsoft.Json.JsonProperty]
        public string[] AncestorNames;

        /// <summary>
        /// 标签是否有效。
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// 标签是否为空，与 IsValid 相反。
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Name);

        /// <summary>
        /// 标签深度，即层级数。
        /// </summary>
        public int Depth => AncestorNames?.Length ?? 0;

        /// <summary>
        /// 空标签。
        /// </summary>
        public static GameplayTag None => new GameplayTag();

        /// <summary>
        /// 创建一个游戏标签。
        /// </summary>
        /// <param name="name">完整标签名，例如 "Ability.Attack.Melee"。</param>
        public GameplayTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = null;
                HashCode = 0;
                ShortName = null;
                AncestorHashCodes = Array.Empty<int>();
                AncestorNames = Array.Empty<string>();
                return;
            }

            Name = name;
            HashCode = name.GetHashCode();

            string[] parts = name.Split('.');
            ShortName = parts[parts.Length - 1];

            if (parts.Length > 1)
            {
                AncestorHashCodes = new int[parts.Length - 1];
                AncestorNames = new string[parts.Length - 1];

                string ancestorPath = string.Empty;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (i > 0)
                    {
                        ancestorPath += ".";
                    }

                    ancestorPath += parts[i];
                    AncestorNames[i] = ancestorPath;
                    AncestorHashCodes[i] = ancestorPath.GetHashCode();
                }
            }
            else
            {
                AncestorHashCodes = Array.Empty<int>();
                AncestorNames = Array.Empty<string>();
            }
        }

        /// <summary>
        /// 检查是否拥有指定标签，支持层级匹配。
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            if (!tag.IsValid || !IsValid)
            {
                return false;
            }

            if (HashCode == tag.HashCode)
            {
                return true;
            }

            if (AncestorHashCodes != null)
            {
                for (int i = 0; i < AncestorHashCodes.Length; i++)
                {
                    if (AncestorHashCodes[i] == tag.HashCode)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查当前标签是否是另一个标签的后代。
        /// </summary>
        public bool IsDescendantOf(GameplayTag other)
        {
            if (!other.IsValid || !IsValid)
            {
                return false;
            }

            return HasTag(other) && HashCode != other.HashCode;
        }

        /// <summary>
        /// 检查当前标签是否是另一个标签的祖先。
        /// </summary>
        public bool IsAncestorOf(GameplayTag other)
        {
            return other.IsDescendantOf(this);
        }

        /// <summary>
        /// 获取父标签。
        /// </summary>
        public GameplayTag GetParent()
        {
            if (AncestorNames == null || AncestorNames.Length == 0)
            {
                return None;
            }

            return new GameplayTag(AncestorNames[AncestorNames.Length - 1]);
        }

        #region 相等性比较

        public bool Equals(GameplayTag other)
        {
            return HashCode == other.HashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public static bool operator ==(GameplayTag left, GameplayTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayTag left, GameplayTag right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            return Name ?? "None";
        }

        /// <summary>
        /// 隐式转换：字符串 -> GameplayTag。
        /// </summary>
        public static implicit operator GameplayTag(string name)
        {
            return new GameplayTag(name);
        }

        /// <summary>
        /// 隐式转换：GameplayTag -> 字符串。
        /// </summary>
        public static implicit operator string(GameplayTag tag)
        {
            return tag.Name;
        }
    }
}
