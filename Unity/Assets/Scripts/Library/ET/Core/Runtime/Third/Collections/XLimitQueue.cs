using System;
using System.Collections.Generic;

namespace ET
{
    public class XLimitQueue<T>
    {
        public XLimitQueue(int limit)
        {
            Limit = limit;

            ListLength = Limit + 1;
            mList = new List<T>(ListLength);

            for (int i = 0; i < ListLength; ++i)
            {
                mList.Add(default(T));
            }

            Count = 0;
            StartIndex = 0;
            EndIndex = 0;
        }

        /// <summary>
        /// 如果最早的元素被挤掉
        /// 将会通过这个回调传出去
        /// </summary>
        public Action<T> OnExtruded;

        public int Limit { get; private set; }

        public int Count { get; private set; }

        private List<T> mList;
        private int ListLength;

        private int StartIndex;
        private int EndIndex;

        private int AddIndex(int index)
        {
            int i = index + 1;
            if (i == ListLength)
                i = 0;
            return i;
        }

        public void Enqueue(T item)
        {
            mList[EndIndex] = item;

            EndIndex = AddIndex(EndIndex);
            if (EndIndex == StartIndex)
            {
                T r = mList[StartIndex];
                mList[StartIndex] = default(T);

                StartIndex = AddIndex(StartIndex);
                OnExtruded.Invoke(r);
            }
            else
            {
                ++Count;
            }
        }

        public T Dequeue()
        {
            if (StartIndex == EndIndex)
            {
                throw new Exception("Quene==0 can not Dequeue");
            }
#if DEBUG
            if (Count == 0)
            {
                Log.Error("No Count");
            }
#endif

            T rt = mList[StartIndex];
            mList[StartIndex] = default(T);
            StartIndex = AddIndex(StartIndex);

            --Count;
            return rt;
        }

        public T Peek()
        {
            return mList[StartIndex];
        }
    }
}