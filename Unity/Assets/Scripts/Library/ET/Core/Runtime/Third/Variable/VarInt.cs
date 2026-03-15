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
    /// int 变量类。
    /// </summary>
    public sealed class VarInt : Variable<int>,IEquatable<VarInt>
    {
        /// <summary>
        /// 初始化 int 变量类的新实例。
        /// </summary>
        public VarInt()
        {
        }

        /// <summary>
        /// 初始化 int 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarInt(int value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 int 到 int 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarInt Create(int value)
        {
            var ret = ObjectPool.Instance.Fetch<VarInt>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 int 变量类到 int 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator int(VarInt value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarInt>(this);
        }
        public bool Equals(VarInt other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarInt )obj));
        }
        public override int GetHashCode()
        {
            return Value;
        }
    }
}
