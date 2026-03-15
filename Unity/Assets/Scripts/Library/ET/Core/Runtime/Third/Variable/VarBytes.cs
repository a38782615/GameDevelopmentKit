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
    /// byte[] 变量类。
    /// </summary>
    public sealed class VarBytes : Variable<byte[]>, IEquatable<VarBytes>
    {
        /// <summary>
        /// 初始化 byte[] 变量类的新实例。
        /// </summary>
        public VarBytes()
        {
        }

        /// <summary>
        /// 初始化 byte[] 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarBytes(byte[] value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 byte[] 到 byte[] 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarBytes Create(byte[] value)
        {
            var ret = ObjectPool.Instance.Fetch<VarBytes>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 byte[] 变量类到 byte[] 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator byte[](VarBytes value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            ObjectPool.Instance.Recycle<VarBytes>(this);
        }
        public bool Equals(VarBytes other)
        {
            if (other == null)
                return false;
            if (this.Value.Length != other.Value.Length)
                return false;
            return this.Value.Equals(other.Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarBytes)obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
