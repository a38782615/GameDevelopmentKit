using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 针对Kernal和View层通讯的字符串(业务使用)和数字(协议通讯用)的
    /// 映射表，以减少gc alloc的产生
    /// 
    /// </summary>
    public class KVStringMap
    {
        private KVStringMap()
        {
            Str2Int = new Dictionary<string, short>();
            Int2Str = new Dictionary<short, string>();
        }//KVStringMap
        /// <summary>
        /// 标记这个对象是否已经初始化过了
        /// </summary>
        /// <summary>
        /// 将short转化为string
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string Find(short i)
        {
            
            var ret = default(string);
            if (!Int2Str.TryGetValue(i, out ret))
            {
                ret = string.Empty;
            }//if
            return ret;
        }//Find
        /// <summary>
        /// 将字符串转化为short
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public short Find(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return -1;
            }//if
            short ret = 0;
            if (!Str2Int.TryGetValue(str, out ret))
            {
                return -1;
            }//if
            return ret;
        }//Find

        /// <summary>
        /// 初始化，需要在合适的地方调用
        /// </summary>
        public void Init()
        {
        }//Init

        public void Release()
        {
            Str2Int.Clear();
            Int2Str.Clear();
        }

        /// <summary>
        /// 序号
        /// </summary>
        private short mOrder = 1;
        /// <summary>
        /// 添加元素到字典里面
        /// </summary>
        /// <param name="str"></param>
        /// <param name="i"></param>
        private void Add(string str)
        {
            var i = mOrder;
            if (!Str2Int.ContainsKey(str))
            {
                Str2Int.Add(str, i);
                Int2Str.Add(i, str);
                ++mOrder;
            }//if
        }//Add
        
        public Dictionary<string,short> Str2Int ;
        
        public Dictionary<short,string> Int2Str;
        [StaticField]
        public static KVStringMap Current ;
        

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <returns></returns>
        public static KVStringMap Create()
        {
            if (null == Current)
            {
                Current = new KVStringMap();
                Current.Init();
            }//if
            return Current;
        }//Create

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void CleanUp()
        {
            if (Current != null)
            {
                Current.Release();
                Current = null;
            }
        }//CleanUp
    }//KVStringMap

}//namespace PLD
