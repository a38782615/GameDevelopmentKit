using System.Collections.Generic;
using System;
using System.Collections;

namespace ET
{
    public interface XIPool
    {
        object GetX();
        void PutX(object item);

        void Clear();
        ///// <summary>
        ///// 自动清零
        ///// </summary>
        //void AutoCleanup();
        /// <summary>
        /// 在AutoCleanup的时候是否自动被清理
        /// </summary>
        bool RemoveWhenAutoCleanup { get; }
        /// <summary>
        /// 设置在被清理的时候自动Cleanup
        /// </summary>
        XIPool SetRemoveWhenAutoCleanup();
        /// <summary>
        /// 
        /// </summary>
        void MakeReserveItems(int count);
    }

    public class PoolManager : List<WeakReference>
    {
        [StaticField]
        public readonly static PoolManager sInstance = new PoolManager();

        public void CleanCached()
        {
            for (int i = Count - 1; i >= 0; --i)
            {
                var it = this[i];
                //将弱引用转换为强引用才可以使用，相当于lock
                XIPool pool = it.Target as XIPool;
                if (null!=pool)
                {
                    //void
                }
                else
                {
                    RemoveAt(i);
                    continue;
                }
                pool.Clear();
                if (pool.RemoveWhenAutoCleanup)
                {
                    RemoveAt(i);
                }//if
            }//for
        }//CleanCached
        /// <summary>
        /// 添加到自动移除列表中
        /// </summary>
        /// <param name="pool"></param>
        public static void AddToAutoCleanUpList(XIPool pool)
        {
            sInstance.Add(new WeakReference(pool));
        }//AddToAutoCleanUpList
    }//PoolManager


