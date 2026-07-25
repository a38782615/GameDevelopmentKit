using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerSkillDataComponent))]
    [FriendOf(typeof(PlayerSkillDataComponent))]
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
            self.SkillDataById?.Dispose();
            self.EquippedSkills?.Dispose();
            self.EquippedActiveSkills?.Dispose();
            self.EquippedPassiveSkills?.Dispose();
            self.LearnedSkills = null;
            self.SkillDataById = null;
            self.EquippedSkills = null;
            self.EquippedActiveSkills = null;
            self.EquippedPassiveSkills = null;
        }

        public static async UniTask LoadPlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureSkillCaches();
            List<PlayerSkillData> playerSkills = await archiveComponent.QueryAll<PlayerSkillData>();
            self.LearnedSkills.Clear();
            self.SkillDataById.Clear();
            if (playerSkills == null)
            {
                self.RebuildSkillCaches();
                return;
            }

            foreach (PlayerSkillData playerSkill in playerSkills)
            {
                if (playerSkill == null || playerSkill.Id <= 0 || self.SkillDataById.ContainsKey(playerSkill.Id))
                {
                    continue;
                }

                self.LearnedSkills.Add(playerSkill);
                self.SkillDataById.Add(playerSkill.Id, playerSkill);
            }

            self.RebuildSkillCaches();
        }

        public static async UniTask SavePlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureSkillCaches();
            await archiveComponent.SaveBatch(self.LearnedSkills);
        }

        public static PlayerSkillData GetPlayerSkill(this PlayerSkillDataComponent self, int skillId)
        {
            self.EnsureSkillCaches();
            return self.SkillDataById.TryGetValue(skillId, out PlayerSkillData playerSkill) ? playerSkill : null;
        }

        public static DRSkill GetSkillConfig(this PlayerSkillDataComponent self, int skillId)
        {
            return Tables.Instance?.DTSkill?.GetOrDefault(skillId);
        }

        public static PlayerSkillData LearnSkill(this PlayerSkillDataComponent self, int skillId, int level = 0, bool isEquipped = false)
        {
            if (skillId <= 0 || self.GetSkillConfig(skillId) == null)
            {
                return null;
            }

            PlayerSkillData playerSkill = self.GetPlayerSkill(skillId);
            if (playerSkill != null)
            {
                return playerSkill;
            }

            playerSkill = new PlayerSkillData
            {
                Id = skillId,
                Level = level,
                IsEquipped = isEquipped,
            };
            self.LearnedSkills.Add(playerSkill);
            self.SkillDataById.Add(skillId, playerSkill);
            self.RebuildSkillCaches();
            return playerSkill;
        }

        public static bool SetSkillEquipped(this PlayerSkillDataComponent self, int skillId, bool isEquipped)
        {
            PlayerSkillData playerSkill = self.GetPlayerSkill(skillId);
            if (playerSkill == null)
            {
                return false;
            }

            playerSkill.IsEquipped = isEquipped;
            self.RebuildSkillCaches();
            return true;
        }

        public static bool UpgradeSkill(this PlayerSkillDataComponent self, int skillId)
        {
            PlayerSkillData playerSkill = self.GetPlayerSkill(skillId);
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
                DRSkill skillConfig = self.GetSkillConfig(playerSkill.Id);
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
            return playerSkill != null && Tables.Instance?.DTSkillAttribute?.Get(playerSkill.Id, playerSkill.Level + 1) != null;
        }

        private static void EnsureSkillCaches(this PlayerSkillDataComponent self)
        {
            self.LearnedSkills ??= XList<PlayerSkillData>.Create();
            self.SkillDataById ??= XDictionary<int, PlayerSkillData>.Create();
            self.EquippedSkills ??= XList<PlayerSkillData>.Create();
            self.EquippedActiveSkills ??= XList<PlayerSkillData>.Create();
            self.EquippedPassiveSkills ??= XList<PlayerSkillData>.Create();
        }
    }
}
