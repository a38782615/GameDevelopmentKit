using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteDB;
using LiteDB.Engine;

namespace ET.Client
{
    [EntitySystemOf(typeof(ArchiveComponent))]
    [FriendOf(typeof(ArchiveComponent))]
    public static partial class ArchiveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveComponent self, string databasePath)
        {
            self.Open(databasePath, null);
        }

        [EntitySystem]
        private static void Awake(this ArchiveComponent self, string databasePath, string password)
        {
            self.Open(databasePath, password);
        }

        [EntitySystem]
        private static void Destroy(this ArchiveComponent self)
        {
            self.Database?.Dispose();
            self.Database = null;
            self.DisposeArchiveStreams();
            self.Mapper = null;
            self.DatabasePath = null;
            self.Password = null;
            self.LockKey = 0;
            self.UseStreamDatabase = false;
        }

        public static async UniTask<BsonValue> Insert<T>(this ArchiveComponent self, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive insert entity is null: {typeof(T).FullName}");
                return BsonValue.Null;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Insert(entity);
            }
        }

        public static async UniTask Insert<T>(this ArchiveComponent self, BsonValue id, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive insert entity is null: {typeof(T).FullName}");
                return;
            }

            using (await self.WaitArchiveLock())
            {
                self.GetCollection<T>(collection).Insert(id, entity);
            }
        }

        public static async UniTask<int> InsertBatch<T>(this ArchiveComponent self, IEnumerable<T> entities, string collection = null)
        {
            if (entities == null)
            {
                Log.Error($"archive insert batch is null: {typeof(T).FullName}");
                return 0;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Insert(entities);
            }
        }

        public static async UniTask<bool> Update<T>(this ArchiveComponent self, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive update entity is null: {typeof(T).FullName}");
                return false;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Update(entity);
            }
        }

        public static async UniTask<bool> Update<T>(this ArchiveComponent self, BsonValue id, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive update entity is null: {typeof(T).FullName}");
                return false;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Update(id, entity);
            }
        }

        public static async UniTask<int> UpdateBatch<T>(this ArchiveComponent self, IEnumerable<T> entities, string collection = null)
        {
            if (entities == null)
            {
                Log.Error($"archive update batch is null: {typeof(T).FullName}");
                return 0;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Update(entities);
            }
        }

        public static async UniTask<bool> Save<T>(this ArchiveComponent self, T entity, string collection = null)
        {
            return await self.Upsert(entity, collection);
        }

        public static async UniTask<bool> Save<T>(this ArchiveComponent self, BsonValue id, T entity, string collection = null)
        {
            return await self.Upsert(id, entity, collection);
        }

        public static async UniTask<int> SaveBatch<T>(this ArchiveComponent self, IEnumerable<T> entities, string collection = null)
        {
            return await self.UpsertBatch(entities, collection);
        }

        public static async UniTask<bool> Upsert<T>(this ArchiveComponent self, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive upsert entity is null: {typeof(T).FullName}");
                return false;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Upsert(entity);
            }
        }

        public static async UniTask<bool> Upsert<T>(this ArchiveComponent self, BsonValue id, T entity, string collection = null)
        {
            if (ReferenceEquals(entity, null))
            {
                Log.Error($"archive upsert entity is null: {typeof(T).FullName}");
                return false;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Upsert(id, entity);
            }
        }

        public static async UniTask<int> UpsertBatch<T>(this ArchiveComponent self, IEnumerable<T> entities, string collection = null)
        {
            if (entities == null)
            {
                Log.Error($"archive upsert batch is null: {typeof(T).FullName}");
                return 0;
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Upsert(entities);
            }
        }

        public static async UniTask<T> Query<T>(this ArchiveComponent self, BsonValue id, string collection = null)
        {
            return await self.QueryById<T>(id, collection);
        }

        public static async UniTask<T> QueryById<T>(this ArchiveComponent self, BsonValue id, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).FindById(id);
            }
        }

        public static async UniTask<T> QueryOne<T>(this ArchiveComponent self, Query query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).FindOne(GetAllQuery(query));
            }
        }

        public static async UniTask<List<T>> Query<T>(this ArchiveComponent self, Query query, int skip = 0, int limit = int.MaxValue, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Find(GetAllQuery(query), skip, limit).ToList();
            }
        }

        public static async UniTask<List<T>> QueryAll<T>(this ArchiveComponent self, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).FindAll().ToList();
            }
        }

        public static async UniTask<bool> Exists<T>(this ArchiveComponent self, Query query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Exists(GetAllQuery(query));
            }
        }

        public static async UniTask<int> Count<T>(this ArchiveComponent self, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Count();
            }
        }

        public static async UniTask<int> Count<T>(this ArchiveComponent self, Query query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Count(GetAllQuery(query));
            }
        }

        public static async UniTask<long> LongCount<T>(this ArchiveComponent self, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).LongCount();
            }
        }

        public static async UniTask<long> LongCount<T>(this ArchiveComponent self, Query query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).LongCount(GetAllQuery(query));
            }
        }

        public static async UniTask<bool> Remove<T>(this ArchiveComponent self, BsonValue id, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Delete(id);
            }
        }

        public static async UniTask<int> Remove<T>(this ArchiveComponent self, Query query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                ILiteCollection<T> liteCollection = self.GetCollection<T>(collection);
                Query deleteQuery = GetAllQuery(query);
                return deleteQuery.Where.Count == 0 ? liteCollection.DeleteAll() : liteCollection.DeleteMany(GetDeletePredicate(deleteQuery));
            }
        }

        public static async UniTask<bool> EnsureIndex<T>(this ArchiveComponent self, string field, bool unique = false, string collection = null)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentException("archive index field is null or empty", nameof(field));
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).EnsureIndex(field, unique);
            }
        }

        public static async UniTask<bool> DropIndex<T>(this ArchiveComponent self, string field, string collection = null)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentException("archive index field is null or empty", nameof(field));
            }

            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).DropIndex(field);
            }
        }

        public static async UniTask<bool> CollectionExists(this ArchiveComponent self, string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("archive collection is null or empty", nameof(collection));
            }

            using (await self.WaitArchiveLock())
            {
                return self.Database.CollectionExists(collection);
            }
        }

        public static async UniTask<List<string>> GetCollectionNames(this ArchiveComponent self)
        {
            using (await self.WaitArchiveLock())
            {
                return self.Database.GetCollectionNames().ToList();
            }
        }

        public static async UniTask<bool> DropCollection(this ArchiveComponent self, string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("archive collection is null or empty", nameof(collection));
            }

            using (await self.WaitArchiveLock())
            {
                return self.Database.DropCollection(collection);
            }
        }

        public static async UniTask<bool> RenameCollection(this ArchiveComponent self, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName))
            {
                throw new ArgumentException("archive old collection is null or empty", nameof(oldName));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("archive new collection is null or empty", nameof(newName));
            }

            using (await self.WaitArchiveLock())
            {
                return self.Database.RenameCollection(oldName, newName);
            }
        }

        public static async UniTask ResetDatabase(this ArchiveComponent self, bool rebuild = true)
        {
            using (await self.WaitArchiveLock())
            {
                self.CheckDatabase();
                List<string> collectionNames = self.Database.GetCollectionNames().ToList();
                foreach (string collectionName in collectionNames)
                {
                    self.Database.DropCollection(collectionName);
                }

                if (rebuild)
                {
                    self.RebuildDatabase(self.Password);
                }
            }
        }

        public static async UniTask<long> Rebuild(this ArchiveComponent self, string password = null)
        {
            using (await self.WaitArchiveLock())
            {
                self.CheckDatabase();
                return self.RebuildDatabase(password ?? self.Password);
            }
        }

        private static void Open(this ArchiveComponent self, string databasePath, string password)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                throw new ArgumentException("archive database path is null or empty", nameof(databasePath));
            }

            string fullPath = GetArchiveFullPath(databasePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BsonMapper mapper = new BsonMapper
            {
                IncludeFields = true,
                IncludeNonPublic = false,
                SerializeNullValues = true,
                EmptyStringToNull = false,
                TrimWhitespace = false,
            };
            mapper.ResolveMember = ResolveArchiveMember;

            self.DatabasePath = fullPath;
            self.Password = password;
            self.LockKey = GetLockKey(fullPath);
            self.Mapper = mapper;
            self.OpenDatabase(fullPath, password, mapper);
        }

        private static void OpenDatabase(this ArchiveComponent self, string fullPath, string password, BsonMapper mapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            self.UseStreamDatabase = true;
            try
            {
                self.DatabaseStream = CreateArchiveStream(fullPath, password, false);
                self.LogStream = CreateArchiveStream(GetLogPath(fullPath), password, true);
                self.Database = new LiteDatabase(self.DatabaseStream, mapper, self.LogStream);
            }
            catch
            {
                self.DisposeArchiveStreams();
                throw;
            }
#else
            ConnectionString connectionString = new ConnectionString
            {
                Filename = fullPath,
                Password = password,
            };
            self.Database = new LiteDatabase(connectionString, mapper);
#endif
        }

        private static string GetArchiveFullPath(string databasePath)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!Path.IsPathRooted(databasePath))
            {
                if (string.IsNullOrEmpty(GameConst.DataPath))
                {
                    Log.Error("archive data path is not initialized");
                    return Path.GetFullPath(databasePath);
                }

                return Path.GetFullPath(Path.Combine(GameConst.DataPath, databasePath));
            }
