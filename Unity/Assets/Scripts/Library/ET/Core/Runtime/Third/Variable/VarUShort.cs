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
    /// ushort 变量类。
    /// </summary>
    public sealed class VarUShort : Variable<ushort>, IEquatable<VarUShort>
    {
        /// <summary>
        /// 初始化 ushort 变量类的新实例。
        /// </summary>
        public VarUShort()
        {
        }

        /// <summary>
        /// 初始化 ushort 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarUShort(ushort value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 ushort 到 ushort 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarUShort Create(ushort value)
        {
            var ret = ObjectPool.Instance.Fetch<VarUShort>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 short 变量类到 short 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ushort(VarUShort value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarUShort>(this);
        }
        public bool Equals(VarUShort other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarUShort)obj));
        }
        public override int GetHashCode()
        {
            return Value;
        }
    }
}
