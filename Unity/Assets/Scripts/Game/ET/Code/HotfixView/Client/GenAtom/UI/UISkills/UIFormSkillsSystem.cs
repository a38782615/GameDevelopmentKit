using System.IO;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormSkills))]
    [EntitySystemOf(typeof(UIFormSkills))]
    [FriendOf(typeof(PlayerData))]
    public static partial class UIFormSkillsSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormSkills self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIFormSkills self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormSkills self)
        {
            self.BindSkillLists();
            self.RefreshSkillDisplay();
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkills self, bool isShutdown)
        {
            self.UnbindSkillLists();
        }

        private static void BindSkillLists(this UIFormSkills self)
        {
            MonoUIFormSkills view = self.View;
            if (object.ReferenceEquals(view, null))
            {
                return;
            }

            view.SkillsCommonLoopScrollRect.itemRenderer = self.RenderLearnedSkill;
            view.Skill0CommonLoopScrollRect.itemRenderer = self.RenderEquippedPassiveSkill;
            view.Skill1CommonLoopScrollRect.itemRenderer = self.RenderEquippedActiveSkill;
        }

        private static void UnbindSkillLists(this UIFormSkills self)
        {
            MonoUIFormSkills view = self.View;
            if (object.ReferenceEquals(view, null))
            {
                return;
            }

            view.SkillsCommonLoopScrollRect.itemRenderer = null;
            view.Skill0CommonLoopScrollRect.itemRenderer = null;
            view.Skill1CommonLoopScrollRect.itemRenderer = null;
        }

        private static void RefreshSkillDisplay(this UIFormSkills self)
        {
            MonoUIFormSkills view = self.View;
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (object.ReferenceEquals(view, null) || skillDataComponent == null)
            {
                return;
            }

            view.SkillsCommonLoopScrollRect.numItems = skillDataComponent.GetLearnedSkills().Count;
            view.Skill0CommonLoopScrollRect.numItems = skillDataComponent.GetEquippedPassiveSkills().Count;
            view.Skill1CommonLoopScrollRect.numItems = skillDataComponent.GetEquippedActiveSkills().Count;

            PlayerData playerData = self.Root().GetPlayerData();
            float normalizedSkillExp = playerData == null ? 0f : Mathf.Clamp01(playerData.SkillExp / 100f);
            view.SkillExpSlider.SetValueWithoutNotify(normalizedSkillExp);
            view.SkillExpSlider.interactable = false;
        }

        private static void RenderLearnedSkill(this UIFormSkills self, int index, Transform itemTransform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetLearnedSkills().Count)
            {
                return;
            }

            self.RenderSkill(itemTransform, skillDataComponent.GetLearnedSkills()[index], true);
        }

        private static void RenderEquippedPassiveSkill(this UIFormSkills self, int index, Transform itemTransform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetEquippedPassiveSkills().Count)
            {
                return;
            }

            self.RenderSkill(itemTransform, skillDataComponent.GetEquippedPassiveSkills()[index], false);
        }

        private static void RenderEquippedActiveSkill(this UIFormSkills self, int index, Transform itemTransform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetEquippedActiveSkills().Count)
            {
                return;
            }

            self.RenderSkill(itemTransform, skillDataComponent.GetEquippedActiveSkills()[index], false);
        }

        private static void RenderSkill(
            this UIFormSkills self,
            Transform itemTransform,
            PlayerSkillData playerSkill,
            bool toggleEquipped)
        {
            MonoUISkillsItem item = itemTransform.GetComponent<MonoUISkillsItem>();
            DRSkill skillConfig = playerSkill == null ? null : Tables.Instance.DTSkill.GetOrDefault(playerSkill.ConfigId);
            if (item == null || playerSkill == null || skillConfig == null)
            {
                Log.Warning("UIFormSkills render skipped because item binding or skill data is missing.");
                return;
            }

            item.NameTextMeshProUGUI.text = skillConfig.Name;
            item.LevelTextMeshProUGUI.text = $"Lv.{playerSkill.Level}";
            item.EquippedImage.enabled = playerSkill.IsEquipped;

            if (string.IsNullOrEmpty(skillConfig.IconPath))
            {
                item.IconImage.enabled = false;
            }
            else
            {
                item.IconImage.enabled = true;
                item.IconImage.SetSprite(GetSkillIconSpritePath(skillConfig.IconPath));
            }

            int configId = playerSkill.ConfigId;
            item.ClickExButton.Set(() => self.OnSkillClick(configId, toggleEquipped));
        }

        private static void OnSkillClick(this UIFormSkills self, int configId, bool toggleEquipped)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            PlayerSkillData playerSkill = skillDataComponent?.GetPlayerSkill(configId);
            if (playerSkill == null)
            {
                return;
            }

            bool targetEquipped = toggleEquipped && !playerSkill.IsEquipped;
            if (!skillDataComponent.SetSkillEquipped(configId, targetEquipped))
            {
                return;
            }

            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            gameDataMgrComponent?.SavePlayerSkillData().Forget();
            self.RefreshSkillDisplay();
        }

        private static PlayerSkillDataComponent GetSkillDataComponent(this UIFormSkills self)
        {
            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            if (gameDataMgrComponent == null)
            {
                Log.Warning("UIFormSkills cannot display skills because GameDataMgrComponent is missing.");
                return null;
            }

            return gameDataMgrComponent.GetPlayerSkillDataComponent();
        }

        private static string GetSkillIconSpritePath(string iconPath)
        {
            string normalizedPath = iconPath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/"))
            {
                return normalizedPath;
            }

            string iconName = Path.GetFileNameWithoutExtension(normalizedPath);
            return AssetUtility.GetUISpriteAsset($"SkillIcon/{iconName}");
        }
    }
}
