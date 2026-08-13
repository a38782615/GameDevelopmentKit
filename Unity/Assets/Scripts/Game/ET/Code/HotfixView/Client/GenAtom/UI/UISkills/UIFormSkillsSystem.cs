using System.IO;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(UIFormSkills))]
    [EntitySystemOf(typeof(UIFormSkills))]
    [FriendOf(typeof(PlayerData))]
    public static partial class UIFormSkillsSystem
    {
        private const int SkillExpPerLevel = 100;

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
            self.OpenWidget(self.View.BtmBarBtmBar);
            self.SelectedSkill = null;
            self.BindMapSwitchButtons();
            self.LoadGrid();
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkills self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
            self.UnbindSkillLists();
            self.SelectedSkill = null;
        }

        private static void BindMapSwitchButtons(this UIFormSkills self)
        {
            self.View.LevelUpExButton.SetAsync(self.LevelUpSkill);
        }

        private static void UnbindMapSwitchButtons(this UIFormSkills self)
        {
            self.View?.LevelUpExButton?.onClick.RemoveAllListeners();
        }

        private static void LoadGrid(this UIFormSkills self)
        {
            self.View.SkillsLoopVerticalScrollRect.itemRenderer = self.LearnedSkillRender;
            self.View.Skill0LoopHorizontalScrollRect.itemRenderer = self.EquippedPassiveSkillRender;
            self.View.Skill1LoopHorizontalScrollRect.itemRenderer = self.EquippedActiveSkillRender;
            self.Refresh();
        }

        private static void UnbindSkillLists(this UIFormSkills self)
        {
            MonoUIFormSkills view = self.View;
            if (object.ReferenceEquals(view, null))
            {
                return;
            }

            view.SkillsLoopVerticalScrollRect.itemRenderer = null;
            view.Skill0LoopHorizontalScrollRect.itemRenderer = null;
            view.Skill1LoopHorizontalScrollRect.itemRenderer = null;
        }

        private static void Refresh(this UIFormSkills self)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null)
            {
                self.SelectedSkill = null;
                self.RefreshSelectedSkill();
                return;
            }

            self.View.SkillsLoopVerticalScrollRect.numItems = skillDataComponent.GetLearnedSkills().Count;
            self.View.Skill0LoopHorizontalScrollRect.numItems = skillDataComponent.GetEquippedPassiveSkills().Count;
            self.View.Skill1LoopHorizontalScrollRect.numItems = skillDataComponent.GetEquippedActiveSkills().Count;

            self.EnsureSelectedSkill(skillDataComponent);
            self.RefreshSelectedSkill();
        }

        private static void EnsureSelectedSkill(this UIFormSkills self, PlayerSkillDataComponent skillDataComponent)
        {
            if (self.SelectedSkill != null && skillDataComponent.GetPlayerSkill(self.SelectedSkill.ConfigId) != null)
            {
                return;
            }

            XList<PlayerSkillData> learnedSkills = skillDataComponent.GetLearnedSkills();
            self.SelectedSkill = learnedSkills.Count > 0 ? learnedSkills[0] : null;
        }

        private static void RefreshSelectedSkill(this UIFormSkills self)
        {
            PlayerSkillData selectedSkill = self.SelectedSkill;
            DRSkill skillConfig = selectedSkill == null ? null : Tables.Instance.DTSkill.GetOrDefault(selectedSkill.ConfigId);
            PlayerData playerData = self.Root().GetPlayerData();
            int skillExp = selectedSkill == null ? 0 : playerData?.SkillExp ?? 0;
            float normalizedSkillExp = Mathf.Clamp01(skillExp / (float)SkillExpPerLevel);
            self.View.SkillExpSlider.SetValueWithoutNotify(normalizedSkillExp);
            self.View.SkillExpSlider.interactable = false;
            self.View.SkillExpTxtUXTextMeshPro.text = selectedSkill == null
                    ? string.Empty
                    : GameFramework.Utility.Text.Format("{0}/{1}", skillExp, SkillExpPerLevel);
            self.View.SkillNameUXTextMeshPro.text = LocalizationHelper.GetString(skillConfig?.Name,"") ?? string.Empty;
            self.View.SkillLevelUXTextMeshPro.text = selectedSkill == null
                    ? string.Empty
                    : GameFramework.Utility.Text.Format("LV.{0}", selectedSkill.Level);
            self.View.LevelUpExButton.interactable = selectedSkill != null &&
                    playerData != null &&
                    skillExp >= SkillExpPerLevel &&
                    Tables.Instance.DTSkillAttribute.Get(selectedSkill.ConfigId, selectedSkill.Level + 1) != null;
        }

        private static void LearnedSkillRender(this UIFormSkills self, int index, Transform transform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetLearnedSkills().Count)
            {
                return;
            }

            var item = new SkillTempLogic
            {
                transform = transform,
                Skills = self,
                Data = skillDataComponent.GetLearnedSkills()[index],
            };
            item.ItemRender();
        }

        private static void EquippedPassiveSkillRender(this UIFormSkills self, int index, Transform transform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetEquippedPassiveSkills().Count)
            {
                return;
            }

            var item = new SkillTempLogic
            {
                transform = transform,
                Skills = self,
                Data = skillDataComponent.GetEquippedPassiveSkills()[index],
            };
            item.ItemRender();
        }

        private static void EquippedActiveSkillRender(this UIFormSkills self, int index, Transform transform)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null || index < 0 || index >= skillDataComponent.GetEquippedActiveSkills().Count)
            {
                return;
            }

            var item = new SkillTempLogic
            {
                transform = transform,
                Skills = self,
                Data = skillDataComponent.GetEquippedActiveSkills()[index],
            };
            item.ItemRender();
        }

        private static void ItemRender(this SkillTempLogic self)
        {
            DRSkill skillConfig = self.Data == null ? null : Tables.Instance.DTSkill.GetOrDefault(self.Data.ConfigId);
            if (skillConfig == null)
            {
                Log.Warning("UIFormSkills render skipped because skill data is missing.");
                return;
            }

            Transform transform = self.transform;
            UXTextMeshPro level = transform.Find("Count").GetComponent<UXTextMeshPro>();
            level.text = self.Data.Level.ToString();

            UXTextMeshPro skillName = transform.Find("Name").GetComponent<UXTextMeshPro>();
            skillName.text = LocalizationHelper.GetString(skillConfig?.Name, "") ?? string.Empty;
            // Image icon = transform.GetComponent<Image>();
            // icon.color = self.Data.IsEquipped ? Color.white : new Color(1f, 1f, 1f, 0.65f);

            // if (string.IsNullOrEmpty(skillConfig.IconPath))
            // {
            //     icon.enabled = false;
            // }
            // else
            // {
            //     icon.enabled = true;
            //     icon.SetSprite(GetSkillIconSpritePath(skillConfig.IconPath));
            // }

            ExButton button = transform.GetComponent<ExButton>();
            button.SetAsync(self.ItemClick);
            button.interactable = true;
        }

        private static async UniTask ItemClick(this SkillTempLogic self, Button button)
        {
            UIFormSkills skills = self.Skills;
            if (skills == null || skills.IsDisposed || self.Data == null)
            {
                return;
            }

            skills.SelectedSkill = self.Data;
            skills.RefreshSelectedSkill();

            PlayerSkillDataComponent skillDataComponent = skills.GetSkillDataComponent();
            bool targetEquipped = !self.Data.IsEquipped;
            if (skillDataComponent == null || !skillDataComponent.SetSkillEquipped(self.Data.ConfigId, targetEquipped))
            {
                return;
            }

            GameDataMgrComponent gameDataMgrComponent = skills.Root().GetComponent<GameDataMgrComponent>();
            if (gameDataMgrComponent != null)
            {
                await gameDataMgrComponent.SavePlayerSkillData();
            }

            if (!skills.IsDisposed)
            {
                skills.Refresh();
            }
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

        private static async UniTask LevelUpSkill(this UIFormSkills self, Button button)
        {
            PlayerSkillData selectedSkill = self.SelectedSkill;
            PlayerData playerData = self.Root().GetPlayerData();
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (selectedSkill == null || playerData == null || skillDataComponent == null ||
                playerData.SkillExp < SkillExpPerLevel || !skillDataComponent.UpgradeSkill(selectedSkill.ConfigId))
            {
                self.RefreshSelectedSkill();
                return;
            }

            playerData.SkillExp -= SkillExpPerLevel;
            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            if (gameDataMgrComponent != null)
            {
                await gameDataMgrComponent.SavePlayerData();
                await gameDataMgrComponent.SavePlayerSkillData();
            }

            if (!self.IsDisposed)
            {
                self.Refresh();
            }
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
        private static async UniTask ReturnMap(this UIFormSkills self, Button button)
        {
            Scene root = self.Root();
            await EventSystem.Instance.PublishAsync(root, new GoScene
            {
                SceneId = Tables.Instance.DTGameConfig.SceneMain,
                UI = UGFUIFormId.UIFormMap,
            });
        }
    }
}