    /// <summary>
    /// 对象池
    /// </summary>
    public class XPool<T> : IEnumerable<T>, IDisposable,XIPool
        where T : class, new()
    {
        /// <summary>
        /// 在AutoCleanup的时候是否自动被清理
        /// </summary>
        bool XIPool.RemoveWhenAutoCleanup { get { return mRemoveWhenAutoCleanup; } }
        /// <summary>
        /// 设置在被清理的时候自动Cleanup
        /// </summary>
        XIPool XIPool.SetRemoveWhenAutoCleanup()
        {
            mRemoveWhenAutoCleanup = true;
            return this;
        }
        /// <summary>
        /// 设置成在AutoCleanUp的时候从PoolManager中移除以
        /// 消除强引用
        /// </summary>
        /// <returns></returns>
        public XPool<T> SetRemoveWhenAutoCleanup()
        {
            mRemoveWhenAutoCleanup = true;
            return this;
        }//SetRemoveWhenAutoCleanup

        /// <summary>
        /// 标记是否在AutoCleanup的时候从管理器里面移除
        /// </summary>
        private bool mRemoveWhenAutoCleanup;

        private XHashSet<T> mSet;
        /// <summary>
        /// 默认的初始化容量
        /// </summary>
        private const int DEFAULT_SIZE = 4;
        /// <summary>
        /// 存放数据的容器
        /// </summary>
        public List<T> mPutImpl;
        public List<T> mGetImpl;
        /// <summary>
        /// 标记是否已经销毁过了
        /// </summary>
        private bool mDisposed = false; 

        /// <summary>
        /// 销毁资源
        /// </summary>
        public void Dispose()
        {
            if (!mDisposed)
            {
                mDisposed = true;
            }
            Clear();
            mCreator = null;//释放资源
            mPutHandler = null;//释放强引用
            mGetHandler = null;//释放强引用
        }//Dispose
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity"></param>
        public XPool(int capacity) 
        {
            Init(null, capacity);
        }
        /// <summary>
        /// 根据构造器来构造
        /// </summary>
        /// <param name="fnCreator"></param>
        public XPool(Func<T> fnCreator)
        {
            Init(fnCreator, DEFAULT_SIZE);
        }
        /// <summary>
        /// 不带任何参数的构造方法
        /// </summary>
        public XPool()
        {
            Init(null, DEFAULT_SIZE);
        }
        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <param name="fnCreator"></param>
        /// <param name="capacity"></param>
        private void Init(Func<T> fnCreator, int capacity)
        {
            mPutImpl = new List<T>(capacity);
            mGetImpl = new List<T>(capacity);
            mSet = new XHashSet<T>();
            mCreator = fnCreator;
        } 

        /// <summary>
        /// 根据构造器和初始容量来构造对象
        /// </summary>
        /// <param name="fnCreator"></param>
        /// <param name="capacity"></param>
        public XPool(Func<T> fnCreator, int capacity)
        {
            Init(fnCreator, capacity);
        }
        /// <summary>f
        /// 用来构造对象的delegate
        /// </summary>
        public Func<T> mCreator = null;
        /// <summary>
        /// 获取的时候的处理器
        /// </summary>
        public Action<T> mGetHandler = null;
        /// <summary>
        /// 放回去的时候的处理器
        /// </summary>
        public Action<T> mPutHandler = null;
        /// <summary>
        /// 类型是否为IReleaseable
        /// </summary>
        // private bool mIsIReleaseable = false;
        /// <summary>
        /// 类型是否为IDisposable
        /// </summary>
        /// <summary>
        /// 清除全部的缓存对象
        /// </summary>
        public Action<T> mDisposeHandler = null;
        public void Clear()
        {
            mSet.Clear();
            // if(mIsIReleaseable)
            // {
            //     for(int i=0,n=mImpl.Count;i<n;++i)
            //     {
            //         var it = mImpl[i];
            //         var r = it as IReleaseable;
            //         if (null != r)
            //         {
            //             r.Release();
            //         }//if
            //     }
            // }//
            // else 
            {
                for (int i = 0, n = mPutImpl.Count; i < n; ++i)
                {
                    var it = mPutImpl[i];
                    var r = it as IDisposable;
                    if (null != r)
                    {
                        mDisposeHandler?.Invoke(it);
                        r.Dispose();
                    }//if
                }
                for (int i = 0, n = mGetImpl.Count; i < n; ++i)
                {
                    var it = mGetImpl[i];
                    var r = it as IDisposable;
                    if (null != r)
                    {
                        mDisposeHandler?.Invoke(it);
                        r.Dispose();
                    }//if
                }
            }//else
            mPutImpl.Clear();
        }//Clear
        /// <summary>
        /// 只做引用清理工作
        /// </summary>
        public void ClearReffOnly()
        {
            if(null != mPutImpl)
            {
                mPutImpl.Clear();
            }
            if(null != mGetImpl)
            {
                mGetImpl.Clear();
            }
            if(null!= mSet)
            {
                mSet.Clear();
            }
        }//ClearReffOnly
        /// <summary>
        /// 迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return mPutImpl.GetEnumerator();
        }
        /// <summary>
        /// 迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mPutImpl.GetEnumerator();
        }
        /// <summary>
        /// 从对象池中获取对象
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            T ret = default(T);
            if(0 == mPutImpl.Count)
            {
                if(null != mCreator)
                {
                    ret = mCreator();
                }
                else
                {
                    ret = new T();
                }
//#if TRACKING_MAX
//                if(null != mTrackingInfo)
//                {
//                    mTrackingInfo.Track(ret);
//                }
//#endif//TRACKING_MAX
            }
            else
            {
                var n = mPutImpl.Count - 1;
                ret = mPutImpl[n];
                mPutImpl.RemoveAt(n);
                if(ret != null && mSet.Contains(ret))
                {
                    mSet.Remove(ret);
                }
            }
            if(null != mGetHandler)
            {
                mGetHandler(ret);
            }
            mGetImpl.Add(ret);
            return ret;
        }
        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        /// <param name="item"></param>
        public void Put(T item)
        {
            if(mSet.Contains(item))
            {
                return;
            }
            mSet.Add(item);
            if(null != mPutHandler)
            {
                mPutHandler(item);
            }
            mPutImpl.Add(item);
            mGetImpl.Remove(item);
        }

        /// <summary>
        /// 对泛化做兼容处理
        /// </summary>
        /// <param name="item"></param>
        public void PutX(object item)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if(!(item is T))
            {
                Log.Error($"type not matched! class:{item.GetType().FullName}");
            }
#endif//platform
            Put(item as T);
        }
        /// <summary>
        /// 对泛化做兼容处理
        /// </summary>
        /// <returns></returns>
        public object GetX()
        {
            return Get();
        }
        /// <summary>
        /// 创建若干个对象
        /// </summary>
        /// <param name="capacity"></param>
        public XPool<T> MakeReserve(int capacity)
        {
            capacity = capacity - mPutImpl.Count;
            for(var i = 0; i < capacity;++i)
            {
                T it = null; ;
                if(null == mCreator)
                {
                    it = new T();
                }
                else
                {
                    it = mCreator();
                }
                mPutImpl.Add(it);
            }
            return this;
        }

        void XIPool.MakeReserveItems(int count)
        {
            MakeReserve(count);
        }//MakeReserve
    }//end class
}//end namespace

