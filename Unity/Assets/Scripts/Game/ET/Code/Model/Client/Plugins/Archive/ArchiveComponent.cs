using UltraLiteDB;

namespace ET.Client
{
    [ComponentOf]
    public class ArchiveComponent : Entity, IAwake<string>, IAwake<string, string>, IDestroy
    {
        public string DatabasePath;
        public string Password;
        public long LockKey;
        public BsonMapper Mapper;
        public UltraLiteDatabase Database;
    }
}
