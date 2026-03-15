using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayEffectContainerComponent))]
    [FriendOf(typeof(GameplayEffectContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]

    public static partial class GameplayEffectContainerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayEffectContainerComponent self)
        {
            self.ActiveEffects.Clear();
            self.PendingRemove.Clear();
            self.IsUpdating = false;
        }

        [EntitySystem]
        private static void Update(this GameplayEffectContainerComponent self)
        {
            self.Tick(UnityEngine.Time.deltaTime);
        }

        [EntitySystem]
        private static void Destroy(this GameplayEffectContainerComponent self)
        {
            self.Clear();
        }

        // ============ 效果管理 ============

        public static void AddEffect(this GameplayEffectContainerComponent self, GameplayEffectSpec spec)
        {
            if (spec == null || self.ActiveEffects.Contains(spec)) return;
            self.ActiveEffects.Add(spec);
        }

        public static GameplayEffectSpec FindStackableEffect(this GameplayEffectContainerComponent self, GameplayEffectSpec spec)
        {
            var stackType = spec.EffectNodeData?.stackType ?? StackType.None;
            if (stackType == StackType.None) return null;

            foreach (var e in self.ActiveEffects)
            {
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.EffectNodeData?.nodeType != spec.EffectNodeData?.nodeType) continue;
                if (!effect.Tags.AssetTags.Equals(spec.Tags.AssetTags)) continue;

                switch (stackType)
                {
                    case StackType.AggregateByTarget:
                        return effect;
                    case StackType.AggregateBySource:
                        if (effect.Source.As() == spec.Source.As())
                            return effect;
                        break;
                }
            }

            return null;
        }

        public static bool RemoveEffect(this GameplayEffectContainerComponent self, GameplayEffectSpec spec)
        {
            if (spec == null || !self.ActiveEffects.Contains(spec)) return false;

            if (self.IsUpdating)
            {
                if (!self.PendingRemove.Contains(spec))
                    self.PendingRemove.Add(spec);
            }
            else
            {
                self.RemoveEffectInternal(spec);
            }

            return true;
        }

        private static void RemoveEffectInternal(this GameplayEffectContainerComponent self, GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                self.ActiveEffects.RemoveAll(effectRef => effectRef.As() == null);
                self.PendingRemove.RemoveAll(effectRef => effectRef.As() == null);
                return;
            }

            spec.RemoveEffect();
            self.ActiveEffects.Remove(spec);
            self.PendingRemove.Remove(spec);
            if (!spec.IsDisposed)
            {
                spec.Dispose();
            }
        }

        public static int RemoveEffectsWithTags(this GameplayEffectContainerComponent self, GameplayTagSet tags)
        {
            if (tags.IsEmpty) return 0;

            int removedCount = 0;
            for (int i = self.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var e = self.ActiveEffects[i];
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.Tags.AssetTags.HasAnyTags(tags))
                {
                    self.RemoveEffect(effect);
                    removedCount++;
                }
            }
            return removedCount;
        }

        public static int RemoveEffectsFromSource(this GameplayEffectContainerComponent self, AbilitySystemComponent source)
        {
            if (source == null) return 0;

            int removedCount = 0;
            for (int i = self.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var e = self.ActiveEffects[i];
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.Source.As() == source)
                {
                    self.RemoveEffect(effect);
                    removedCount++;
                }
            }
            return removedCount;
        }

        // ============ 查询方法 ============

        public static IReadOnlyList<EntityRef<GameplayEffectSpec>> GetActiveEffects(this GameplayEffectContainerComponent self)
        {
            return self.ActiveEffects;
        }

        public static GameplayEffectSpec FindEffectByTag(this GameplayEffectContainerComponent self, GameplayTag tag)
        {
            foreach (var e in self.ActiveEffects)
            {
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.Tags.AssetTags.HasTag(tag))
                    return effect;
            }
            return null;
        }

        public static GameplayEffectSpec FindEffectByGrantedTag(this GameplayEffectContainerComponent self, GameplayTag tag)
        {
            foreach (var e in self.ActiveEffects)
            {
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.Tags.GrantedTags.HasTag(tag))
                    return effect;
            }
            return null;
        }

        public static GameplayEffectSpec FindEffectByNodeGuid(this GameplayEffectContainerComponent self, string nodeGuid)
        {
            if (string.IsNullOrEmpty(nodeGuid)) return null;

            foreach (var e in self.ActiveEffects)
            {
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                if (effect.NodeGuid == nodeGuid)
                    return effect;
            }
            return null;
        }

        public static bool HasEffect(this GameplayEffectContainerComponent self, GameplayEffectSpec spec)
        {
            return spec != null && self.ActiveEffects.Contains(spec);
        }

        public static GameplayEffectSpec FindBuffById(this GameplayEffectContainerComponent self, int buffId)
        {
            foreach (var effect in self.ActiveEffects)
            {
                if (effect is EntityRef<GameplayEffectSpec> buffSpec)
                {
                    GameplayEffectSpec spec = buffSpec.As();
                    if (spec == null)
                    {
                        continue;
                    }

                    var buffData = spec.EffectNodeData as BuffEffectNodeData;
                    if (buffData != null && buffData.buffId == buffId)
                        return buffSpec;
                }
            }
            return null;
        }

        // ============ 更新 ============

        public static void Tick(this GameplayEffectContainerComponent self, float deltaTime)
        {
            self.IsUpdating = true;

            for (int i = 0; i < self.ActiveEffects.Count; i++)
            {
                var e = self.ActiveEffects[i];
                var effect = e.As();
                if (effect == null)
                {
                    continue;
                }

                effect.TickEffect(deltaTime);

                if (effect.IsExpired && !self.PendingRemove.Contains(effect))
                    self.PendingRemove.Add(effect);
            }

            self.IsUpdating = false;
            self.ActiveEffects.RemoveAll(effectRef => effectRef.As() == null);
            self.PendingRemove.RemoveAll(effectRef => effectRef.As() == null);

            while (self.PendingRemove.Count > 0)
            {
                GameplayEffectSpec effect = self.PendingRemove[self.PendingRemove.Count - 1].As();
                self.RemoveEffectInternal(effect);
            }
        }

        public static void Clear(this GameplayEffectContainerComponent self)
        {
            while (self.ActiveEffects.Count > 0)
            {
                GameplayEffectSpec effect = self.ActiveEffects[self.ActiveEffects.Count - 1].As();
                self.RemoveEffectInternal(effect);
            }

            self.ActiveEffects.Clear();
            self.PendingRemove.Clear();
        }
    }
}
