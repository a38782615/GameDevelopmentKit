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
    /// ulong 变量类。
    /// </summary>
    public sealed class VarULong : Variable<ulong>, IEquatable<VarULong>
    {
        /// <summary>
        /// 初始化 ulong 变量类的新实例。
        /// </summary>
        public VarULong()
        {
        }

        /// <summary>
        /// 初始化 ulong 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarULong(ulong value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 ulong 到 ulong 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarULong Create(ulong value)
        {
            var ret = ObjectPool.Instance.Fetch<VarULong>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 ulong 变量类到 ulong 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ulong(VarULong value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarULong>(this);
        }
        public bool Equals(VarULong other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarULong)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
