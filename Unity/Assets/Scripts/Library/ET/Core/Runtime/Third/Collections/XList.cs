using System.Collections.Generic;
using System;

namespace ET
{
    /// <summary>
    /// 拓展的List,可以在使用foreach的时候不产生gc alloc
    /// 谭仲添
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class XList<T> : List<T>,IEnumerable<T>,IDisposable,IPool
    {
        public static XList<T> Create()
        {
            var rt = ObjectPool.Instance.Fetch<XList<T>>();
            rt.Clear();
            return rt;
        }
        public XList() { }
        public XList(IEnumerable<T> collection) : base(collection) { }
        public XList(int capacity) : base(capacity) { }
        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        new public IEnumerator<T> GetEnumerator()
        {
            var ret = ListEnumerator<T>.sPool.Get();
            ret.SetList(this);
            return ret;
        }

        public void Dispose()
        {
            this.Clear();
            ObjectPool.Instance.Recycle<XList<T>>(this);
        }

        public bool IsFromPool { get; set; }
    }//XList

}//end namespace


