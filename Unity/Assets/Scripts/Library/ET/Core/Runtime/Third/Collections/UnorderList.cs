using System;
using System.Collections;
using System.Collections.Generic;

namespace ET
{

    /// <summary>
    /// 慎用，这个类是设计给CellGrid专用的，或者在
    /// 对性能要求比较苛刻的环境下使用
    /// 其他一般情景请使用XList<T>
    /// 这个类并不保证插入删除的操作得到有序的元素
    /// 采用的加速优化是删除i位置元素会将最后一个位置的元素顶上
    /// 然后删除最后一个元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UnorderList<T> : IList<T> where T: class
    {
        public UnorderList()
        {
            mCapacity = 0;
        }//UnorderList

        public UnorderList(int capacity)
        {
            if(capacity>0)
            {
                mCapacity = capacity;
                mImpl = new T[capacity];
            }//if
        }//UnorderList

        /// <summary>
        /// 实现类
        /// </summary>
        private T[] mImpl;
        /// <summary>
        /// 查询元素
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                return mImpl[index];
            }//get
            set
            {
                mImpl[index] = value;
            }//set
        }//this
        /// <summary>
        /// 获取数组元素当前个数
        /// </summary>
        private int mCount;
        /// <summary>
        /// 数组的长度
        /// </summary>
        private int mCapacity;
        /// <summary>
        /// 获取当前数组的当前最大容量
        /// </summary>
        public int Capacity
        {
            get
            {
                return mCapacity;
            }//get
        }//Capacity

        public int Count
        {
            get
            {
                return mCount;
            }//get
        }//Count

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }//get
        }//IsReadOnly

        public void Add(T item)
        {
            if(null == mImpl)
            {
                mCapacity = 4;
                mImpl = new T[mCapacity];
                mImpl[0] = item;
                ++mCount;
            }
            else
            {
                if(mCount>=mCapacity)
                {
                    mCapacity += mCapacity;
                    Array.Resize(ref mImpl, mCapacity);
                }//if
                mImpl[mCount] = item;
                ++mCount;
            }//else
        }//Add
        /// <summary>
        /// 清除所有的元素但是不消除引用
        /// </summary>
        public void Clear()
        {
            mCount = 0;
        }//Clear
        /// <summary>
        /// 清除所有的元素并且消除引用
        /// </summary>
        public void ClearCompletely()
        {
            if(null != mImpl)
            {
                for (int i = 0, n = mCount; i < n; ++i)
                {
                    mImpl[i] = null;
                }//for
            }//if
        }//ClearCompletely
        /// <summary>
        /// 检测是否包含某个元素
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            if(null == mImpl) { return false; }
            for(int i=mCount-1;i>=0;--i)
            {
                if (mImpl[i] == item)
                {
                    return true;
                }//if
            }//for
            return false;
        }//Contains
        /// <summary>
        /// 不允许使用
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }//CopyTo
        /// <summary>
        /// 不允许使用
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 从后往前匹配元素，如果匹配成功返回下标
        /// 否则返回-1
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            if(null == mImpl)
            {
                return -1;
            }//if
            for(int i=mCount-1;i>=0;--i)
            {
                if(mImpl[i]==item)
                {
                    return i;
                }//if
            }//for
            return -1;
        }//IndexOf
        /// <summary>
        /// 不允许使用
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }//Insert
        /// <summary>
        /// 删除元素
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            if(null == mImpl)
            {
                return false;
            }//if
            for(int i=mCount;i>=0;--i)
            {
                if(mImpl[i]==item)
                {
                    RemoveAt(i);
                    return true;
                }//if
            }//for
            return false;
        }//Remove
        /// <summary>
        /// 移除特定位置的元素
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if(null != mImpl)
            {
                mImpl[index] = mImpl[mCount - 1];
                --mCount;
            }//if
        }//RemoveAt
        /// <summary>
        /// 不允许使用
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }//GetEnumerator
    }//UnorderList
}