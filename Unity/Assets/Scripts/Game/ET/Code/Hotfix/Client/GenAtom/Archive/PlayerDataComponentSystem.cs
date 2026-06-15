using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerDataComponent))]
    [FriendOf(typeof(PlayerDataComponent))]
    public static partial class PlayerDataComponentSystem
    {
        private const string PlayerDataDocumentId = nameof(PlayerData);

        [EntitySystem]
        private static void Awake(this PlayerDataComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this PlayerDataComponent self)
        {
            self.PlayerData = null;
        }

        public static async UniTask LoadPlayerData(this PlayerDataComponent self, ArchiveComponent archiveComponent)
        {
            PlayerData playerData = await archiveComponent.QueryById<PlayerData>(PlayerDataDocumentId);
            if (playerData == null)
            {
                playerData = CreateDefaultPlayerData();
                await archiveComponent.Save(PlayerDataDocumentId, playerData);
            }

            self.PlayerData = playerData;
        }

        public static async UniTask SavePlayerData(this PlayerDataComponent self, ArchiveComponent archiveComponent)
        {
            if (self.PlayerData == null)
            {
                return;
            }

            await archiveComponent.Save(PlayerDataDocumentId, self.PlayerData);
        }

        public static void SetPlayerData(this PlayerDataComponent self, PlayerData playerData)
        {
            self.PlayerData = playerData;
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
