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
    /// System.DateTime 变量类。
    /// </summary>
    public sealed class VarDateTime : Variable<DateTime>
    {
        /// <summary>
        /// 初始化 System.DateTime 变量类的新实例。
        /// </summary>
        public VarDateTime()
        {
        }

        /// <summary>
        /// 初始化 System.DateTime 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarDateTime(DateTime value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 System.DateTime 到 System.DateTime 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarDateTime Create(DateTime value)
        {
            var ret = ObjectPool.Instance.Fetch<VarDateTime>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 System.DateTime 变量类到 System.DateTime 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator DateTime(VarDateTime value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarDateTime>(this);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
