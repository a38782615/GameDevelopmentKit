//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bright.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Game.Hot
{

public sealed partial class DTAircraft : IDataTable
{
    private readonly Dictionary<int, DRAircraft> _dataMap;
    private readonly List<DRAircraft> _dataList;
    private readonly System.Func<Task<ByteBuf>> _loadFunc;

    public DTAircraft(System.Func<Task<ByteBuf>> loadFunc)
    {
        _loadFunc = loadFunc;
        _dataMap = new Dictionary<int, DRAircraft>();
        _dataList = new List<DRAircraft>();
    }

    public async Task LoadAsync()
    {
        ByteBuf _buf = await _loadFunc();
        _dataMap.Clear();
        _dataList.Clear();
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            DRAircraft _v;
            _v = DRAircraft.DeserializeDRAircraft(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.Id, _v);
        }
        PostInit();
    }

    public Dictionary<int, DRAircraft> DataMap => _dataMap;
    public List<DRAircraft> DataList => _dataList;
    public DRAircraft GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public DRAircraft Get(int key) => _dataMap[key];
    public DRAircraft this[int key] => _dataMap[key];

    public void Resolve(Dictionary<string, IDataTable> _tables)
    {
        foreach(var v in _dataList)
        {
            v.Resolve(_tables);
        }
        PostResolve();
    }

    public void TranslateText(System.Func<string, string, string> translator)
    {
        foreach(var v in _dataList)
        {
            v.TranslateText(translator);
        }
    }

 
    partial void PostInit();
    partial void PostResolve();
}
}