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
    /// UnityEngine.Vector3 变量类。
    /// </summary>
    public sealed class VarFloat3 : Variable<float3>,IEquatable<VarFloat3>
    {
        /// <summary>
        /// 初始化 UnityEngine.Vector3 变量类的新实例。
        /// </summary>
        public VarFloat3()
        {
        }

        /// <summary>
        /// 初始化 UnityEngine.Vector3 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarFloat3(float3 value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 UnityEngine.Vector3 到 UnityEngine.Vector3 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarFloat3 Create(float3 value)
        {
            var ret = ObjectPool.Instance.Fetch<VarFloat3>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 UnityEngine.Vector3 变量类到 UnityEngine.Vector3 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float3(VarFloat3 value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            this.Value = float3.zero;
            ObjectPool.Instance.Recycle<VarFloat3>(this);
        }
        public bool Equals(VarFloat3 other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarFloat3)obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
