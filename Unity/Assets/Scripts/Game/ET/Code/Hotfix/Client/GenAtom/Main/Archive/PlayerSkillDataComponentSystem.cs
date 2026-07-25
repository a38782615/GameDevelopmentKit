using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerSkillDataComponent))]
    [FriendOf(typeof(PlayerSkillDataComponent))]
    [FriendOf(typeof(GameDataMgrComponent))]
    [FriendOf(typeof(PlayerDataComponent))]
    public static partial class PlayerSkillDataComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
        }

        [EntitySystem]
        private static void Destroy(this PlayerSkillDataComponent self)
        {
            self.LearnedSkills?.Dispose();
            self.SkillDataByConfigId?.Dispose();
            self.EquippedSkills?.Dispose();
            self.EquippedActiveSkills?.Dispose();
            self.EquippedPassiveSkills?.Dispose();
            self.LearnedSkills = null;
            self.SkillDataByConfigId = null;
            self.EquippedSkills = null;
            self.EquippedActiveSkills = null;
            self.EquippedPassiveSkills = null;
        }

        public static async UniTask LoadPlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureSkillCaches();
            List<PlayerSkillData> playerSkills = await archiveComponent.QueryAll<PlayerSkillData>();
            long playerId = self.GetPlayerId();
            self.LearnedSkills.Clear();
            self.SkillDataByConfigId.Clear();
            if (playerSkills == null)
            {
                self.RebuildSkillCaches();
                return;
            }

            foreach (PlayerSkillData playerSkill in playerSkills)
            {
                if (playerSkill == null)
                {
                    continue;
                }

                // Migrate records created before PlayerId and ConfigId were introduced.
                playerSkill.ConfigId = playerSkill.ConfigId > 0 ? playerSkill.ConfigId : (int)playerSkill.Id;
                playerSkill.PlayerId = playerSkill.PlayerId > 0 ? playerSkill.PlayerId : playerId;
                if (playerSkill.PlayerId != playerId || playerSkill.ConfigId <= 0 || self.SkillDataByConfigId.ContainsKey(playerSkill.ConfigId))
                {
                    continue;
                }

                self.LearnedSkills.Add(playerSkill);
                self.SkillDataByConfigId.Add(playerSkill.ConfigId, playerSkill);
            }

            self.RebuildSkillCaches();
        }

        public static async UniTask SavePlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureSkillCaches();
            await archiveComponent.SaveBatch(self.LearnedSkills);
        }

        public static PlayerSkillData GetPlayerSkill(this PlayerSkillDataComponent self, int configId)
        {
            self.EnsureSkillCaches();
            return self.SkillDataByConfigId.TryGetValue(configId, out PlayerSkillData playerSkill) ? playerSkill : null;
        }

        public static DRSkill GetSkillConfig(this PlayerSkillDataComponent self, int configId)
        {
            return Tables.Instance?.DTSkill?.GetOrDefault(configId);
        }

        public static PlayerSkillData LearnSkill(this PlayerSkillDataComponent self, int configId, int level = 0, bool isEquipped = false)
        {
            if (configId <= 0 || self.GetSkillConfig(configId) == null)
            {
                return null;
            }

            PlayerSkillData playerSkill = self.GetPlayerSkill(configId);
            if (playerSkill != null)
            {
                return playerSkill;
            }

            playerSkill = new PlayerSkillData
            {
                Id = IdGenerater.Instance.GenerateId(),
                PlayerId = self.GetPlayerId(),
                ConfigId = configId,
                Level = level,
                IsEquipped = isEquipped,
            };
            self.LearnedSkills.Add(playerSkill);
            self.SkillDataByConfigId.Add(configId, playerSkill);
            self.RebuildSkillCaches();
            return playerSkill;
        }

        public static bool SetSkillEquipped(this PlayerSkillDataComponent self, int configId, bool isEquipped)
        {
            PlayerSkillData playerSkill = self.GetPlayerSkill(configId);
            if (playerSkill == null)
            {
                return false;
            }

            playerSkill.IsEquipped = isEquipped;
            self.RebuildSkillCaches();
            return true;
        }

        public static bool UpgradeSkill(this PlayerSkillDataComponent self, int configId)
        {
            PlayerSkillData playerSkill = self.GetPlayerSkill(configId);
            if (playerSkill == null || !self.CanUpgrade(playerSkill))
            {
                return false;
            }

            ++playerSkill.Level;
            return true;
        }

        public static XList<PlayerSkillData> GetLearnedSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
            return self.LearnedSkills;
        }

        public static XList<PlayerSkillData> GetEquippedSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
            return self.EquippedSkills;
        }

        public static XList<PlayerSkillData> GetEquippedActiveSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
            return self.EquippedActiveSkills;
        }

        public static XList<PlayerSkillData> GetEquippedPassiveSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
            return self.EquippedPassiveSkills;
        }

        public static List<PlayerSkillData> GetUpgradeableSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureSkillCaches();
            List<PlayerSkillData> upgradeableSkills = new List<PlayerSkillData>();
            foreach (PlayerSkillData playerSkill in self.LearnedSkills)
            {
                if (self.CanUpgrade(playerSkill))
                {
                    upgradeableSkills.Add(playerSkill);
                }
            }

            return upgradeableSkills;
        }

        private static void RebuildSkillCaches(this PlayerSkillDataComponent self)
        {
            self.EquippedSkills.Clear();
            self.EquippedActiveSkills.Clear();
            self.EquippedPassiveSkills.Clear();

            foreach (PlayerSkillData playerSkill in self.LearnedSkills)
            {
                if (!playerSkill.IsEquipped)
                {
                    continue;
                }

                self.EquippedSkills.Add(playerSkill);
                DRSkill skillConfig = self.GetSkillConfig(playerSkill.ConfigId);
                if (skillConfig == null)
                {
                    continue;
                }

                if (skillConfig.IsAct == 0)
                {
                    self.EquippedPassiveSkills.Add(playerSkill);
                }
                else
                {
                    self.EquippedActiveSkills.Add(playerSkill);
                }
            }
        }

        private static bool CanUpgrade(this PlayerSkillDataComponent self, PlayerSkillData playerSkill)
        {
            return playerSkill != null && Tables.Instance?.DTSkillAttribute?.Get(playerSkill.ConfigId, playerSkill.Level + 1) != null;
        }

        private static long GetPlayerId(this PlayerSkillDataComponent self)
        {
            GameDataMgrComponent gameDataMgrComponent = self.GetParent<GameDataMgrComponent>();
            PlayerDataComponent playerDataComponent = gameDataMgrComponent?.PlayerDataComponent;
            PlayerData playerData = playerDataComponent?.PlayerData;
            return playerData?.Id ?? GameConst.PlayerDataId;
        }

        private static void EnsureSkillCaches(this PlayerSkillDataComponent self)
        {
            self.LearnedSkills ??= XList<PlayerSkillData>.Create();
            self.SkillDataByConfigId ??= XDictionary<int, PlayerSkillData>.Create();
            self.EquippedSkills ??= XList<PlayerSkillData>.Create();
            self.EquippedActiveSkills ??= XList<PlayerSkillData>.Create();
            self.EquippedPassiveSkills ??= XList<PlayerSkillData>.Create();
        }
    }
}
