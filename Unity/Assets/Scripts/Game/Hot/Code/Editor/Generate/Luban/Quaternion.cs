//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bright.Serialization;
using System.Collections.Generic;
using SimpleJSON;



namespace Game.Hot.Editor
{

public sealed partial class Quaternion :  Bright.Config.EditorBeanBase 
{
    public Quaternion()
    {
    }

    public override void LoadJson(SimpleJSON.JSONObject _json)
    {
        { 
            var _fieldJson = _json["x"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  X = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["y"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  Y = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["z"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  Z = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["w"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  W = _fieldJson;
            }
        }
        
    }

    public override void SaveJson(SimpleJSON.JSONObject _json)
    {
        _json["$type"] = "Quaternion";
        {
            _json["x"] = new JSONNumber(X);
        }
        {
            _json["y"] = new JSONNumber(Y);
        }
        {
            _json["z"] = new JSONNumber(Z);
        }
        {
            _json["w"] = new JSONNumber(W);
        }
    }

    public static Quaternion LoadJsonQuaternion(SimpleJSON.JSONNode _json)
    {
        Quaternion obj = new Quaternion();
        obj.LoadJson((SimpleJSON.JSONObject)_json);
        return obj;
    }
        
    public static void SaveJsonQuaternion(Quaternion _obj, SimpleJSON.JSONNode _json)
    {
        _obj.SaveJson((SimpleJSON.JSONObject)_json);
    }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float W { get; set; }

}

}