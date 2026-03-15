using System.Collections.Generic;

namespace ET
{

    /// <summary>
    /// 基于链表实现的XDictionary
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class XDictionaryLinkedList<K, V> where V : class
    {
        /// <summary>
        /// 为Dictionary定制的内存分配器
        /// </summary>
        public class Allocator: IAllocator<KeyValue<K, V>>
        {
            /// <summary>
            /// 数据存储表
            /// </summary>
            private List<KeyValue<K, V>> mList
                = new List<KeyValue<K, V>>();
            public KeyValue<K, V> Get()
            {
                var ret = default(KeyValue<K, V>);
                var n = mList.Count;
                if (0 < n)
                {
                    ret = mList[n - 1];
                    mList.RemoveAt(n - 1);
                }
                else
                {
                    ret = new KeyValue<K, V>();
                }
                return ret;
            }//Get

            public void Put(KeyValue<K, V> item)
            {
                item.key = default(K);
                item.value = default(V);
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

            public KeyValue<K, V> this[int i]
            {
                get
                {
                    return mList[i];
                }
            }
        }//Allocator
        /// <summary>
        /// 内存分配器
        /// </summary>
        private IAllocator<KeyValue<K, V>> mAllocator
            = new Allocator();

        /// <summary>
        /// 字典
        /// </summary>
        private Dictionary<K, XLinkedList<KeyValue<K, V>>.XListNode> mMap
            = new Dictionary<K, XLinkedList<KeyValue<K, V>>.XListNode>();
        /// <summary>
        /// 链表
        /// </summary>
        private XLinkedList<KeyValue<K, V>> mList = new XLinkedList<KeyValue<K, V>>();
        /// <summary>
        /// 获取列表来遍历
        /// </summary>
        /// <returns></returns>
        public XLinkedList<KeyValue<K,V>> GetList()
        {
            return mList;
        }//GetList


        public void Add(K k,V v)
        {
            if(!mMap.ContainsKey(k))
            {
                var kv = mAllocator.Get();
                kv.key = k;
                kv.value = v;

                mList.AddLast(kv);
                mMap.Add(k, mList.LastNode);
            }
        }
        /// <summary>
        /// 判断是否包含元素
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public bool ContainsKey(K k)
        {
            return mMap.ContainsKey(k);
        }
        /// <summary>
        /// 移除元素
        /// </summary>
        /// <param name="k"></param>
        public void Remove(K k)
        {
            var r = default(XLinkedList<KeyValue<K, V>>.XListNode);
            if(mMap.TryGetValue(k,out r))
            {
                var kv = r.Value;
                mList.RemoveAt(r);
                mMap.Remove(k);
                mAllocator.Put(kv);
            }//if
        }//Remove
        /// <summary>
        /// 查找元素
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool TryGetValue(K k,out V v)
        {
            var r = default(XLinkedList<KeyValue<K, V>>.XListNode);
            var ret = mMap.TryGetValue(k, out r);
            if(null != r)
            {
                v = r.Value.value;
            }
            else
            {
                v = default(V);
            }
            return ret;
        }//TryGetValue
        public int Count
        {
            get
            {
                return mMap.Count;
            }
        }//Count

        /// <summary>
        /// 清除所有的元素
        /// </summary>
        public void Clear()
        {
            mMap.Clear();
            var it = mList.Begin;
            var end = mList.End;
            for(;it != end;it=it.Next)
            {
                mAllocator.Put(it.Value);
            }//for
            mList.Clear();
        }//Clear

    }//XDictionaryLinkedList
}