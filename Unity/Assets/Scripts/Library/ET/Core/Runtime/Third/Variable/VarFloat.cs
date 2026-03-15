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
    /// float 变量类。
    /// </summary>
    public sealed class VarFloat : Variable<float>,IEquatable<VarFloat>
    {
        /// <summary>
        /// 初始化 float 变量类的新实例。
        /// </summary>
        public VarFloat()
        {
        }

        /// <summary>
        /// 初始化 float 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarFloat(float value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 float 到 float 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarFloat Create(float value)
        {
            var ret = ObjectPool.Instance.Fetch<VarFloat>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 float 变量类到 float 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float(VarFloat value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarFloat>(this);
        }
        public bool Equals(VarFloat other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(obj as VarFloat);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
