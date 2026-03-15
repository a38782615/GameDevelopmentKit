//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2020 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
 
namespace ET
{
    /// <summary>
    /// double 变量类。
    /// </summary>
    public sealed class VarDouble : Variable<double>
    {
        /// <summary>
        /// 初始化 double 变量类的新实例。
        /// </summary>
        public VarDouble()
        {
        }

        /// <summary>
        /// 初始化 double 变量类的新实例。
        /// </summary>
        /// <param name="value">值。</param>
        public VarDouble(double value)
            : base(value)
        {
        }

        /// <summary>
        /// 从 double 到 double 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static VarDouble Create(double value)
        {
            var ret = ObjectPool.Instance.Fetch<VarDouble>();
            ret.Value = value;
            return ret;
        }

        /// <summary>
        /// 从 double 变量类到 double 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator double(VarDouble value)
        {
            return value.Value;
        }
        public override void Dispose()
        {
            base.Dispose();
            ObjectPool.Instance.Recycle<VarDouble>(this);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
