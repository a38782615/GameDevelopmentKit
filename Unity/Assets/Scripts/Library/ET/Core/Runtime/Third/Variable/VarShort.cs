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
    /// short 变量类。
    /// </summary>
    public sealed class VarShort : Variable<short>,IEquatable<VarShort>
    {
        /// <summary>
        /// 初始化 short 变量类的新实例。
        /// </summary>
        public VarShort()
        {
        }

        /// <summary>
        /// 初始化 short 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarShort(short value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 short 到 short 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarShort Create(short value)
        {
            var ret = ObjectPool.Instance.Fetch<VarShort>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 short 变量类到 short 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator short(VarShort value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarShort>(this);
        }
        public bool Equals(VarShort other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarShort)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
