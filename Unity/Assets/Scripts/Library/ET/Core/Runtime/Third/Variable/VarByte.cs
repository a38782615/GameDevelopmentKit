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
    /// byte 变量类。
    /// </summary>
    public sealed class VarByte : Variable<byte>, IEquatable<VarByte>
    {
        /// <summary>
        /// 初始化 byte 变量类的新实例。
        /// </summary>
        public VarByte()
        {
        }

        /// <summary>
        /// 初始化 byte 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarByte(byte value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 byte 到 byte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarByte Create(byte value)
        {
            var ret = ObjectPool.Instance.Fetch<VarByte>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 byte 变量类到 byte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator byte(VarByte value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarByte>(this);
        }
        public bool Equals(VarByte other)
        {
            if (other == null)
                return false;
            return this.Value.Equals(other.Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarByte)obj));
        }
        public override int GetHashCode()
        {
            return Value;
        }
    }
}