#endif
            return Path.GetFullPath(databasePath);
        }

        private static Stream CreateArchiveStream(string path, string password, bool appendOnly)
        {
            FileStream fileStream = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                8192,
                appendOnly ? FileOptions.SequentialScan : FileOptions.RandomAccess);

            return string.IsNullOrEmpty(password) ? fileStream : new AesStream(password, fileStream);
        }

        private static string GetLogPath(string databasePath)
        {
            return Path.Combine(
                Path.GetDirectoryName(databasePath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(databasePath)}-log{Path.GetExtension(databasePath)}");
        }

        private static void DisposeArchiveStreams(this ArchiveComponent self)
        {
            self.DatabaseStream?.Dispose();
            self.DatabaseStream = null;
            self.LogStream?.Dispose();
            self.LogStream = null;
        }

        private static ILiteCollection<T> GetCollection<T>(this ArchiveComponent self, string collection)
        {
            self.CheckDatabase();
            return self.Database.GetCollection<T>(string.IsNullOrEmpty(collection) ? typeof(T).FullName : collection);
        }

        private static async UniTask<CoroutineLock> WaitArchiveLock(this ArchiveComponent self)
        {
            CoroutineLockComponent coroutineLockComponent = self.Root().GetComponent<CoroutineLockComponent>();
            if (coroutineLockComponent == null)
            {
                throw new InvalidOperationException("ArchiveComponent requires CoroutineLockComponent on root");
            }

            return await coroutineLockComponent.Wait(CoroutineLockType.DB, self.LockKey);
        }

        private static void CheckDatabase(this ArchiveComponent self)
        {
            if (self.Database == null)
            {
                throw new ObjectDisposedException(nameof(ArchiveComponent));
            }
        }

        private static long GetLockKey(string value)
        {
            unchecked
            {
                const long offset = 1469598103934665603;
                const long prime = 1099511628211;
                long hash = offset;
                string normalized = value.ToLowerInvariant();
                for (int i = 0; i < normalized.Length; ++i)
                {
                    hash ^= normalized[i];
                    hash *= prime;
                }

                hash &= long.MaxValue;
                return hash == 0 ? 1 : hash;
            }
        }

        private static Query GetAllQuery(Query query)
        {
            return query ?? LiteDB.Query.All();
        }

        private static long RebuildDatabase(this ArchiveComponent self, string password)
        {
            if (self.UseStreamDatabase)
            {
                if (password != self.Password)
                {
                    throw new NotSupportedException("archive rebuild with password change is not supported in stream database mode");
                }

                self.Database.Checkpoint();
                return 0;
            }

            long diff = self.Database.Rebuild(new RebuildOptions
            {
                Password = password,
                Collation = self.Database.Collation,
            });

            self.Password = password;
            return diff;
        }

        private static BsonExpression GetDeletePredicate(Query query)
        {
            if (query.Where.Count == 1)
            {
                return query.Where[0];
            }

            return BsonExpression.Create(string.Join(" AND ", query.Where.Select(expression => $"({expression.Source})")));
        }

        private static void ResolveArchiveMember(Type type, System.Reflection.MemberInfo memberInfo, MemberMapper member)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
            {
                return;
            }

            if (memberInfo.DeclaringType == typeof(Entity) && memberInfo.Name != nameof(Entity.Id))
            {
                member.FieldName = null;
            }
        }
    }
}
