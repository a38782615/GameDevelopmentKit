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
    /// uint 变量类。
    /// </summary>
    public sealed class VarUInt : Variable<uint>, IEquatable<VarUInt>
    {
        /// <summary>
        /// 初始化 uint 变量类的新实例。
        /// </summary>
        public VarUInt()
        {
        }

        /// <summary>
        /// 初始化 uint 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarUInt(uint value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 uint 到 uint 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarUInt Create(uint value)
        {
            var ret = ObjectPool.Instance.Fetch<VarUInt>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 uint 变量类到 uint 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator uint(VarUInt value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarUInt>(this);
        }
        public bool Equals(VarUInt other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarUInt)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
