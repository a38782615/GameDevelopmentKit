using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(ArchiveMgrComponent))]
    [FriendOf(typeof(ArchiveMgrComponent))]
    public static partial class ArchiveMgrComponentSystem
    {
        private const string ArchiveDirectoryName = "Archive";
        private const string ArchiveFileExtension = ".db";
        private const string DefaultApplicationName = "myGameDevelopmentKit";

        [EntitySystem]
        private static void Awake(this ArchiveMgrComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ArchiveMgrComponent self)
        {
            self.CloseCurrentArchive();
            self.CurrentArchiveName = null;
            self.CurrentArchivePath = null;
        }

        public static ArchiveComponent GetCurrentArchive(this ArchiveMgrComponent self)
        {
            return self.CurrentArchive;
        }

        public static ArchiveComponent LoadDefaultArchive(this ArchiveMgrComponent self, string password = null)
        {
            return self.LoadArchive(GetDefaultArchiveName(), password);
        }

        public static ArchiveComponent LoadArchive(this ArchiveMgrComponent self, string archiveName, string password = null)
        {
            CheckArchiveName(archiveName);
            string archivePath = GetArchivePath(archiveName);

            ArchiveComponent currentArchive = self.CurrentArchive;
            if (currentArchive != null && self.CurrentArchiveName == archiveName && self.CurrentArchivePath == archivePath)
            {
                return currentArchive;
            }

            self.CloseCurrentArchive();

            ArchiveComponent archiveComponent = self.AddComponent<ArchiveComponent, string, string>(archivePath, password);
            self.CurrentArchive = archiveComponent;
            self.CurrentArchiveName = archiveName;
            self.CurrentArchivePath = archivePath;
            return archiveComponent;
        }

        public static async UniTask<ArchiveComponent> ResetDefaultArchive(this ArchiveMgrComponent self, string password = null)
        {
            return await self.ResetArchive(GetDefaultArchiveName(), password);
        }

        public static async UniTask<ArchiveComponent> ResetArchive(this ArchiveMgrComponent self, string archiveName, string password = null)
        {
            CheckArchiveName(archiveName);
            string archivePath = GetArchivePath(archiveName);

            self.CloseArchiveIfCurrent(archiveName, archivePath);

            using (await self.WaitArchiveMgrLock(archiveName))
            {
                DeleteArchiveFiles(archivePath);
            }

            return self.LoadArchive(archiveName, password);
        }

        public static string GetDefaultArchiveName()
        {
            return $"Save{GetDeviceId()}";
        }

        public static string GetArchivePath(string archiveName)
        {
            CheckArchiveName(archiveName);
            string fileName = $"{archiveName}{ArchiveFileExtension}";
            return Path.GetFullPath(Path.Combine(GetArchiveDirectory(), fileName));
        }

        private static void CloseArchiveIfCurrent(this ArchiveMgrComponent self, string archiveName, string archivePath)
        {
            ArchiveComponent currentArchive = self.CurrentArchive;
            if (currentArchive == null)
            {
                return;
            }

            if (self.CurrentArchiveName != archiveName && self.CurrentArchivePath != archivePath)
            {
                return;
            }

            self.CloseCurrentArchive();
        }

        private static void CloseCurrentArchive(this ArchiveMgrComponent self)
        {
            ArchiveComponent currentArchive = self.CurrentArchive;
            if (currentArchive != null)
            {
                currentArchive.Dispose();
            }

            self.CurrentArchive = null;
            self.CurrentArchiveName = null;
            self.CurrentArchivePath = null;
        }

        private static async UniTask<CoroutineLock> WaitArchiveMgrLock(this ArchiveMgrComponent self, string archiveName)
        {
            CoroutineLockComponent coroutineLockComponent = self.Root().GetComponent<CoroutineLockComponent>();
            if (coroutineLockComponent == null)
            {
                throw new InvalidOperationException("ArchiveMgrComponent requires CoroutineLockComponent on root");
            }

            return await coroutineLockComponent.Wait(CoroutineLockType.DB, GetLockKey(archiveName));
        }

        private static void DeleteArchiveFiles(string archivePath)
        {
            string fullPath = Path.GetFullPath(archivePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }

            string fileName = Path.GetFileName(fullPath);
            foreach (string filePath in Directory.GetFiles(directory, $"{fileName}*"))
            {
                File.Delete(filePath);
            }
        }

        private static void CheckArchiveName(string archiveName)
        {
            if (string.IsNullOrWhiteSpace(archiveName))
            {
                throw new ArgumentException("archive name is null or empty", nameof(archiveName));
            }
        }

        private static string GetDeviceId()
        {
            string deviceId = global::ET.GameConst.DeviceId;

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("GameConst.DeviceId is null or empty");
            }

            return deviceId;
        }

        private static string GetArchiveDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, DefaultApplicationName, ArchiveDirectoryName);
        }

        private static long GetLockKey(string value)
        {
            unchecked
            {
                const long offset = 1469598103934665603;
                const long prime = 1099511628211;
                long hash = offset;
                string lowerValue = value.ToLowerInvariant();
                for (int i = 0; i < lowerValue.Length; ++i)
                {
                    hash ^= lowerValue[i];
                    hash *= prime;
                }

                hash &= long.MaxValue;
                return hash == 0 ? 1 : hash;
            }
        }
    }
}
