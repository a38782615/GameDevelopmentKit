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
            self.BindMapSwitchButtons();
            self.LoadGrid();
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkills self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
            self.UnbindSkillLists();
        }

        private static void BindMapSwitchButtons(this UIFormSkills self)
        {
            self.View.ReturnExButton.SetAsync(self.ReturnMap);
        }

        private static void UnbindMapSwitchButtons(this UIFormSkills self)
        {
            self.View?.ReturnExButton?.onClick.RemoveAllListeners();
        }

        private static void LoadGrid(this UIFormSkills self)
        {
            self.View.SkillsCommonLoopScrollRect.itemRenderer = self.LearnedSkillRender;
            self.View.Skill0CommonLoopScrollRect.itemRenderer = self.EquippedPassiveSkillRender;
            self.View.Skill1CommonLoopScrollRect.itemRenderer = self.EquippedActiveSkillRender;
            self.Refresh();
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

        private static void Refresh(this UIFormSkills self)
        {
            PlayerSkillDataComponent skillDataComponent = self.GetSkillDataComponent();
            if (skillDataComponent == null)
            {
                return;
            }

            self.View.SkillsCommonLoopScrollRect.numItems = skillDataComponent.GetLearnedSkills().Count;
            self.View.Skill0CommonLoopScrollRect.numItems = skillDataComponent.GetEquippedPassiveSkills().Count;
            self.View.Skill1CommonLoopScrollRect.numItems = skillDataComponent.GetEquippedActiveSkills().Count;

            PlayerData playerData = self.Root().GetPlayerData();
            float normalizedSkillExp = playerData == null ? 0f : Mathf.Clamp01(playerData.SkillExp / 100f);
            self.View.SkillExpSlider.SetValueWithoutNotify(normalizedSkillExp);
            self.View.SkillExpSlider.interactable = false;
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
                ToggleEquipped = true,
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
                ToggleEquipped = false,
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
                ToggleEquipped = false,
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

            PlayerSkillDataComponent skillDataComponent = skills.GetSkillDataComponent();
            bool targetEquipped = self.ToggleEquipped && !self.Data.IsEquipped;
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
