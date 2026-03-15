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
    /// UnityEngine.Vector4 变量类。
    /// </summary>
    public sealed class VarFloat4 : Variable<float4>,IEquatable<VarFloat4>
    {
        /// <summary>
        /// 初始化 UnityEngine.Vector4 变量类的新实例。
        /// </summary>
        public VarFloat4()
        {
        }

        /// <summary>
        /// 初始化 UnityEngine.Vector4 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarFloat4(float4 value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 UnityEngine.Vector4 到 UnityEngine.Vector4 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarFloat4 Create(float4 value)
        {
            var ret = ObjectPool.Instance.Fetch<VarFloat4>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 UnityEngine.Vector4 变量类到 UnityEngine.Vector4 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float4(VarFloat4 value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarFloat4>(this);
        }
        public bool Equals(VarFloat4 other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarFloat4)obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
