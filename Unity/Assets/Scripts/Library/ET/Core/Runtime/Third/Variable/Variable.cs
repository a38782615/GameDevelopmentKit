// Decompiled with JetBrains decompiler
// Type: GameFramework.Variable
// Assembly: GameFramework, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 40B9F625-6AD5-4348-B9BB-597BC306BD6E
// Assembly location: D:\work\NEW5G\develop\client\My5G\Assets\UnityGameFramework\Libraries\GameFramework.dll
// XML documentation location: D:\work\NEW5G\develop\client\My5G\Assets\UnityGameFramework\Libraries\GameFramework.xml

using System;

namespace ET
{
    /// <summary>变量。</summary>
    public abstract class Variable : Object, IDisposable
    {
        /// <summary>获取变量类型。</summary>
        public abstract Type Type { get; }
        public virtual void Dispose()
        {
        }
    }
}
