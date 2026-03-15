using System;
using System.Collections.Generic;


namespace ET
{
    /// <summary>
    /// 内存分配器的接口
    /// 
    /// 谭仲添
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAllocator<T> where T:class,new()
    {
        T Get();
        void Put(T item);

        int Count { get; }
    }//Allocator



    /// <summary>
    /// 自定义链表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XLinkedList<T> : IDisposable where T : class
    {
        public static XLinkedList<T> Create()
        {
            var rt = ObjectPool.Instance.Fetch<XLinkedList<T>>();
            rt.Clear();
            return rt;
        }

        public void Dispose()
        {
            this.Clear();
            ObjectPool.Instance.Recycle<XLinkedList<T>>(this);
        }
        
        public class XListNode
        {
#if DEBUG
            public T Value { get; internal set; }
#else
            public T Value ;
#endif
#if DEBUG
            public XLinkedList<T> Parent { get; internal set; }
#endif
#if DEBUG
            public XListNode Next { get; internal set; }
#else
            public XListNode Next ;
#endif

#if DEBUG
            public XListNode Prev { get; internal set; }
#else
            public XListNode Prev ;
#endif
            /// <summary>
            /// 重置数据
            /// </summary>
            internal void Reset()
            {
                Value = null;
                Prev = null;
                Next = null;

#if DEBUG
                Parent = null;
#endif
            }//Reset
             /// <summary>
             /// 将this和node相连接
             /// </summary>
             /// <param name="node"></param>
            internal void Connect(XListNode node)
            {
                node.Prev = this;
                Next = node;
            }//Connect

            /// <summary>
            /// 将this连接向node，将node连接向this.Next
            /// </summary>
            /// <param name="node"></param>
            internal void Insert(XListNode node)
            {
                node.Next = Next;
                Next.Prev = node;

                Next = node;
                node.Prev = this;
            }//Insert

            /// <summary>
            /// 将node从链表中移除，
            /// 并且将node的前面的节点和node后面的节点连接
            /// </summary>
            /// <param name="node"></param>
            internal void Discard()
            {
                Prev.Connect(Next);
                Value = null;
                Prev = Next = null;
            }//Discard
        }//XListNode
        /// <summary>
        /// 内存分配器
        /// </summary>
        public class Allocator : IAllocator<XListNode>
        {
            /// <summary>
            /// 数据存储表
            /// </summary>
            private List<XListNode> mList
                = new List<XListNode>();
            public XListNode Get()
            {
                var ret = default(XListNode);
                var n = mList.Count;
                if (0 < n)
                {
                    ret = mList[n - 1];
                    mList.RemoveAt(n - 1);
                }
                else
                {
                    ret = new XListNode();
                }
                return ret;
            }//Get

            public void Put(XListNode item)
            {
                item.Reset();
                mList.Add(item);
            }//Put
            /// <summary>
            /// 内存分配器里面的元素个数
            /// </summary>
            public int Count
            {
                get
                {
                    return mList.Count;
                }//get
            }//Count

            public XListNode this[int i]
            {
                get
                {
                    return mList[i];
                }
            }
        }//Allocator

#if DEBUG
        /// <summary>
        /// 数据容量
        /// </summary>
        public int Count { get; private set; }
#else
        public int Count ;
#endif
        /// <summary>
        /// 链表的节点分配器
        /// </summary>
        private IAllocator<XListNode> mAllocator;
        /// <summary>
        /// 获取内存分配器
        /// </summary>
        /// <returns></returns>
        public IAllocator<XListNode> GetAllocator()
        {
            return mAllocator;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="allocator"></param>
        public XLinkedList(int capacity, IAllocator<XListNode> allocator = null)
        {
            if (null == allocator)
            {
                mAllocator = new Allocator();
            }//if
            else
            {
                mAllocator = allocator;
            }

            mHead = mAllocator.Get();
            mHead.Connect(mHead);

            if(capacity>0)
            {
                for(int i=0,n=capacity;i<n;++i)
                {
                    mAllocator.Put(new XListNode());
                }//for
            }
            Count = 0;
        }//XLinkedList

        public XLinkedList():this(0,null)
        {
            
        }

        public XLinkedList(IAllocator<XListNode> allocator = null)
            :this(0,allocator)
        {

        }

        /// <summary>
        /// 头节点
        /// </summary>
        private XListNode mHead;

        /// <summary>
        /// 在尾部插入节点
        /// 返回的是新插入的节点
        /// </summary>
        /// <param name="val"></param>
        public XListNode AddLast(T val)
        {
            var node = mAllocator.Get();
#if DEBUG
            node.Parent = (this);
#endif

            node.Value = (val);

            mHead.Prev.Insert(node);
            ++Count;
            return node;
        }//Add

        public XListNode AddBefore(XListNode node, T val)
        {
            var newNode = mAllocator.Get();
            newNode.Value = (val);
            
            newNode.Next = node;
            newNode.Prev = node.Prev;
            
            node.Prev.Next = newNode;
            node.Prev = newNode;

            ++Count;
            return newNode;
        }

        /// <summary>
        /// 移除元素,返回的是移除的元素的Prev
        /// </summary>
        /// <param name="val"></param>
        public XListNode Remove(T val)
        {
            var node = Find(val);
            if (node == null)
            {
                return null;
            }
            var ret = node.Prev;
            if (node != mHead)
            {
                node.Discard();
                mAllocator.Put(node);
                --Count;
            }//if
            return ret;
        }//Remove
        /// <summary>
        /// 清理所有的元素
        /// </summary>
        public void Clear()
        {
            var end = End;
            sTmp.Clear();
            //用最快的方法将
            for (var it = Begin; it != end; it = it.Next)
            {
                sTmp.Add(it);
            }//for
            mHead.Connect(mHead);
            //消除引用
            for (int i = 0, n = sTmp.Count; i < n; ++i)
            {
                mAllocator.Put(sTmp[i]);
            }//for
            sTmp.Clear();
            Count = 0;
        }//Clear

        /// <summary>
        /// 清理用的交换缓存
        /// </summary>
        [StaticField]
        private static List<XListNode> sTmp
            = new List<XListNode>();

        /// <summary>
        /// 移除某个位置的元素,并返回它的Next节点
        /// </summary>
        /// <param name="val"></param>
        public XListNode RemoveAt(XListNode val)
        {
#if DEBUG
            if (val.Parent != this)
            {
                Log.Error("非法移除节点，Parent不一致");
                return mHead;
            }
            if (val == mHead)
            {
                Log.Error("非法移除节点，禁止移除Head");
                return mHead;
            }
#endif
            if (null == val.Prev)
            {
                return mHead;//保护
            }
            var ret = val.Next;
            val.Discard();
            mAllocator.Put(val);
            --Count;
            return ret;
        }//RemoveAt

        /// <summary>
        /// 检测链表中是否包含某个元素
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contains(T val)
        {
            var ret = false;
            var it = Begin;
            var end = End;
            for (; it != end; it = it.Next)
            {
                if (it.Value == val)
                {
                    ret = true;
                    break;
                }//if
            }//for
            return ret;
        }//Contains

        /// <summary>
        /// 查找val所在的节点的位置
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public XListNode Find(T val)
        {
            var ret = default(XListNode);
            var it = Begin;
            var end = End;
            for (; it != end; it = it.Next)
            {
                if (it.Value == val)
                {
                    ret = it;
                    break;
                }//if
            }//for
            return ret;
        }//Find

        public XListNode Find(Predicate<XListNode> predicate)
        {
            var ret = default(XListNode);
            var it = Begin;
            var end = End;
            for (; it != end; it = it.Next)
            {
                if (predicate.Invoke(it))
                {
                    ret = it;
                    break;
                }//if
            }//for
            return ret;
        }

        /// <summary>
        /// 获取最后一个有效节点
        /// </summary>
        public XListNode LastNode
        {
            get
            {
                return mHead.Prev;
            }//get
        }//LastNode

        /// <summary>
        /// 获取第一个有效节点
        /// </summary>
        public XListNode First
        {
            get
            {
                return mHead.Next;
            }//get
        }//FirstNode

        /// <summary>
        /// 结尾的迭代器
        /// </summary>
        public XListNode End
        {
            get
            {
                return mHead;
            }//get
        }//End

        /// <summary>
        /// 开始的迭代器
        /// </summary>
        public XListNode Begin
        {
            get
            {
                return mHead.Next;
            }//get
        }//Begin

    }//XLinkedList

}//namespace PLD

