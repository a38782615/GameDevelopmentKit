using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerDataComponent))]
    [FriendOf(typeof(PlayerDataComponent))]
    public static partial class PlayerDataComponentSystem
    {
        private const string LegacyPlayerDataDocumentId = nameof(PlayerData);

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
            var PlayerDataId = GameConst.PlayerDataId;
            PlayerData playerData = await archiveComponent.QueryById<PlayerData>(PlayerDataId);
            bool needSave = false;
            bool needRemoveLegacyData = false;
            if (playerData == null)
            {
                playerData = await archiveComponent.QueryById<PlayerData>(LegacyPlayerDataDocumentId);
                if (playerData != null)
                {
                    playerData.Id = PlayerDataId;
                    needSave = true;
                    needRemoveLegacyData = true;
                }
            }

            if (playerData == null)
            {
                playerData = CreateDefaultPlayerData(PlayerDataId);
                needSave = true;
            }
            var heroConfig = Tables.Instance.DTHero.Get(PlayerDataId);
            var unitAttribute = Tables.Instance.DTUnitAttribute.Get(heroConfig.UnitConfigId, playerData.Level,playerData.SubLevel);

           // if (playerData.Hp == 0)
            {
                playerData.Hp = (long)(unitAttribute.HP*10000);
            }

            if (playerData.Age == 0)
            {
                playerData.Age = 16;
                needSave = true;
            }

            self.PlayerData = playerData;
            Log.Info($"PlayerData loaded: Id={playerData.Id}, Age={playerData.Age}, Level={playerData.Level}, Exp={playerData.Exp}, NickName={playerData.NickName}");

            if (needSave)
            {
                await archiveComponent.Save(playerData);
            }

            if (needRemoveLegacyData)
            {
                await archiveComponent.Remove<PlayerData>(LegacyPlayerDataDocumentId);
            }
        }

        public static async UniTask SavePlayerData(this PlayerDataComponent self, ArchiveComponent archiveComponent)
        {
            if (self.PlayerData == null)
            {
                return;
            }

            var PlayerDataId = GameConst.PlayerDataId;
            self.PlayerData.Id = PlayerDataId;
            await archiveComponent.Save(self.PlayerData);
        }

        private static PlayerData CreateDefaultPlayerData(long id)
        {
            var heroConfig = Tables.Instance.DTHero.Get(id);
            return new PlayerData
            {
                Id = id,
                ConfigId = heroConfig.UnitConfigId,
                Age = 16,
                Exp = 0,
                Level = 0,
                SubLevel = 0,
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
