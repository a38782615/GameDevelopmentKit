using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 不重复元素的数组,会阻止重复添加相同的元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UniList<T> : List<T>
        where T: class,ISetItem
    {
        /// <summary>
        /// 元素集合
        /// </summary>
        private HashSet<T> mSet;
        /// <summary>
        /// 初始容量
        /// </summary>
        /// <param name="capacity"></param>
        public UniList(int capacity = 1024):base(capacity)
        {
            mSet = new HashSet<T>();
        }//SimpleList
        /// <summary>
        /// 往数组中添加元素
        /// </summary>
        /// <param name="item"></param>
        public new void Add(T item)
        {
            if(mSet.Contains(item))
            {
                return;
            }
            else
            {
                mSet.Add(item);
                base.Add(item);
            }
        }//Add
        /// <summary>
        /// 清除所有的元素
        /// </summary>
        public new void Clear()
        {
            mSet.Clear();
            base.Clear();
        }//Clear
    }//SimpleList
    /// <summary>
    /// 这个类只是给CellGrid使用的，其他地方请使用HashSet
    /// 提供Add和Contains方法，但是不提供Remove方法,因为Remove的开销比较大
    /// ClearUsedItems可以提供最优的Clear实现
    /// </summary>
    public class XSet
    {
        public bool[] mItems;
        public List<int> mUsedItems;

        public int mCapacity;

        public XSet(int capacity)
        {
            mCapacity = capacity;
            mItems = new bool[capacity];
            mUsedItems = new List<int>(capacity);
        }

        /// <summary>
        /// 重新分配内存
        /// </summary>
        /// <param name="capacity"></param>
        public void ReAllocate(int capacity)
        {
            mCapacity = capacity;
            mItems = new bool[capacity];
            mUsedItems.Clear();
        }//ReAllocate

        public bool Contains(int it)
        {
            return mItems[it];
        }

        public bool CheckAndAdd(int it)
        {
            var tmp = mItems[it];
            if (!tmp)
            {
                mItems[it] = true;
                mUsedItems.Add(it);
            }
            return tmp;
        }

        public void Add(int it)
        {
            if(!mItems[it])
            {
                mItems[it] = true;
                mUsedItems.Add(it);
            }
        }

        public void ClearUsedItems()
        {
            for(int I=0,n=mUsedItems.Count;I<n;++I)
            {
                var i = mUsedItems[I];
                mItems[i] = false;
            }
            mUsedItems.Clear();
        }
    }//XSet

    public interface ISetItem
    {
        int Order { get; set; }
    }

    public class XSet<T> where T : ISetItem
    {
        public bool[] mItems;
        public List<int> mUsedItems;

        public XSet(int capacity)
        {
            mItems = new bool[capacity];
            mUsedItems = new List<int>(capacity);
        }//XSet

        public bool Contains(T it)
        {
            return mItems[it.Order];
        }//Contains


        public void Add(T it)
        {
            var i = it.Order;
            if (!mItems[i])
            {
                mItems[i] = true;
                mUsedItems.Add(i);
            }
        }

        public void ClearUsedItems()
        {
            for (int I = 0, n = mUsedItems.Count; I < n; ++I)
            {
                var i = mUsedItems[I];
                mItems[i] = false;
            }//for
            mUsedItems.Clear();
        }//ClearUsedItems
    }//class XSet<T>
}//end namespace PLD
