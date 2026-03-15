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
    /// char[] 变量类。
    /// </summary>
    public sealed class VarChars : Variable<char[]>, IEquatable<VarChars>
    {
        /// <summary>
        /// 初始化 char[] 变量类的新实例。
        /// </summary>
        public VarChars()
        {
        }

        /// <summary>
        /// 初始化 char[] 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarChars(char[] value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 char[] 到 char[] 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarChars Create(char[] value)
        {
            var ret = ObjectPool.Instance.Fetch<VarChars>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 char[] 变量类到 char[] 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator char[](VarChars value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarChars>(this);
        }
        public bool Equals(VarChars other)
        {
            if (other == null)
                return false;
            return this.Value.Equals(other.Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarChars)obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
