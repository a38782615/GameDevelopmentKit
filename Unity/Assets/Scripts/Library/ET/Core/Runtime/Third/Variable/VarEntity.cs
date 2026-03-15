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
    /// string 变量类。
    /// </summary>
    public sealed class VarEntityRef : Variable<EntityRef<Entity>>,IEquatable<VarEntityRef>
    {
        /// <summary>
        /// 初始化 string 变量类的新实例。
        /// </summary>
        public VarEntityRef()
        {
        }

        /// <summary>
        /// 初始化 string 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarEntityRef(EntityRef<Entity> value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 string 到 string 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarEntityRef Create(EntityRef<Entity> value)
        {
            var ret = ObjectPool.Instance.Fetch<VarEntityRef>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 string 变量类到 string 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator EntityRef<Entity>(VarEntityRef value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            this.Value = null;
            ObjectPool.Instance.Recycle<VarEntityRef>(this);
        }
        public bool Equals(VarEntityRef other)
        {
            if (other == null)
                return false;
            return this.Value.Equals((other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((VarEntityRef)obj));
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
