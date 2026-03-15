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
    /// char 变量类。
    /// </summary>
    public sealed class VarChar : Variable<char>, IEquatable<VarChar>
    {
        /// <summary>
        /// 初始化 char 变量类的新实例。
        /// </summary>
        public VarChar()
        {
        }

        /// <summary>
        /// 初始化 char 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarChar(char value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 char 到 char 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarChar Create(char value)
        {
            var ret = ObjectPool.Instance.Fetch<VarChar>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 char 变量类到 char 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator char(VarChar value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarChar>(this);
        }
        public bool Equals(VarChar other)
        {
            if (other == null)
                return false;
            return this.Value.Equals(other.Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarChar)obj));
        }
        public override int GetHashCode()
        {
            return Value;
        }
    }
}
