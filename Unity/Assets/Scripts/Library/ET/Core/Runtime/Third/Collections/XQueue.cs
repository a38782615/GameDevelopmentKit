using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 这个类不是真正的queue，它只是对List做了简单的拓展
    /// 以支持随机访问
    /// 获取长度请用Size
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XQueue<T> : Queue<T>,IDisposable,IPool
    {
        public static XQueue<T> Create()
        {
            var rt = ObjectPool.Instance.Fetch<XQueue<T>>();
            rt.Clear();
            return rt;
        }
        public XQueue(int capacity):base(capacity)
        {
            
        }

        public XQueue():this(0)
        {

        }
        /// <summary>
        /// 清除所有的元素
        /// </summary>
        public void Dispose()
        {
            ObjectPool.Instance.Recycle<XQueue<T>>(this);
        }

        public bool IsFromPool { get; set; }
    }//end class
}//end namespace


