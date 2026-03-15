using System.Collections.Generic;
using System;

namespace ET
{
    /// <summary>
    /// 对数组拓展以支持遍历过程的删除和添加的操作
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XArray<T> : XList<T> ,IEnumerable<T>
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity"></param>
        public XArray(int capacity):base(capacity)
        {
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity"></param>
        public XArray() : base()
        {
        }
        /// <summary>
        /// 这个方法的开销非常昂贵
        /// </summary>
        /// <param name="fnMatch"></param>
        /// <returns></returns>
        public T Find(Func<T,bool> fnMatch)
        {
            for(int i=0,n=Count;i<n;++i)
            {
                var it = this[i];
                if(fnMatch(it))
                {
                    return it;
                }
            }
            return default(T);
        }//Find

        /// <summary>
        /// 移除元素
        /// </summary>
        public const int REMOVE = 0;
        /// <summary>
        /// 添加元素
        /// </summary>
        public const int ADD = 1;
        /// <summary>
        /// 有状态的元素
        /// </summary>
        public struct ItemOperation<T1>: IEquatable<ItemOperation<T1>>
        {
            /// <summary>
            /// 数值
            /// </summary>
            public T1 value;
            /// <summary>
            /// 操作
            /// </summary>
            public int operation;
            /// <summary>
            /// 对象操作
            /// </summary>
            /// <param name="data"></param>
            /// <param name="state"></param>
            public ItemOperation(T1 data = default(T1),int state = ADD)
            {
                this.value = data;
                this.operation = state;
            }
            /// <summary>
            /// 实现这个接口可以提升性能
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(ItemOperation<T1> other)
            {
                return value.Equals(other.value) && operation == other.operation;
            }

            /// <summary>
            /// 实现这个接口可以提升性能
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(ref ItemOperation<T1> other)
            {
                return value.Equals(other.value) && operation == other.operation;
            }

            public override bool Equals(object obj)
            {
                Log.Error("XArray.ItemOperation.Equals被装箱了");
                if(obj is ItemOperation<T1>)
                {
                    var other = (ItemOperation<T1>)obj;
                    return value.Equals(other.value) && operation == other.operation;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }//end ItemOperation

        /// <summary>
        /// 清除缓存的操作
        /// </summary>
        /// <returns></returns>
        public XArray<T> ClearOperations()
        {
            CachedList.Clear();
            return this;
        }

        /// <summary>
        /// 延迟删除
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public XArray<T> RemoveDelay(T item)
        {
            CachedList.Add(new ItemOperation<T>(item, REMOVE));
            return this;
        }
        /// <summary>
        /// 延迟添加
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public XArray<T> AddDelay(T item)
        {
            CachedList.Add(new ItemOperation<T>(item, ADD));
            return this;
        }
        public XList<ItemOperation<T>> GetCachedList()
        {
            return mCachedList;
        }
        /// <summary>
        /// 刷新缓存数据
        /// </summary>
        /// <returns></returns>
        public XArray<T> Flush()
        {
            if(Dirty)
            {
                for(int i=0,n=CachedList.Count;i<n;++i)
                {
                    var it = CachedList[i];
                    if(ADD == it.operation)
                    {
                        Add(it.value);
                    }
                    if(REMOVE == it.operation)
                    {
                        Remove(it.value);
                    }
                }
				CachedList.Clear ();
            }
            return this;
        }
        /// <summary>
        /// 列表是否被修改过
        /// </summary>
        public bool Dirty
        {
            get
            {
                if(null == mCachedList)
                {
                    return false;
                }
                else
                {
                    return mCachedList.Count > 0;
                }
            }
        }
        /// <summary>
        /// 缓存队列
        /// </summary>
        private XList<ItemOperation<T>> mCachedList;
        /// <summary>
        /// 缓存列表
        /// </summary>
        private XList<ItemOperation<T>> CachedList
        {
            get
            {
                if(null == mCachedList)
                {
                    mCachedList = new XList<ItemOperation<T>>();
                }
                return mCachedList;
            }
        }

        private XList<T> mSolveCachedTemp;
        private XList<T> SolveCachedTemp
        {
            get
            {
                if(mSolveCachedTemp == null)
                {
                    mSolveCachedTemp = new XList<T>();
                }
                return mSolveCachedTemp;
            }
        }
        /// <summary>
        /// 处理缓存内容
        /// 将被添加缓存的列表返回
        /// 等同于Flush，但是返回值不同
        /// </summary>
        public XList<T> SolveCached()
        {
            if (Dirty)
            {
                XList<T> rt = SolveCachedTemp;
                rt.Clear();
                for(int i=0,n=CachedList.Count;i<n;++i)
                {
                    var it = CachedList[i];
                    if (ADD == it.operation)
                    {
                        rt.Add(it.value);
                        Add(it.value);
                    }
                    if (REMOVE == it.operation)
                    {
                        rt.Remove(it.value);
                        Remove(it.value);
                    }
                }
                CachedList.Clear();
                return rt;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 得到缓存内容
        /// 将被添加缓存的列表返回
        /// 不等同于Flush，不做Flush操作
        /// </summary>
        public XList<T> GetCached()
        {
            XList<T> rt = SolveCachedTemp;
            rt.Clear();

            if (Dirty)
            {
                for(int i=0,n=CachedList.Count;i<n;++i)
                {
                    var it = CachedList[i];
                    if (ADD == it.operation)
                    {
                        rt.Add(it.value);
                    }
                    if (REMOVE == it.operation)
                    {
                        rt.Remove(it.value);
                    }
                }
            }

            return rt;
        }

        new IEnumerator<T> GetEnumerator()
        {
            return base.GetEnumerator();
        }
    }//end class
}//end namespace

