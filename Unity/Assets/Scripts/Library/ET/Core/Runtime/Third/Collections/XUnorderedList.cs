using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 无序的线性表，如果需要有序的请使用XArray，
    /// 可以实现特定情况下以O(1)的时间复杂度移除元素
    /// 支持在迭代过程删除元素以及插入元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XUnorderedList<T>
    {
        /// <summary>
        /// 实现的容器类
        /// </summary>
        public List<T> mImpl;
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity"></param>
        public XUnorderedList(int capacity = 0)
        {
            Count = 0;
            mImpl = new List<T>(capacity);
        }
        /// <summary>
        /// 记录元素个数
        /// </summary>
        public int Count { get; private set; }
        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            ++Count;
            mImpl.Add(item);
        }
        /// <summary>
        /// 记录迭代器位置
        /// </summary>
        private int mIter;
        /// <summary>
        /// 开始遍历
        /// </summary>
        public void ForEachBegin()
        {
            mIter = -1;
        }
        /// <summary>
        /// 判断是否可以继续遍历
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            ++mIter;
            return mIter < Count;
        }
        /// <summary>
        /// 获取当前的元素
        /// </summary>
        public T Current
        {
            get
            {
                return mImpl[mIter];
            }
        }
        /// <summary>
        /// 移除迭代器的位置的元素，并且用最后一个元素顶上
        /// </summary>
        public void RemoveCurrent()
        {
            --Count;
            mImpl[mIter] = mImpl[Count];
            mImpl.RemoveAt(Count);
            --mIter;
        }
        /// <summary>
        /// 要删除的元素
        /// </summary>
        public List<T> mDeleteItems = new List<T>();
        /// <summary>
        /// 删除缓存数据中的元素
        /// </summary>
        public void DeleteCached()
        {
            if(mDeleteItems.Count > 0)
            {
                for(int i=0,n= mDeleteItems.Count;i<n;++i)
                {
                    var it = mDeleteItems[i];
                    ForEachBegin();
                    while(MoveNext())
                    {
                        if(Current.Equals(it))
                        {
                            RemoveCurrent();
                            break;//while
                        }
                    }//while
                }//for
            }//if
            Count = mImpl.Count;
            mDeleteItems.Clear();
        }

    }//end class

}//end namespace

