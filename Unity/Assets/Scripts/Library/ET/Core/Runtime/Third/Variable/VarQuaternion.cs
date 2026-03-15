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
    /// </summary>
    public sealed class VarQuaternion : Variable<quaternion>,IEquatable<VarQuaternion>
    {
        /// <summary>
        /// 初始化 UnityEngine.Quaternion 变量类的新实例。
        /// </summary>
        public VarQuaternion()
        {
        }

        /// <summary>
        /// 初始化 UnityEngine.Quaternion 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarQuaternion(quaternion value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 UnityEngine.Quaternion 到 UnityEngine.Quaternion 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarQuaternion Create(quaternion value)
        {
            var ret = ObjectPool.Instance.Fetch<VarQuaternion>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 UnityEngine.Quaternion 变量类到 UnityEngine.Quaternion 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator quaternion(VarQuaternion value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarQuaternion>(this);
        }
        
        public bool Equals(VarQuaternion other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarQuaternion)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
