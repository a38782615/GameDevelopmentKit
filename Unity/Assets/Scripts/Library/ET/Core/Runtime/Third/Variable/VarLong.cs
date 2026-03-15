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
    /// long 变量类。
    /// </summary>
    public sealed class VarLong : Variable<long>,IEquatable<VarLong>
    {
        /// <summary>
        /// 初始化 long 变量类的新实例。
        /// </summary>
        public VarLong()
        {
        }

        /// <summary>
        /// 初始化 long 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarLong(long value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 long 到 long 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarLong Create(long value)
        {
            var ret = ObjectPool.Instance.Fetch<VarLong>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 long 变量类到 long 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator long(VarLong value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarLong>(this);
        }
        public bool Equals(VarLong other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarLong)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
