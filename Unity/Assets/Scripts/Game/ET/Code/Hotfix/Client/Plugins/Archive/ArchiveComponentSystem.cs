using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UltraLiteDB;
using UltraLiteQuery = UltraLiteDB.Query;

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
            self.Mapper = null;
            self.DatabasePath = null;
            self.Password = null;
            self.LockKey = 0;
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

        public static async UniTask<T> QueryOne<T>(this ArchiveComponent self, UltraLiteQuery query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).FindOne(GetAllQuery(query));
            }
        }

        public static async UniTask<List<T>> Query<T>(this ArchiveComponent self, UltraLiteQuery query, int skip = 0, int limit = int.MaxValue, string collection = null)
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

        public static async UniTask<bool> Exists<T>(this ArchiveComponent self, UltraLiteQuery query, string collection = null)
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

        public static async UniTask<int> Count<T>(this ArchiveComponent self, UltraLiteQuery query, string collection = null)
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

        public static async UniTask<long> LongCount<T>(this ArchiveComponent self, UltraLiteQuery query, string collection = null)
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

        public static async UniTask<int> Remove<T>(this ArchiveComponent self, UltraLiteQuery query, string collection = null)
        {
            using (await self.WaitArchiveLock())
            {
                return self.GetCollection<T>(collection).Delete(GetAllQuery(query));
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

        public static async UniTask<long> Shrink(this ArchiveComponent self, string password = null)
        {
            using (await self.WaitArchiveLock())
            {
                return password == null ? self.Database.Shrink() : self.Database.Shrink(password);
            }
        }

        private static void Open(this ArchiveComponent self, string databasePath, string password)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                throw new ArgumentException("archive database path is null or empty", nameof(databasePath));
            }

            string fullPath = Path.GetFullPath(databasePath);
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

            ConnectionString connectionString = new ConnectionString
            {
                Filename = fullPath,
                Password = password,
            };

            self.DatabasePath = fullPath;
            self.Password = password;
            self.LockKey = GetLockKey(fullPath);
            self.Mapper = mapper;
            self.Database = new UltraLiteDatabase(connectionString, mapper, null);
        }

        private static UltraLiteCollection<T> GetCollection<T>(this ArchiveComponent self, string collection)
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

        private static UltraLiteQuery GetAllQuery(UltraLiteQuery query)
        {
            return query ?? UltraLiteQuery.All(1);
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
