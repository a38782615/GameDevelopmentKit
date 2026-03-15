//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace ET
{
    /// <summary>
    /// string 变量类。
    /// </summary>
    public sealed class VarString : Variable<string>,IEquatable<VarString>
    {
        /// <summary>
        /// 初始化 string 变量类的新实例。
        /// </summary>
        public VarString()
        {
        }

        /// <summary>
        /// 初始化 string 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarString(string value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 string 到 string 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarString Create(string value)
        {
            var ret = ObjectPool.Instance.Fetch<VarString>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 string 变量类到 string 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator string(VarString value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarString>(this);
        }
        public bool Equals(VarString other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarString)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
