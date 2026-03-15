using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ET
{
    public class ReGoapState<T, W> : IDisposable where W : VarFun
    {
        // can change to object
        public XDictionary<T, W> values;
        public XDictionary<T, W> bufferA;
        public XDictionary<T, W> bufferB;

        public const int DefaultSize = 20;
        public int concurrencyLevel = 5; // No idea.

        public ReGoapState()
        {
            Init(null);
        }
        
        private void Init(ReGoapState<T, W> old)
        {
            bufferA = ObjectPool.Instance.Fetch<XDictionary<T, W>>();
            bufferB = ObjectPool.Instance.Fetch<XDictionary<T, W>>();
            values = bufferA;
            AddFromState(old);
        }
        
        public void Dispose()
        {
            bufferA.Dispose();
            bufferB.Dispose();
            
            Clear();
            ObjectPool.Instance.Recycle<ReGoapState<T, W>>(this);
        }


        public ReGoapState<T, W> Clone()
        {
            return Create(this);
        }

        public static ReGoapState<T, W> Create(ReGoapState<T, W> old = null)
        {
            ReGoapState<T, W> state = ObjectPool.Instance.Fetch<ReGoapState<T, W>>();
            state.Init(old);
            return state;
        }

        public void AddFromState(ReGoapState<T, W> old)
        {
            if (old != null)
            {
                foreach (var pair in old.values)
                {
                    values[pair.Key] = pair.Value;
                }
            }
        }
        public int Count
        {
            get { return values.Count; }
        }

        public bool HasAny(ReGoapState<T, W> other)
        {
            foreach (var pair in other.values)
            {
                W thisValue;
                values.TryGetValue(pair.Key, out thisValue);
                if (thisValue != null && thisValue.Equals(pair.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyConflict(ReGoapState<T, W> effect) // used only in backward for now
        {
            foreach (var pair in effect.values)
            {
                var effectValue = pair.Value;

                //目标不包含结果关键字
                // not here, ignore this check
                W goalValue;
                if (!values.TryGetValue(pair.Key, out goalValue))
                {
                    continue;
                }

                if (!effectValue.Equals(goalValue))
                {
                    return true;
                }
            }

            return false;
        }

        // this method is more relaxed than the other, also accepts conflits that are fixed by "changes"
        public bool HasAnyConflict(ReGoapState<T, W> effects, ReGoapState<T, W> cons)
        {
            foreach (var pair in cons.values)
            {
                var conValue = pair.Value;

                // not here, ignore this check 
                // 目标不包含前提关键字  忽略 默认不冲突
                // 目标包含了前提关键字 就要去比对,如果都不相等,就冲突
                W goalValue;
                if (!values.TryGetValue(pair.Key, out goalValue))
                {
                    continue;
                }

                //结果中含有前提的key
                W effectValue;
                effects.values.TryGetValue(pair.Key, out effectValue);
                bool cg = conValue.Equals(goalValue);
                bool eg = effectValue != null && effectValue.Equals(goalValue);
                //条件的值和目标的值不相等
                //效果的值和目标的值不相等 
                if (!cg && !eg)
                {
                    return true;
                }
            }

            return false;
        }

        public int MissingDifference(ReGoapState<T, W> other, int stopAt = int.MaxValue)
        {
            var count = 0;
            foreach (var pair in values)
            {
                W otherValue;
                other.values.TryGetValue(pair.Key, out otherValue);
                if (!pair.Value.Equals(otherValue))
                {
                    count++;
                    if (count >= stopAt)
                    {
                        break;
                    }
                }
            }

            return count;
        }

        // write differences in "difference"
        public int MissingDifference(ReGoapState<T, W> world, ReGoapState<T, W> difference, int stopAt = int.MaxValue, Func<KeyValuePair<T, W>, W, bool> predicate = null, bool test = false)
        {
            var count = 0;
            foreach (var pair in values)
            {
                //values: a2=2 other a1=1 ,a2=1, a3=3 diff : a1=1
                // diff: a1=1, a2=2 把有other和this有差异化的合并进来
                W worldlValue;
                world.values.TryGetValue(pair.Key, out worldlValue);
                if (!pair.Value.Equals(worldlValue) && (predicate == null || predicate(pair, worldlValue)))
                {
                    count++;
                    if (difference != null)
                    {
                        difference.values[pair.Key] = pair.Value;
                    }

                    if (count >= stopAt)
                    {
                        break;
                    }
                }
            }

            return count;
        }

        // keep only missing differences in values
        public int ReplaceWithMissingDifference(ReGoapState<T, W> other, int stopAt = int.MaxValue, Func<KeyValuePair<T, W>, W, bool> predicate = null, bool test = false)
        {
            var count = 0;
            var buffer = values;
            // swap buffers bf:a1=1 a2=2, other a1=1 a2=4
            // 经过计算 bf:a2=2
            values = values == bufferA ? bufferB : bufferA;
            values.Clear();
            foreach (var pair in buffer)
            {
                W otherValue;
                other.values.TryGetValue(pair.Key, out otherValue);
                if (!pair.Value.Equals(otherValue) && (predicate == null || predicate(pair, otherValue)))
                {
                    count++;
                    values[pair.Key] = pair.Value;
                    if (count >= stopAt)
                    {
                        break;
                    }
                }
            }

            return count;
        }

        public override string ToString()
        {
            var result = "";
            foreach (var pair in values)
            {
                result += string.Format("'{0}': {1}, ", pair.Key, pair.Value);
            }

            return result;
        }

        public W Get(T key)
        {
            lock (values)
            {
                values.TryGetValue(key, out var value);
                return value;
            }
        }

        public void Set<V>(T key, V value) where V : W
        {
            values[key] = value;
        }

        public void Remove(T key)
        {
            values.Remove(key);
        }

        public XDictionary<T, W> GetValues()
        {
            return values;
        }

        public bool TryGetValue<V>(T key, out V value) where V : W
        {
            if (key == null)
            {
                value = null;
                return false;
            }

            var ret = values.TryGetValue(key, out var v);
            if (ret)
            {
                value = (V)v;
            }
            else
            {
                value = null;
            }

            return ret;
        }

        public bool HasKey(T key)
        {
            return values.ContainsKey(key);
        }

        public bool TryStartKeyWith(T key,out string ret)
        {
            ret = null;
            var k = key.ToString();
            foreach (var pair in values)
            {
                var e = pair.Key.ToString();
                if (e.StartsWith(k))
                {
                    ret = e;
                    break;
                }
            }

            return ret != null;
        }

        public void Clear()
        {
            values.Clear();
        }
    }
}