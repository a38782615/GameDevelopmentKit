using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(ArchiveMgrComponent))]
    [FriendOf(typeof(ArchiveMgrComponent))]
    public static partial class ArchiveMgrComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveMgrComponent self)
        {
            self.LoadDefaultArchive();
        }

        [EntitySystem]
        private static void Destroy(this ArchiveMgrComponent self)
        {
            self.CloseCurrentArchive();
            self.CurrentArchiveName = null;
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

            ArchiveComponent currentArchive = self.CurrentArchive;
            if (currentArchive != null && self.CurrentArchiveName == archiveName)
            {
                return currentArchive;
            }

            self.CloseCurrentArchive();

            ArchiveComponent archiveComponent = self.AddComponent<ArchiveComponent, string, string>(archiveName, password);
            self.CurrentArchive = archiveComponent;
            self.CurrentArchiveName = archiveName;
            return archiveComponent;
        }

        public static async UniTask<ArchiveComponent> ResetDefaultArchive(this ArchiveMgrComponent self, string password = null)
        {
            return await self.ResetArchive(GetDefaultArchiveName(), password);
        }

        public static async UniTask<ArchiveComponent> ResetArchive(this ArchiveMgrComponent self, string archiveName, string password = null)
        {
            CheckArchiveName(archiveName);

            self.CloseArchiveIfCurrent(archiveName);

            return self.LoadArchive(archiveName, password);
        }

        public static string GetDefaultArchiveName()
        {
            return $"Save{GetDeviceId()}";
        }

        private static void CloseArchiveIfCurrent(this ArchiveMgrComponent self, string archiveName)
        {
            ArchiveComponent currentArchive = self.CurrentArchive;
            if (currentArchive == null)
            {
                return;
            }

            if (self.CurrentArchiveName != archiveName)
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

            return deviceId;
        }
    }
}
