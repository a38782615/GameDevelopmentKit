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
    /// sbyte 变量类。
    /// </summary>
    public sealed class VarSByte : Variable<sbyte>,IEquatable<VarSByte>
    {
        /// <summary>
        /// 初始化 sbyte 变量类的新实例。
        /// </summary>
        public VarSByte()
        {
        }

        /// <summary>
        /// 初始化 sbyte 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarSByte(sbyte value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 sbyte 到 sbyte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarSByte Create(sbyte value)
        {
            var ret = ObjectPool.Instance.Fetch<VarSByte>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 sbyte 变量类到 sbyte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator sbyte(VarSByte value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarSByte>(this);
        }
        public bool Equals(VarSByte other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarSByte)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
