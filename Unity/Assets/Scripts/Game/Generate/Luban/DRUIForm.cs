//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bright.Serialization;
using System.Collections.Generic;

namespace Game
{
public sealed partial class DRUIForm :  Bright.Config.BeanBase 
{
    public DRUIForm(ByteBuf _buf) 
    {
        Id = _buf.ReadInt();
        AssetName = _buf.ReadString();
        UIGroupName = _buf.ReadString();
        AllowMultiInstance = _buf.ReadBool();
        PauseCoveredUIForm = _buf.ReadBool();
        PostInit();
    }

    public static DRUIForm DeserializeDRUIForm(ByteBuf _buf)
    {
        return new DRUIForm(_buf);
    }

    /// <summary>
    /// 界面编号
    /// </summary>
    public int Id { get; private set; }
    /// <summary>
    /// 资源名称
    /// </summary>
    public string AssetName { get; private set; }
    /// <summary>
    /// 界面组名称
    /// </summary>
    public string UIGroupName { get; private set; }
    /// <summary>
    /// 是否允许多个界面实例
    /// </summary>
    public bool AllowMultiInstance { get; private set; }
    /// <summary>
    /// 是否暂停被其覆盖的界面
    /// </summary>
    public bool PauseCoveredUIForm { get; private set; }
    public const int __ID__ = 515966854;
    public override int GetTypeId() => __ID__;

    public  void Resolve(Dictionary<string, IDataTable> _tables)
    {
        PostResolve();
    }

    public  void TranslateText(System.Func<string, string, string> translator)
    {
    }

    public override string ToString()
    {
        return "{ "
        + "Id:" + Id + ","
        + "AssetName:" + AssetName + ","
        + "UIGroupName:" + UIGroupName + ","
        + "AllowMultiInstance:" + AllowMultiInstance + ","
        + "PauseCoveredUIForm:" + PauseCoveredUIForm + ","
        + "}";
    }

    partial void PostInit();
    partial void PostResolve();
}
}