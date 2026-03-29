using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 游戏标签集合，用于配置时定义的固定标签集。
    /// </summary>
    [Serializable]
    public struct GameplayTagSet : IEquatable<GameplayTagSet>
    {
        [Newtonsoft.Json.JsonProperty]
        public GameplayTag[] Tags;

        /// <summary>
        /// 标签数量。
        /// </summary>
        public int Count => Tags?.Length ?? 0;

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => Tags == null || Tags.Length == 0;

        /// <summary>
        /// 空标签集合。
        /// </summary>
        public static GameplayTagSet Empty => new GameplayTagSet(Array.Empty<GameplayTag>());

        /// <summary>
        /// 创建标签集合。
        /// </summary>
        public GameplayTagSet(params GameplayTag[] tags)
        {
            Tags = tags ?? Array.Empty<GameplayTag>();
        }

        /// <summary>
        /// 从字符串数组创建标签集合。
        /// </summary>
        public GameplayTagSet(params string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0)
            {
                Tags = Array.Empty<GameplayTag>();
                return;
            }

            Tags = new GameplayTag[tagNames.Length];
            for (int i = 0; i < tagNames.Length; i++)
            {
                Tags[i] = new GameplayTag(tagNames[i]);
            }
        }

        /// <summary>
        /// 从列表创建标签集合。
        /// </summary>
        public GameplayTagSet(List<GameplayTag> tags)
        {
            Tags = tags?.ToArray() ?? Array.Empty<GameplayTag>();
        }

        /// <summary>
        /// 检查是否包含指定标签，支持层级匹配。
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            if (!tag.IsValid || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i].HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否包含指定标签，精确匹配。
        /// </summary>
        public bool HasTagExact(GameplayTag tag)
        {
            if (!tag.IsValid || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否包含所有指定标签。
        /// </summary>
        public bool HasAllTags(GameplayTagSet other)
        {
            if (other.IsEmpty)
            {
                return true;
            }

            if (IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < other.Tags.Length; i++)
            {
                if (!HasTag(other.Tags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查是否包含任意一个指定标签。
        /// </summary>
        public bool HasAnyTags(GameplayTagSet other)
        {
            if (other.IsEmpty || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < other.Tags.Length; i++)
            {
                if (HasTag(other.Tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否不包含任何指定标签。
        /// </summary>
        public bool HasNoneTags(GameplayTagSet other)
        {
            return !HasAnyTags(other);
        }

        /// <summary>
        /// 获取与另一个集合的交集。
        /// </summary>
        public GameplayTagSet Intersect(GameplayTagSet other)
        {
            if (IsEmpty || other.IsEmpty)
            {
                return Empty;
            }

            List<GameplayTag> result = new List<GameplayTag>();
            for (int i = 0; i < Tags.Length; i++)
            {
                if (other.HasTagExact(Tags[i]))
                {
                    result.Add(Tags[i]);
                }
            }

            return new GameplayTagSet(result);
        }

        /// <summary>
        /// 获取与另一个集合的并集。
        /// </summary>
        public GameplayTagSet Union(GameplayTagSet other)
        {
            if (IsEmpty)
            {
                return other;
            }

            if (other.IsEmpty)
            {
                return this;
            }

            List<GameplayTag> result = new List<GameplayTag>(Tags);
            for (int i = 0; i < other.Tags.Length; i++)
            {
                if (!HasTagExact(other.Tags[i]))
                {
                    result.Add(other.Tags[i]);
                }
            }

            return new GameplayTagSet(result);
        }

        /// <summary>
        /// 获取差集，即当前集合有但另一个集合没有的标签。
        /// </summary>
        public GameplayTagSet Except(GameplayTagSet other)
        {
            if (IsEmpty)
            {
                return Empty;
            }

            if (other.IsEmpty)
            {
                return this;
            }

            List<GameplayTag> result = new List<GameplayTag>();
            for (int i = 0; i < Tags.Length; i++)
            {
                if (!other.HasTagExact(Tags[i]))
                {
                    result.Add(Tags[i]);
                }
            }

            return new GameplayTagSet(result);
        }

        /// <summary>
        /// 添加单个标签，并返回新的标签集合。
        /// </summary>
        public GameplayTagSet AddTag(GameplayTag tag)
        {
            if (!tag.IsValid || HasTagExact(tag))
            {
                return this;
            }

            List<GameplayTag> result = new List<GameplayTag>(Tags)
            {
                tag
            };
            return new GameplayTagSet(result);
        }

        /// <summary>
        /// 移除单个标签，并返回新的标签集合。
        /// </summary>
        public GameplayTagSet RemoveTag(GameplayTag tag)
        {
            if (!tag.IsValid || IsEmpty || !HasTagExact(tag))
            {
                return this;
            }

            List<GameplayTag> result = new List<GameplayTag>();
            for (int i = 0; i < Tags.Length; i++)
            {
                if (Tags[i] != tag)
                {
                    result.Add(Tags[i]);
                }
            }

            return new GameplayTagSet(result);
        }

        #region 相等性比较

        public bool Equals(GameplayTagSet other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            if (IsEmpty && other.IsEmpty)
            {
                return true;
            }

            for (int i = 0; i < Tags.Length; i++)
            {
                if (!other.HasTagExact(Tags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagSet other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }

            int hash = 17;
            for (int i = 0; i < Tags.Length; i++)
            {
                hash = hash * 31 + Tags[i].GetHashCode();
            }

            return hash;
        }

        public static bool operator ==(GameplayTagSet left, GameplayTagSet right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayTagSet left, GameplayTagSet right)
        {
            return !left.Equals(right);
        }

        #endregion

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "[]";
            }

            return "[" + string.Join(", ", Array.ConvertAll(Tags, tag => tag.ToString())) + "]";
        }

        /// <summary>
        /// 索引器。
        /// </summary>
        public GameplayTag this[int index] => Tags[index];
    }
}
