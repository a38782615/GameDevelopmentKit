// Decompiled with JetBrains decompiler
// Type: GameFramework.Variable`1
// Assembly: GameFramework, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 40B9F625-6AD5-4348-B9BB-597BC306BD6E
// Assembly location: D:\work\NEW5G\develop\client\My5G\Assets\UnityGameFramework\Libraries\GameFramework.dll
// XML documentation location: D:\work\NEW5G\develop\client\My5G\Assets\UnityGameFramework\Libraries\GameFramework.xml

using System;

namespace ET
{
    /// <summary>变量。</summary>
    /// <typeparam name="T">变量类型。</typeparam>
    public abstract class Variable<T> : Variable,IEquatable<Variable<T>>
    {
        private T m_Value;

        /// <summary>初始化变量的新实例。</summary>
        protected Variable() => this.m_Value = default (T);

        /// <summary>初始化变量的新实例。</summary>
        /// <param name="value">初始值。</param>
        protected Variable(T value) => this.m_Value = value;

        /// <summary>获取变量类型。</summary>
        public override Type Type => typeof (T);

        /// <summary>获取或设置变量值。</summary>
        public T Value
        {
            get => this.m_Value;
            set => this.m_Value = value;
        }
        /// <summary>获取变量字符串。</summary>
        /// <returns>变量字符串。</returns>
        public override string ToString() => (object) this.m_Value == null ? "<Null>" : this.m_Value.ToString();
        public virtual bool Equals(Variable<T> other)
        {
            if (other == null)
                return false;
            return this.Value.Equals(((Variable<T>) other).Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return this.Equals(((Variable<T> )obj));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}