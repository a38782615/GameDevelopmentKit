using System;
using Cysharp.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormLoginComponent : UGFUIForm<MonoUIFormLogin>, IAwake, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        [BsonIgnore]
        public bool IsAllTestWidgetsLoaded;

        [BsonIgnore]
        public AutoResetUniTaskCompletionSourcePlus TestWidgetsLoadedTcs;

        [BsonIgnore]
        public Exception TestWidgetsLoadException;
    }
}
