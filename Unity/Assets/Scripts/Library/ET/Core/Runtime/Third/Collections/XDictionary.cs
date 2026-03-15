using System.Collections.Generic;
using System;
using System.Collections;
#if Unity
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

namespace ET
{
    public class XDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>,IDisposable,IPool
    {
        public static XDictionary<K, V> Create()
        {
            var rt = ObjectPool.Instance.Fetch<XDictionary<K, V>>();
            rt.Clear();
            return rt;
        }
        /// <summary>
        /// 字典
        /// </summary>
#if Unity
        [OdinSerialize]
        [NonSerialized]
#endif
        private Dictionary<K, int> mMap;
        /// <summary>
        /// 顺序表
        /// </summary>
#if Unity
        [OdinSerialize]
        [NonSerialized]
#endif
        private XList<KeyValuePair<K, V>> mList;

        public bool Remove(KeyValuePair<K, V> item)
        {
            Remove(item.Key);
            return true;
        }

        public int Count
        {
            get
            {
                if (mList == null)
                {
                    mList = new XList<KeyValuePair<K, V>>();
                }
                return mList.Count;
            }
        }

        public bool IsReadOnly { get; }

        public XList<KeyValuePair<K, V>> GetList()
        {
            return mList;
        }

        public XDictionary(int capacity)
        {
            mMap = new Dictionary<K, int>(capacity);
            mList = new XList<KeyValuePair<K, V>>(capacity);
        }

        public XDictionary(IEqualityComparer<K> comparer)
        {
            mMap = new Dictionary<K, int>(comparer);
            mList = new XList<KeyValuePair<K, V>>();
        }

        public XDictionary(int capacity,
            IEqualityComparer<K> comparer)
        {
            mMap = new Dictionary<K, int>(capacity, comparer);
            mList = new XList<KeyValuePair<K, V>>(capacity);
        }
        public XDictionary()
        {
            mMap = new Dictionary<K, int>();
            mList = new XList<KeyValuePair<K, V>>();
        }

        /// <summary>
        /// 添加，时间复杂度O(1)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Add(K key, V val)
        {
            if (key == null || val==null)
            {
                Log.Error("key or val is null");
                return;
            }
            if (!mMap.ContainsKey(key))
            {
                mList.Add(new KeyValuePair<K, V>(key, val));
                mMap.Add(key, mList.Count - 1);
            }//if
            else
            {
                var i = mMap[key];
                mList[i] = new KeyValuePair<K, V>(key, val);
            }
        }//Add

        public void Replace(K old, K newKey,V val)
        {
            if (!mMap.TryGetValue(old, out var odx))
            {
                return;
            }
            //如果存在
            //找到odx 移动到idx
            //小odx往大idx移动 大于odx小于idx的-1
            //大odx往小idx移动 大于idx小于odx的+1
            // a b c d e   0 ,1, 2, 3,4
            // a b x c d e 
            // odx = 2
            //
            //如果不存在
            //大于idx的 +1
            mList.RemoveAt(odx);
            
            var newl = new KeyValuePair<K, V>(newKey, val);
            mList.Insert(odx,newl);
            
            mMap.Remove(old);
            mMap[newKey] = odx;
        }

        /// <summary>
        /// 移除，时间复杂度O(1)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Remove(K key)
        {
            if (mMap.ContainsKey(key))
            {
                var i = mMap[key];
                var n = mList.Count;
                if (n > 1)
                {
                    var last = n - 1;
                    var lastItem = mList[last];
                    mList[i] = lastItem;
                    mList.RemoveAt(n - 1);//remove last
                    mMap[lastItem.Key] = i;
                    mMap.Remove(key);
                }//if
                else
                {
                    mList.RemoveAt(i);
                    mMap.Remove(key);
                }//else
            }//if
        }//Remove
        /// <summary>
        /// 存取器
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V this[K key]
        {
            get
            {
                var i = 0;
                if (mMap.TryGetValue(key, out i))
                {
                    return mList[i].Value;
                }
                return default(V);
            }
            set
            {
                var i = 0;
                if (mMap.TryGetValue(key, out i))
                {
                    mList[i] = new KeyValuePair<K, V>(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }
        public bool ContainsKey(K key)
        {
            return mMap.ContainsKey(key);
        }

        /// <summary>
        /// 原则上不允许随意用这个接口，开销比较大
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(V value)
        {
            var ret = false;
            for (int i = 0, n = mList.Count; i < n; ++i)
            {
                if (value.Equals(mList[i].Value))
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        private XList<K> tempList = new XList<K>();
        public XList<K> GetKeyForValue(V v)
        {
            tempList.Clear();
            
            for (int i = 0, n = mList.Count; i < n; ++i)
            {
                if (v.Equals(mList[i].Value))
                {
                    tempList.Add(mList[i].Key);
                }
            }
            return tempList;
        }
        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V value)
        {
            var ret = false;
            value = default(V);
            if (key == null)
            {
                return ret;
            }
            var i = 0;
            ret = mMap.TryGetValue(key, out i);
            if (ret)
            {
                value = mList[i].Value;
            }//if
            return ret;
        }//TryGetValue

        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key,item.Value);
        }

        /// <summary>
        /// 清除所有的元素
        /// </summary>
        public void Dispose()
        {
            Clear();
            ObjectPool.Instance.Recycle<XDictionary<K, V>>(this);
        }

        public void Clear()
        {
            mMap.Clear();
            mList.Clear();
        }

        public int GetIdx(K key)
        {
            mMap.TryGetValue(key, out var idx);
            return idx;
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < mList.Count; i++)
            {
                array[i-arrayIndex] = mList[i];
            }
        }
        //Clear

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        public bool IsFromPool { get; set; }
    }//XDictionary

}//namespace PLD


