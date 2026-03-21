using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(GameTagsComponent))]
    [EntitySystemOf(typeof(GameTagsComponent))]
    public static partial class GameTagsComponentSystem
    {
        [EntitySystem]
        private static void Awake(this GameTagsComponent self)
        {
            self.tags.Clear();
            self.tagCounts.Clear();
        }

        [EntitySystem]
        private static void Destroy(this GameTagsComponent self)
        {
            self.Clear();
        }

        public static int GetTagCount(this GameTagsComponent self, GameplayTag tag)
        {
            if (self == null || !tag.IsValid)
            {
                return 0;
            }

            return self.tagCounts.TryGetValue(tag.GetHashCode(), out int count) ? count : 0;
        }

        public static void AddTag(this GameTagsComponent self, GameplayTag tag)
        {
            if (self == null || !tag.IsValid)
            {
                return;
            }

            int hashCode = tag.GetHashCode();
            int oldCount = self.tagCounts.TryGetValue(hashCode, out int count) ? count : 0;
            int newCount = oldCount + 1;
            self.tagCounts[hashCode] = newCount;

            if (oldCount == 0)
            {
                self.tags.Add(tag);
                self.NotifyTagAdded(tag);
            }

            self.NotifyTagCountChanged(tag, oldCount, newCount);
            self.NotifyTagsChanged();
        }

        public static void AddTags(this GameTagsComponent self, GameplayTagSet tagSet)
        {
            if (self == null || tagSet.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < tagSet.Count; i++)
            {
                self.AddTag(tagSet[i]);
            }
        }

        public static bool RemoveTag(this GameTagsComponent self, GameplayTag tag)
        {
            if (self == null || !tag.IsValid)
            {
                return false;
            }

            int hashCode = tag.GetHashCode();
            if (!self.tagCounts.TryGetValue(hashCode, out int oldCount) || oldCount <= 0)
            {
                return false;
            }

            int newCount = oldCount - 1;
            if (newCount <= 0)
            {
                self.tagCounts.Remove(hashCode);
                for (int i = self.tags.Count - 1; i >= 0; i--)
                {
                    if (self.tags[i] == tag)
                    {
                        self.tags.RemoveAt(i);
                        break;
                    }
                }

                self.NotifyTagRemoved(tag);
            }
            else
            {
                self.tagCounts[hashCode] = newCount;
            }

            self.NotifyTagCountChanged(tag, oldCount, newCount);
            self.NotifyTagsChanged();
            return true;
        }

        public static void RemoveTags(this GameTagsComponent self, GameplayTagSet tagSet)
        {
            if (self == null || tagSet.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < tagSet.Count; i++)
            {
                self.RemoveTag(tagSet[i]);
            }
        }

        public static void Clear(this GameTagsComponent self)
        {
            if (self == null || self.tags.Count <= 0)
            {
                return;
            }

            foreach (GameplayTag tag in self.tags)
            {
                self.NotifyTagRemoved(tag);
            }

            self.tags.Clear();
            self.tagCounts.Clear();
            self.NotifyTagsChanged();
        }

        public static bool HasTag(this GameTagsComponent self, GameplayTag tag)
        {
            if (self == null || !tag.IsValid || self.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < self.tags.Count; i++)
            {
                if (self.tags[i].HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasTagExact(this GameTagsComponent self, GameplayTag tag)
        {
            if (self == null || !tag.IsValid || self.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < self.tags.Count; i++)
            {
                if (self.tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAllTags(this GameTagsComponent self, GameplayTagSet other)
        {
            if (other.IsEmpty)
            {
                return true;
            }

            if (self == null || self.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < other.Count; i++)
            {
                if (!self.HasTag(other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasAnyTags(this GameTagsComponent self, GameplayTagSet other)
        {
            if (self == null || other.IsEmpty || self.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < other.Count; i++)
            {
                if (self.HasTag(other[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasNoneTags(this GameTagsComponent self, GameplayTagSet other)
        {
            return !self.HasAnyTags(other);
        }

        public static GameplayTagSet ToTagSet(this GameTagsComponent self)
        {
            return self == null ? default : new GameplayTagSet(self.tags);
        }

        public static void SetFromTagSet(this GameTagsComponent self, GameplayTagSet tagSet)
        {
            if (self == null)
            {
                return;
            }

            self.Clear();
            if (tagSet.IsEmpty)
            {
                return;
            }

            foreach (GameplayTag tag in tagSet.Tags)
            {
                self.AddTag(tag);
            }
        }
    }
}
