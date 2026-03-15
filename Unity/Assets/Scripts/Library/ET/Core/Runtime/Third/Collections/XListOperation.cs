using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 记录List的添加和删除操作
    /// 之前这个内部的内容是在XArray中的实现，
    /// 但是为了通用，将它分离出来可以单独使用
    /// 清理完记得调用Clear哇
    /// 
    /// 谭仲添
    /// </summary>
    public class XListOperation<T>
    {
#if DEBUG
        /// <summary>
        /// 如果是true标识添加，false表示移除
        /// </summary>
        public List<bool> mOpTag { get; private set; }
#else
        public List<bool> mOpTag = null;
#endif

#if DEBUG
        public List<T> mItems { get; private set; }
#else
        public List<T> mItems = null;
#endif

#if DEBUG
        /// <summary>
        /// 标记是否需要更新
        /// </summary>
        public bool Dirty { get; private set; }
#else
        public bool Dirty  = false;
#endif

#if DEBUG
        /// <summary>
        /// 遍历的元素的个数
        /// </summary>
        public int Count { get; private set; }
#else
        public int Count = 0;
#endif
        /// <summary>
        /// 构造方法
        /// </summary>
        public XListOperation()
        {
            mOpTag = new List<bool>();
            mItems = new List<T>();
        }//XListOperation

        /// <summary>
        /// 清理数据
        /// </summary>
        public void Clear()
        {
            mOpTag.Clear();
            mItems.Clear();
            Count = 0;
            Dirty = false;
        }//Clear

        /// <summary>
        /// 刷新列表里面的内容
        /// </summary>
        /// <param name="L"></param>
        public void Flush(List<T> L)
        {
            try
            {
                if (Dirty)
                {
                    var L1 = mOpTag;
                    var L2 = mItems;
                    for (int i = 0, n = Count; i < n; ++i)
                    {
                        var v = L2[i];
                        if (L1[i])
                        {
                            L.Add(v);
                        }//if
                        else
                        {
                            L.Remove(v);
                        }//else
                    }//for
                    Clear();
                }//Dirty
            }catch(Exception)
            {
                
            }
        }//Flush

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            mOpTag.Add(true);
            mItems.Add(item);
            ++Count;
            Dirty = true;
        }//Add

        /// <summary>
        /// 添加要移除的元素
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            mOpTag.Add(false);
            mItems.Add(item);
            ++Count;
            Dirty = true;
        }//Remove

    }//class
}//namespace PLD


