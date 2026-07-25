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
            self.EnsureLearnedSkills();
        }

        [EntitySystem]
        private static void Destroy(this PlayerSkillDataComponent self)
        {
            self.LearnedSkills?.Clear();
            self.LearnedSkills = null;
        }

        public static async UniTask LoadPlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureLearnedSkills();
            List<PlayerSkillData> playerSkills = await archiveComponent.QueryAll<PlayerSkillData>();
            self.LearnedSkills.Clear();
            if (playerSkills == null)
            {
                return;
            }

            foreach (PlayerSkillData playerSkill in playerSkills)
            {
                if (playerSkill == null || playerSkill.Id <= 0 || self.GetPlayerSkill(playerSkill.Id) != null)
                {
                    continue;
                }

                self.LearnedSkills.Add(playerSkill);
            }
        }

        public static async UniTask SavePlayerSkillData(this PlayerSkillDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureLearnedSkills();
            await archiveComponent.SaveBatch(self.LearnedSkills);
        }

        public static PlayerSkillData GetPlayerSkill(this PlayerSkillDataComponent self, int skillId)
        {
            self.EnsureLearnedSkills();
            return self.LearnedSkills.Find(skill => skill.Id == skillId);
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

        public static List<PlayerSkillData> GetLearnedSkills(this PlayerSkillDataComponent self)
        {
            self.EnsureLearnedSkills();
            return new List<PlayerSkillData>(self.LearnedSkills);
        }

        public static List<PlayerSkillData> GetEquippedSkills(this PlayerSkillDataComponent self)
        {
            return self.GetSkills(skill => skill.IsEquipped);
        }

        public static List<PlayerSkillData> GetEquippedActiveSkills(this PlayerSkillDataComponent self)
        {
            return self.GetSkills(skill => skill.IsEquipped && self.IsActiveSkill(skill.Id));
        }

        public static List<PlayerSkillData> GetEquippedPassiveSkills(this PlayerSkillDataComponent self)
        {
            return self.GetSkills(skill => skill.IsEquipped && !self.IsActiveSkill(skill.Id));
        }

        public static List<PlayerSkillData> GetUpgradeableSkills(this PlayerSkillDataComponent self)
        {
            return self.GetSkills(skill => self.CanUpgrade(skill));
        }

        private static List<PlayerSkillData> GetSkills(this PlayerSkillDataComponent self, System.Predicate<PlayerSkillData> predicate)
        {
            self.EnsureLearnedSkills();
            return self.LearnedSkills.FindAll(predicate);
        }

        private static bool IsActiveSkill(this PlayerSkillDataComponent self, int skillId)
        {
            DRSkill skillConfig = self.GetSkillConfig(skillId);
            return skillConfig != null && skillConfig.IsAct != 0;
        }

        private static bool CanUpgrade(this PlayerSkillDataComponent self, PlayerSkillData playerSkill)
        {
            return playerSkill != null && Tables.Instance?.DTSkillAttribute?.Get(playerSkill.Id, playerSkill.Level + 1) != null;
        }

        private static void EnsureLearnedSkills(this PlayerSkillDataComponent self)
        {
            self.LearnedSkills ??= new List<PlayerSkillData>();
        }
    }
}
