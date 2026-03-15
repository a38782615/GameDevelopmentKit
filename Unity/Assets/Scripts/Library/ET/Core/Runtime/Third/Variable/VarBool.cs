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
    /// bool 变量类。
    /// </summary>
    public sealed class VarBool : Variable<bool>, IEquatable<VarBool>
    {
        /// <summary>
        /// 初始化 bool 变量类的新实例。
        /// </summary>
        public VarBool()
        {
        }

        /// <summary>
        /// 初始化 bool 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarBool(bool value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 bool 到 bool 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarBool Create(bool value)
        {
            var ret = ObjectPool.Instance.Fetch<VarBool>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 bool 变量类到 bool 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator bool(VarBool value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            this.Value = false;
            ObjectPool.Instance.Recycle<VarBool>(this);
        }

        public bool Equals(VarBool other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals((obj as VarBool));
        }
        public override int GetHashCode()
        {
            return Value ? 1 : 0;
        }
    }
}
