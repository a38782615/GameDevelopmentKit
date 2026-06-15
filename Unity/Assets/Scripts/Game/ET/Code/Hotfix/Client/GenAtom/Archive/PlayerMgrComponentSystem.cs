using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerMgrComponent))]
    [FriendOf(typeof(PlayerMgrComponent))]
    public static partial class PlayerMgrComponentSystem
    {
        private const string PlayerDataDocumentId = "PlayerData";

        [EntitySystem]
        private static void Awake(this PlayerMgrComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this PlayerMgrComponent self)
        {
            self.PlayerData = null;
        }

        public static async UniTask LoadPlayerData(this PlayerMgrComponent self)
        {
            ArchiveMgrComponent archiveMgrComponent = self.Root().GetComponent<ArchiveMgrComponent>();
            if (archiveMgrComponent == null)
            {
                Log.Error("PlayerMgrComponent load failed: ArchiveMgrComponent is missing.");
                return;
            }

            ArchiveComponent archiveComponent = archiveMgrComponent.GetCurrentArchive();
            if (archiveComponent == null)
            {
                Log.Error("PlayerMgrComponent load failed: current archive is null.");
                return;
            }

            PlayerData playerData = await archiveComponent.QueryById<PlayerData>(PlayerDataDocumentId);
            if (playerData == null)
            {
                playerData = CreateDefaultPlayerData();
                await archiveComponent.Save(PlayerDataDocumentId, playerData);
            }

            self.PlayerData = playerData;
        }

        public static async UniTask SavePlayerData(this PlayerMgrComponent self)
        {
            if (self.PlayerData == null)
            {
                Log.Error("PlayerMgrComponent save failed: PlayerData is null.");
                return;
            }

            ArchiveMgrComponent archiveMgrComponent = self.Root().GetComponent<ArchiveMgrComponent>();
            if (archiveMgrComponent == null)
            {
                Log.Error("PlayerMgrComponent save failed: ArchiveMgrComponent is missing.");
                return;
            }

            ArchiveComponent archiveComponent = archiveMgrComponent.GetCurrentArchive();
            if (archiveComponent == null)
            {
                Log.Error("PlayerMgrComponent save failed: current archive is null.");
                return;
            }

            await archiveComponent.Save(PlayerDataDocumentId, self.PlayerData);
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
