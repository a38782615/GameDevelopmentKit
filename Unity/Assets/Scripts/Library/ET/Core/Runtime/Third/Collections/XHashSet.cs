using System.Collections.Generic;
using System;
using System.Collections;

namespace ET
{
    public class XHashSet<T> : IEnumerable<T>, IDisposable
    {
        private Dictionary<T, int> mMap;
        private XList<T> mList;

        public XList<T> GetList()
        {
            return mList;
        }

        public int Count
        {
            get { return mList.Count; }
        }

        public bool IsFromPool;
        public static XHashSet<T> Create()
        {
            var ret = ObjectPool.Instance.Fetch<XHashSet<T>>();
            ret.Clear();
            ret.IsFromPool = true;
            return ret;
        }

        public XHashSet()
        {
            mMap = new Dictionary<T, int>();
            mList = new XList<T>();
        }

        public void Add(T item)
        {
            if (!mMap.ContainsKey(item))
            {
                mMap.Add(item, mList.Count);
                mList.Add(item);
            }
        }

        /// <summary>
        /// 移除，时间复杂度O(1)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Remove(T key)
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
                    mList.RemoveAt(n - 1); //remove last
                    mMap[lastItem] = i;
                    mMap.Remove(key);
                } //if
                else
                {
                    mList.RemoveAt(i);
                    mMap.Remove(key);
                } //else
            } //if
        } //Remove

        public void Clear()
        {
            mMap.Clear();
            mList.Clear();
        }

        public bool Contains(T item)
        {
            return mMap.ContainsKey(item);
        }

        /// <summary>
        /// 遍历接口
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.Clear();
            if (IsFromPool)
            {
                ObjectPool.Instance.Recycle<XHashSet<T>>(this);
            }
        }
    } //class
} //namespace PLD