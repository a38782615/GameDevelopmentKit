using System;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameDataMgrComponent))]
    [FriendOf(typeof(GameDataMgrComponent))]
    public static partial class GameDataMgrComponentSystem
    {
        private const string PlayerDataDocumentId = "PlayerData";

        [EntitySystem]
        private static void Awake(this GameDataMgrComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this GameDataMgrComponent self)
        {
            self.PlayerData = null;
        }

        public static async UniTask LoadAllData(this GameDataMgrComponent self)
        {
            self.PlayerData = await self.LoadData(PlayerDataDocumentId, CreateDefaultPlayerData);
        }

        public static async UniTask SaveAllData(this GameDataMgrComponent self)
        {
            await self.SaveData(PlayerDataDocumentId, self.PlayerData);
        }

        public static async UniTask SavePlayerData(this GameDataMgrComponent self)
        {
            await self.SaveData(PlayerDataDocumentId, self.PlayerData);
        }

        public static void SetPlayerData(this GameDataMgrComponent self, PlayerData playerData)
        {
            self.PlayerData = playerData;
        }

        private static async UniTask<T> LoadData<T>(this GameDataMgrComponent self, string documentId, Func<T> defaultFactory) where T : class
        {
            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return null;
            }

            T data = await archiveComponent.QueryById<T>(documentId);
            if (data != null)
            {
                return data;
            }

            data = defaultFactory();
            await archiveComponent.Save(documentId, data);
            return data;
        }

        private static async UniTask SaveData<T>(this GameDataMgrComponent self, string documentId, T data) where T : class
        {
            if (data == null)
            {
                Log.Error($"GameDataMgrComponent save failed: {documentId} is null.");
                return;
            }

            ArchiveComponent archiveComponent = self.GetArchiveComponent();
            if (archiveComponent == null)
            {
                return;
            }

            await archiveComponent.Save(documentId, data);
        }

        private static ArchiveComponent GetArchiveComponent(this GameDataMgrComponent self)
        {
            ArchiveMgrComponent archiveMgrComponent = self.Root().GetComponent<ArchiveMgrComponent>();
            if (archiveMgrComponent == null)
            {
                Log.Error("GameDataMgrComponent load failed: ArchiveMgrComponent is missing.");
                return null;
            }

            ArchiveComponent archiveComponent = archiveMgrComponent.GetCurrentArchive();
            if (archiveComponent == null)
            {
                Log.Error("GameDataMgrComponent load failed: current archive is null.");
                return null;
            }

            return archiveComponent;
        }

        private static PlayerData CreateDefaultPlayerData()
        {
            return new PlayerData
            {
                Age = 0,
                Exp = 0,
                Level = 1,
                NickName = string.Empty,
                Diamond = 0,
                XRoot = default,
                ElixirPoison = 0,
                Physique = 0,
                Comprehension = 0,
                DivineSense = 0,
                Fortune = 0,
            };
        }
    }
}
