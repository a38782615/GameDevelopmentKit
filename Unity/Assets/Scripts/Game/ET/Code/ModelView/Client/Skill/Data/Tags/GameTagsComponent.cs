using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 游戏标签组件，负责维护运行时可变标签集合和引用计数。
    /// </summary>
    [EnableMethod]
    [ComponentOf(typeof(AbilitySystemComponent))]
    public class GameTagsComponent : Entity, IAwake, IDestroy
    {
        [SerializeField]
        private List<GameplayTag> tags = new List<GameplayTag>();

        private Dictionary<int, int> tagCounts = new Dictionary<int, int>();

        public event Action OnTagsChanged;
        public event Action<GameplayTag> OnTagAdded;
        public event Action<GameplayTag> OnTagRemoved;
        public event Action<GameplayTag, int, int> OnTagCountChanged;

        public IReadOnlyList<GameplayTag> Tags => tags;

        public int Count => tags.Count;

        public bool IsEmpty => tags.Count == 0;

        public int GetTagCount(GameplayTag tag)
        {
            if (!tag.IsValid)
            {
                return 0;
            }

            return tagCounts.TryGetValue(tag.GetHashCode(), out int count) ? count : 0;
        }

        public void AddTag(GameplayTag tag)
        {
            if (!tag.IsValid)
            {
                return;
            }

            int hashCode = tag.GetHashCode();
            int oldCount = tagCounts.TryGetValue(hashCode, out int count) ? count : 0;
            int newCount = oldCount + 1;
            tagCounts[hashCode] = newCount;

            if (oldCount == 0)
            {
                tags.Add(tag);
                OnTagAdded?.Invoke(tag);
            }

            OnTagCountChanged?.Invoke(tag, oldCount, newCount);
            OnTagsChanged?.Invoke();
        }

        public void AddTags(GameplayTagSet tagSet)
        {
            if (tagSet.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < tagSet.Count; i++)
            {
                AddTag(tagSet[i]);
            }
        }

        public bool RemoveTag(GameplayTag tag)
        {
            if (!tag.IsValid)
            {
                return false;
            }

            int hashCode = tag.GetHashCode();
            if (!tagCounts.TryGetValue(hashCode, out int oldCount) || oldCount <= 0)
            {
                return false;
            }

            int newCount = oldCount - 1;
            if (newCount <= 0)
            {
                tagCounts.Remove(hashCode);
                for (int i = tags.Count - 1; i >= 0; i--)
                {
                    if (tags[i] == tag)
                    {
                        tags.RemoveAt(i);
                        break;
                    }
                }

                OnTagRemoved?.Invoke(tag);
            }
            else
            {
                tagCounts[hashCode] = newCount;
            }

            OnTagCountChanged?.Invoke(tag, oldCount, newCount);
            OnTagsChanged?.Invoke();
            return true;
        }

        public void RemoveTags(GameplayTagSet tagSet)
        {
            if (tagSet.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < tagSet.Count; i++)
            {
                RemoveTag(tagSet[i]);
            }
        }

        public void Clear()
        {
            if (tags.Count <= 0)
            {
                return;
            }

            foreach (GameplayTag tag in tags)
            {
                OnTagRemoved?.Invoke(tag);
            }

            tags.Clear();
            tagCounts.Clear();
            OnTagsChanged?.Invoke();
        }

        public bool HasTag(GameplayTag tag)
        {
            if (!tag.IsValid || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTagExact(GameplayTag tag)
        {
            if (!tag.IsValid || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

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

            for (int i = 0; i < other.Count; i++)
            {
                if (!HasTag(other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAnyTags(GameplayTagSet other)
        {
            if (other.IsEmpty || IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < other.Count; i++)
            {
                if (HasTag(other[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasNoneTags(GameplayTagSet other)
        {
            return !HasAnyTags(other);
        }

        public GameplayTagSet ToTagSet()
        {
            return new GameplayTagSet(tags);
        }

        public void SetFromTagSet(GameplayTagSet tagSet)
        {
            Clear();
            if (tagSet.IsEmpty)
            {
                return;
            }

            foreach (GameplayTag tag in tagSet.Tags)
            {
                AddTag(tag);
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "[]";
            }

            return "[" + string.Join(", ", tags.ConvertAll(tag => tag.ToString())) + "]";
        }
    }
}
