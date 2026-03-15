//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// UnityEngine.Vector2 变量类。
    /// </summary>
    public sealed class VarFloat2 : Variable<float2>,IEquatable<VarFloat2>
    {
        /// <summary>
        /// 初始化 UnityEngine.Vector2 变量类的新实例。
        /// </summary>
        public VarFloat2()
        {
        }

        /// <summary>
        /// 初始化 UnityEngine.Vector2 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarFloat2(float2 value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 UnityEngine.Vector2 到 UnityEngine.Vector2 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarFloat2 Create(float2 value)
        {
            var ret = ObjectPool.Instance.Fetch<VarFloat2>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 UnityEngine.Vector2 变量类到 UnityEngine.Vector2 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float2(VarFloat2 value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarFloat2>(this);
        }
        public bool Equals(VarFloat2 other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarFloat2)obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
