//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// bool 变量类。
    /// </summary>
    public class VarFun : Object, IDisposable
    {
        [BsonIgnore] public Func<bool> Func;
        public VarBool Input;
        [BsonIgnore] public Object UserData;

        /// <summary>
        /// 初始化 bool 变量类的新实例。
        /// </summary>
        public VarFun()
        {
        }

        private bool m_FromPool;

        /// <summary>
        /// 从 bool 到 bool 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarFun Create(bool input = false, Func<bool> func = null)
        {
            var ret = ObjectPool.Instance.Fetch<VarFun>();
            ret.Func = func;
            ret.Input = VarBool.Create(input);
            ret.m_FromPool = true;
            ret.UserData = null;
            return ret;
        }

        public void Dispose()
        {
            Func = null;
            Input?.Dispose();
            Input = null;
            UserData = null;
            if (m_FromPool)
            {
                ObjectPool.Instance.Recycle<VarFun>(this);
            }
        }

        public bool Equals(VarFun other)
        {
            if (other == null)
                return false;
            return GetResult() == other.GetResult();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals((obj as VarFun));
        }

        public override int GetHashCode()
        {
            return GetResult() ? 1 : 0;
        }

        public bool GetResult()
        {
            var ret = Func?.Invoke() ?? Input.Value;
            return ret;
        }

        public T As<T>() where T : Object
        {
            return UserData as T;
        }
    }
}
