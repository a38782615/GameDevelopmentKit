using System.IO;
using LiteDB;

namespace ET.Client
{
    [ComponentOf]
    public class ArchiveComponent : Entity, IAwake<string>, IAwake<string, string>, IDestroy
    {
        public string DatabasePath;
        public string Password;
        public long LockKey;
        public BsonMapper Mapper;
        public LiteDatabase Database;
        public Stream DatabaseStream;
        public Stream LogStream;
        public bool UseStreamDatabase;
    }
}
