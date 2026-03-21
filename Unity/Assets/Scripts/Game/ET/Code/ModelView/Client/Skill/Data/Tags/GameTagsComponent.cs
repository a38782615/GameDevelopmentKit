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
        internal List<GameplayTag> tags = new List<GameplayTag>();

        internal Dictionary<int, int> tagCounts = new Dictionary<int, int>();

        public event Action OnTagsChanged;
        public event Action<GameplayTag> OnTagAdded;
        public event Action<GameplayTag> OnTagRemoved;
        public event Action<GameplayTag, int, int> OnTagCountChanged;

        public IReadOnlyList<GameplayTag> Tags => this.tags;

        public int Count => this.tags.Count;

        public bool IsEmpty => this.tags.Count == 0;

        internal void NotifyTagsChanged()
        {
            this.OnTagsChanged?.Invoke();
        }

        internal void NotifyTagAdded(GameplayTag tag)
        {
            this.OnTagAdded?.Invoke(tag);
        }

        internal void NotifyTagRemoved(GameplayTag tag)
        {
            this.OnTagRemoved?.Invoke(tag);
        }

        internal void NotifyTagCountChanged(GameplayTag tag, int oldCount, int newCount)
        {
            this.OnTagCountChanged?.Invoke(tag, oldCount, newCount);
        }

        public override string ToString()
        {
            if (this.IsEmpty)
            {
                return "[]";
            }

            return "[" + string.Join(", ", this.tags.ConvertAll(tag => tag.ToString())) + "]";
        }
    }
}
